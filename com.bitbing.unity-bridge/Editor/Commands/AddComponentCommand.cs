using System;
using System.Reflection;
using UnityEngine;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Adds a component to a GameObject.
    /// </summary>
    [Serializable]
    public class AddComponentCommand : IAgentCommand
    {
        private readonly string _gameObjectPath;
        private readonly string _componentType;

        public AddComponentCommand(string gameObjectPath, string componentType)
        {
            _gameObjectPath = gameObjectPath;
            _componentType = componentType;
        }

        public CommandResult Execute()
        {
            if (string.IsNullOrEmpty(_gameObjectPath))
            {
                return CommandResult.Failure("INVALID_PATH", "GameObject path is required");
            }

            if (string.IsNullOrEmpty(_componentType))
            {
                return CommandResult.Failure("INVALID_COMPONENT", "Component type is required");
            }

            try
            {
                var gameObject = GameObject.Find(_gameObjectPath);

                if (gameObject == null)
                {
                    return CommandResult.Failure("NOT_FOUND", $"GameObject not found: {_gameObjectPath}");
                }

                var type = FindType(_componentType);

                if (type == null)
                {
                    return CommandResult.Failure("COMPONENT_NOT_FOUND", $"Component type not found: {_componentType}");
                }

                var component = gameObject.AddComponent(type);

                return CommandResult.SuccessResult(new System.Collections.Generic.Dictionary<string, object>
                {
                    ["componentName"] = component.GetType().Name,
                    ["gameObjectPath"] = _gameObjectPath
                });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure("ADD_COMPONENT_FAILED", ex.Message);
            }
        }

        private Type FindType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == typeName || type.FullName == typeName)
                    {
                        return type;
                    }
                }
            }
            return null;
        }
    }
}
