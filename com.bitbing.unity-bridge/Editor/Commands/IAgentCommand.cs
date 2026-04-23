using System;
using System.Collections.Generic;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Base class for command execution results.
    /// </summary>
    [Serializable]
    public class CommandResult
    {
        public bool Success { get; private set; }
        public Dictionary<string, object> Data { get; private set; }
        public string Error { get; private set; }
        public string ErrorCode { get; private set; }

        private CommandResult() { }

        public static CommandResult SuccessResult(Dictionary<string, object> data = null)
        {
            return new CommandResult
            {
                Success = true,
                Data = data ?? new Dictionary<string, object>(),
                Error = null,
                ErrorCode = null
            };
        }

        public static CommandResult Failure(string errorCode, string errorMessage, Dictionary<string, object> data = null)
        {
            return new CommandResult
            {
                Success = false,
                Data = data,
                Error = errorMessage,
                ErrorCode = errorCode
            };
        }
    }

    /// <summary>
    /// Interface for agent commands.
    /// </summary>
    public interface IAgentCommand
    {
        CommandResult Execute();
    }
}
