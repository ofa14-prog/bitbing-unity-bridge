using System;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Marks a class as a BitBing tool/command, auto-discovered by CommandRegistry.
    /// Adapted from COPLAY's McpForUnityToolAttribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BitBingCommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; set; }
        public string Group { get; set; } = "core";

        public BitBingCommandAttribute(string name, string description = null)
        {
            Name = name;
            Description = description;
        }
    }
}
