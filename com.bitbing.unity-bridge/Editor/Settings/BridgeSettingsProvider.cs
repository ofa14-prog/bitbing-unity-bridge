using UnityEngine;
using UnityEditor;

namespace BitBing.UnityBridge.Editor.Settings
{
    /// <summary>
    /// Project Settings provider for BridgeSettings.
    /// Registers the settings in Edit > Project Settings > BitBing Unity Bridge.
    /// </summary>
    public static class BridgeSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/BitBing Unity Bridge", SettingsScope.Project)
            {
                label = "BitBing Unity Bridge",
                guiHandler = (searchContext) =>
                {
                    var settings = BridgeSettings.GetOrCreate();
                    var so = new SerializedObject(settings);

                    EditorGUILayout.PropertyField(so.FindProperty("_transportMode"));
                    EditorGUILayout.PropertyField(so.FindProperty("_tcpPort"));
                    EditorGUILayout.PropertyField(so.FindProperty("_pipeName"));
                    EditorGUILayout.PropertyField(so.FindProperty("_mcpPort"));
                    EditorGUILayout.PropertyField(so.FindProperty("_autoConnect"));
                    EditorGUILayout.PropertyField(so.FindProperty("_logVerbose"));
                    EditorGUILayout.PropertyField(so.FindProperty("_timeoutMs"));
                    EditorGUILayout.PropertyField(so.FindProperty("_screenshotDir"));

                    if (so.ApplyModifiedProperties())
                    {
                        settings.Save();
                    }
                },
                keywords = new System.Collections.Generic.HashSet<string>(
                    new[] { "BitBing", "MCP", "Bridge", "Port", "Agent" })
            };
            return provider;
        }
    }
}
