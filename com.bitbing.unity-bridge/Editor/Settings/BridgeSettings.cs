using System;
using UnityEngine;
using UnityEditor;

namespace BitBing.UnityBridge.Editor.Settings
{
    /// <summary>
    /// Persistent settings for the Unity Bridge.
    /// Stored in ProjectSettings/AIGameDevBridgeSettings.asset
    /// </summary>
    [Serializable]
    public class BridgeSettings
    {
        public enum TransportMode { Tcp, NamedPipe, Mcp }

        [SerializeField]
        private TransportMode _transportMode = TransportMode.Mcp;

        [SerializeField]
        private int _tcpPort = 57432;

        [SerializeField]
        private string _pipeName = "aigamedev-unity";

        [SerializeField]
        private int _mcpPort = 8080;

        [SerializeField]
        private bool _autoConnect = true;

        [SerializeField]
        private bool _logVerbose = false;

        [SerializeField]
        private int _timeoutMs = 10000;

        [SerializeField]
        private string _screenshotDir = "Temp/AgentScreenshots";

        public TransportMode transportMode => _transportMode;
        public int tcpPort => _tcpPort;
        public string pipeName => _pipeName;
        public int mcpPort => _mcpPort;
        public bool autoConnect => _autoConnect;
        public bool logVerbose => _logVerbose;
        public int timeoutMs => _timeoutMs;
        public string screenshotDir => _screenshotDir;

        private static BridgeSettings s_instance;

        public static BridgeSettings GetOrCreate()
        {
            if (s_instance == null)
            {
                Load();
            }
            return s_instance;
        }

        private static void Load()
        {
            var path = "ProjectSettings/AIGameDevBridgeSettings.asset";
            var assets = AssetDatabase.FindAssets("t:BridgeSettings");

            if (assets.Length > 0)
            {
                s_instance = AssetDatabase.LoadAssetAtPath<BridgeSettings>(
                    AssetDatabase.GUIDToAssetPath(assets[0]));
            }

            if (s_instance == null)
            {
                s_instance = CreateNew();
            }
        }

        private static BridgeSettings CreateNew()
        {
            var directory = "ProjectSettings";
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            var path = $"{directory}/AIGameDevBridgeSettings.asset";
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<BridgeSettings>(), path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<BridgeSettings>(path);
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}
