using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Creates a new Unity scene.
    /// </summary>
    [Serializable]
    public class CreateSceneCommand : IAgentCommand
    {
        private string _path;

        public CreateSceneCommand(string path)
        {
            _path = path;
        }

        public CommandResult Execute()
        {
            if (string.IsNullOrEmpty(_path))
            {
                return CommandResult.Failure("INVALID_PATH", "Scene path is required");
            }

            var path = _path;
            if (!path.EndsWith(".unity"))
            {
                path += ".unity";
            }

            try
            {
                if (!IsPathSafe(path))
                {
                    return CommandResult.Failure("INVALID_PATH", "Path must be within Assets/ directory");
                }

                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);

                EditorSceneManager.SaveScene(scene, path);

                return CommandResult.SuccessResult(new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["name"] = System.IO.Path.GetFileNameWithoutExtension(path)
                });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure("CREATE_SCENE_FAILED", ex.Message);
            }
        }

        private bool IsPathSafe(string path)
        {
            var fullPath = System.IO.Path.GetFullPath(path);
            var dataPath = Application.dataPath;

            if (!fullPath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var relativePath = fullPath.Substring(dataPath.Length);

            return !relativePath.Contains("..");
        }
    }
}
