using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Enters Unity Play Mode.
    /// </summary>
    [Serializable]
    public class EnterPlayModeCommand : IAgentCommand
    {
        private readonly bool _waitForLoad;
        private readonly float _timeoutSeconds;

        public EnterPlayModeCommand(bool waitForLoad = true, float timeoutSeconds = 30f)
        {
            _waitForLoad = waitForLoad;
            _timeoutSeconds = timeoutSeconds;
        }

        public CommandResult Execute()
        {
            try
            {
                if (EditorApplication.isPlaying)
                {
                    return CommandResult.Failure("ALREADY_PLAYING", "Already in Play Mode");
                }

                if (EditorApplication.isCompiling)
                {
                    return CommandResult.Failure("COMPILING", "Cannot enter Play Mode while compiling");
                }

                var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                EditorApplication.isPlaying = true;

                if (_waitForLoad)
                {
                    var startTime = DateTime.Now;
                    while (!EditorApplication.isPlaying && (DateTime.Now - startTime).TotalSeconds < _timeoutSeconds)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }

                return CommandResult.SuccessResult(new Dictionary<string, object>
                {
                    ["sceneName"] = sceneName,
                    ["entered"] = true
                });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure("ENTER_PLAY_MODE_FAILED", ex.Message);
            }
        }
    }
}
