using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Takes a screenshot of the Game view.
    /// </summary>
    [Serializable]
    public class TakeScreenshotCommand : IAgentCommand
    {
        private readonly string _outputPath;
        private readonly int _width;
        private readonly int _height;
        private readonly bool _includeUI;

        public TakeScreenshotCommand(string outputPath, int width = 1920, int height = 1080, bool includeUI = true)
        {
            _outputPath = outputPath;
            _width = width;
            _height = height;
            _includeUI = includeUI;
        }

        public CommandResult Execute()
        {
            if (string.IsNullOrEmpty(_outputPath))
            {
                return CommandResult.Failure("INVALID_PATH", "Output path is required");
            }

            try
            {
                var directory = Path.GetDirectoryName(_outputPath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var fullPath = Path.GetFullPath(_outputPath);

                // ScreenCapture is available in Unity 2022+
#if UNITY_2022_3_OR_NEWER
                var screenshot = UnityEngine.ScreenCapture.CaptureScreenshot(_outputPath, _includeUI);
#else
                var screenshot = UnityEngine.ScreenCapture.CaptureScreenshot(_outputPath);
#endif

                return CommandResult.SuccessResult(new Dictionary<string, object>
                {
                    ["path"] = fullPath,
                    ["width"] = _width,
                    ["height"] = _height,
                    ["includeUI"] = _includeUI
                });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure("SCREENSHOT_FAILED", ex.Message);
            }
        }
    }
}
