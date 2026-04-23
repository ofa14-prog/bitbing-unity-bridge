using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Triggers script compilation in Unity.
    /// </summary>
    [Serializable]
    public class CompileScriptsCommand : IAgentCommand
    {
        public CommandResult Execute()
        {
            try
            {
                if (EditorApplication.isCompiling)
                {
                    return CommandResult.Failure("ALREADY_COMPILING", "Compilation is already in progress");
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                return CommandResult.SuccessResult(new Dictionary<string, object>
                {
                    ["status"] = "compilation_started"
                });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure("COMPILE_ERROR", ex.Message);
            }
        }
    }
}
