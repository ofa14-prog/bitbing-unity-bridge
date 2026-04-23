using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Writes a C# script file to the Assets folder.
    /// Implements path security from EKLENTİR.md §7.2.
    /// </summary>
    [Serializable]
    public class WriteScriptCommand : IAgentCommand
    {
        private readonly string _path;
        private readonly string _content;
        private readonly bool _refreshAssets;
        private readonly bool _waitForCompile;

        public WriteScriptCommand(string path, string content, bool refreshAssets = true, bool waitForCompile = false)
        {
            _path = path;
            _content = content;
            _refreshAssets = refreshAssets;
            _waitForCompile = waitForCompile;
        }

        public CommandResult Execute()
        {
            if (string.IsNullOrEmpty(_path))
            {
                return CommandResult.Failure("INVALID_PATH", "Script path is required");
            }

            if (!IsPathSafe(_path))
            {
                return CommandResult.Failure("INVALID_PATH", "Path must be within Assets/ directory");
            }

            try
            {
                var directory = Path.GetDirectoryName(_path);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var fullPath = Path.GetFullPath(_path);
                File.WriteAllText(fullPath, _content, System.Text.Encoding.UTF8);

                if (_refreshAssets)
                {
                    AssetDatabase.ImportAsset(_path, ImportAssetOptions.ForceUpdate);
                }

                if (_waitForCompile)
                {
                    WaitForCompilation();
                }

                return CommandResult.SuccessResult(new Dictionary<string, object>
                {
                    ["path"] = _path,
                    ["bytesWritten"] = _content.Length
                });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure("WRITE_FAILED", ex.Message);
            }
        }

        private bool IsPathSafe(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var dataPath = Application.dataPath;

            if (!fullPath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var relativePath = fullPath.Substring(dataPath.Length);

            if (relativePath.Contains(".."))
            {
                return false;
            }

            if (relativePath.StartsWith("/Editor/", StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith("/Packages/", StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith("/ProjectSettings/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private void WaitForCompilation()
        {
            var timeout = DateTime.Now.AddSeconds(60);

            while (!EditorApplication.isCompiling && DateTime.Now < timeout)
            {
                System.Threading.Thread.Sleep(100);
            }
        }
    }
}
