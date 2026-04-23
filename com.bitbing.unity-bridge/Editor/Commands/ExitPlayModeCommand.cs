using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Exits Unity Play Mode.
    /// </summary>
    [Serializable]
    public class ExitPlayModeCommand : IAgentCommand
    {
        private DateTime _playModeStartTime;

        public CommandResult Execute()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                {
                    return CommandResult.Failure("NOT_PLAYING", "Not in Play Mode");
                }

                var startTime = _playModeStartTime;
                if (startTime == default(DateTime))
                {
                    startTime = DateTime.Now;
                }

                EditorApplication.isPlaying = false;

                var duration = (DateTime.Now - startTime).TotalSeconds;

                return CommandResult.SuccessResult(new Dictionary<string, object>
                {
                    ["exited"] = true,
                    ["duration"] = duration
                });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure("EXIT_PLAY_MODE_FAILED", ex.Message);
            }
        }
    }
}
