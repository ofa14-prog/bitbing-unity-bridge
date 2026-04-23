using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Opens an existing Unity scene.
    /// </summary>
    [Serializable]
    public class OpenSceneCommand : IAgentCommand
    {
        private readonly string _path;

        public OpenSceneCommand(string path)
        {
            _path = path;
        }

        public CommandResult Execute()
        {
            if (string.IsNullOrEmpty(_path))
            {
                return CommandResult.Failure("INVALID_PATH", "Scene path is required");
            }

            try
            {
                if (!System.IO.File.Exists(_path))
                {
                    return CommandResult.Failure("NOT_FOUND", $"Scene not found: {_path}");
                }

                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return CommandResult.Failure("SAVE_CANCELLED", "User cancelled save current scenes");
                }

                EditorSceneManager.OpenScene(_path);

                return CommandResult.SuccessResult(new Dictionary<string, object>
                {
                    ["path"] = _path,
                    ["name"] = System.IO.Path.GetFileNameWithoutExtension(_path)
                });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure("OPEN_SCENE_FAILED", ex.Message);
            }
        }
    }
}
