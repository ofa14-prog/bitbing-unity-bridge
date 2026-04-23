using UnityEngine;
using UnityEditor;

namespace BitBing.UnityBridge.Editor.Settings
{
    /// <summary>
    /// Project Settings provider for BridgeSettings.
    /// Registers the settings in Unity's Project Settings menu.
    /// </summary>
    public class BridgeSettingsProvider
    {
        private const string MenuPath = "Project Settings/AI GameDev Bridge";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            var settings = BridgeSettings.GetOrCreate();
            if (settings.autoConnect)
            {
                // Auto-start MCP listener when project loads
            }
        }
    }
}
