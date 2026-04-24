using System;
using Debug = UnityEngine.Debug;

namespace BitBing.UnityBridge.Editor.Helpers
{
    /// <summary>
    /// Centralized logger, adapted from COPLAY's McpLog. Routes through Unity Debug
    /// AND fires OnLog so the Agent Panel can display log lines without each
    /// component knowing about the panel.
    /// </summary>
    public static class BitBingLog
    {
        public static event Action<string, string, LogLevel> OnLog;

        public enum LogLevel { Info, Warn, Error }

        public static void Info(string source, string message)
        {
            Debug.Log($"[{source}] {message}");
            OnLog?.Invoke(source, message, LogLevel.Info);
        }

        public static void Warn(string source, string message)
        {
            Debug.LogWarning($"[{source}] {message}");
            OnLog?.Invoke(source, message, LogLevel.Warn);
        }

        public static void Error(string source, string message)
        {
            Debug.LogError($"[{source}] {message}");
            OnLog?.Invoke(source, message, LogLevel.Error);
        }
    }
}
