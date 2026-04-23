using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Creates a new GameObject in the Unity scene.
    /// </summary>
    [Serializable]
    public class CreateGameObjectCommand : IAgentCommand
    {
        private readonly string _name;
        private readonly string _parentPath;
        private readonly Vector3 _position;
        private readonly List<string> _components;

        public CreateGameObjectCommand(string name, string parentPath = null, Vector3? position = null, List<string> components = null)
        {
            _name = name ?? "NewGameObject";
            _parentPath = parentPath;
            _position = position ?? Vector3.zero;
            _components = components ?? new List<string>();
        }

        public CommandResult Execute()
        {
            try
            {
                var parent = GetParent(_parentPath);

                var gameObject = new GameObject(_name);
                gameObject.transform.position = _position;

                if (parent != null)
                {
                    gameObject.transform.parent = parent.transform;
                }

                foreach (var componentName in _components)
                {
                    var componentType = GetTypeByName(componentName);
                    if (componentType != null)
                    {
                        gameObject.AddComponent(componentType);
                    }
                }

                return CommandResult.SuccessResult(new Dictionary<string, object>
                {
                    ["gameObjectPath"] = GetGameObjectPath(gameObject),
                    ["name"] = _name
                });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure("CREATE_FAILED", ex.Message);
            }
        }

        private Transform GetParent(string parentPath)
        {
            if (string.IsNullOrEmpty(parentPath)) return null;

            var parent = GameObject.Find(parentPath);
            return parent?.transform;
        }

        private Type GetTypeByName(string typeName)
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

        private string GetGameObjectPath(GameObject obj)
        {
            var path = obj.name;
            var parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
