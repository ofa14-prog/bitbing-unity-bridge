using System;
using UnityEngine;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Deletes a GameObject from the Unity scene.
    /// </summary>
    [Serializable]
    public class DeleteGameObjectCommand : IAgentCommand
    {
        private readonly string _path;

        public DeleteGameObjectCommand(string path)
        {
            _path = path;
        }

        public CommandResult Execute()
        {
            if (string.IsNullOrEmpty(_path))
            {
                return CommandResult.Failure("INVALID_PATH", "GameObject path is required");
            }

            try
            {
                var gameObject = GameObject.Find(_path);

                if (gameObject == null)
                {
                    return CommandResult.Failure("NOT_FOUND", $"GameObject not found: {_path}");
                }

                UnityEngine.Object.DestroyImmediate(gameObject);

                return CommandResult.SuccessResult(new System.Collections.Generic.Dictionary<string, object>
                {
                    ["deletedPath"] = _path
                });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure("DELETE_FAILED", ex.Message);
            }
        }
    }
}
