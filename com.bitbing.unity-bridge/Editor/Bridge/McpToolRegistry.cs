using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if AIGAMEDEV_NEWTONSOFT
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace BitBing.UnityBridge.Editor
{
    /// <summary>
    /// Registry for MCP tools that map to Unity commands.
    /// Each tool corresponds to a command from EKLENTİR.md §6.3.
    /// </summary>
    public static class McpToolRegistry
    {
        private static readonly Dictionary<string, IMcpTool> s_tools = new Dictionary<string, IMcpTool>();
        private static bool s_initialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize()
        {
            if (s_initialized) return;
            RegisterDefaultTools();
            s_initialized = true;
        }

        private static void RegisterDefaultTools()
        {
            RegisterTool(new CreateGameObjectTool());
            RegisterTool(new DeleteGameObjectTool());
            RegisterTool(new AddComponentTool());
            RegisterTool(new WriteScriptTool());
            RegisterTool(new CompileScriptsTool());
            RegisterTool(new CreateSceneTool());
            RegisterTool(new OpenSceneTool());
            RegisterTool(new EnterPlayModeTool());
            RegisterTool(new ExitPlayModeTool());
            RegisterTool(new TakeScreenshotTool());
            RegisterTool(new RunTestsTool());

            Debug.Log($"[McpToolRegistry] Registered {s_tools.Count} tools");
        }

        public static void RegisterTool(IMcpTool tool)
        {
            s_tools[tool.Name] = tool;
        }

        public static List<object> GetToolList()
        {
            var tools = new List<object>();

            foreach (var kvp in s_tools)
            {
                tools.Add(kvp.Value.GetSchema());
            }

            return tools;
        }

        public static object CallTool(object parameters)
        {
            if (parameters == null)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "No parameters provided"
                };
            }

            string toolName = null;
            Dictionary<string, object> args = null;

            try
            {
                var paramObj = parameters as JObject;
                if (paramObj != null)
                {
                    toolName = paramObj["name"]?.ToString();
                    args = paramObj["arguments"]?.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                }
                else if (parameters is Dictionary<string, object> dict)
                {
                    toolName = dict["name"]?.ToString();
                    args = dict["arguments"] as Dictionary<string, object> ?? dict;
                }
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = $"Failed to parse parameters: {ex.Message}"
                };
            }

            if (string.IsNullOrEmpty(toolName))
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Tool name not provided"
                };
            }

            if (!s_tools.TryGetValue(toolName, out var tool))
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = $"Tool not found: {toolName}"
                };
            }

            try
            {
                var result = tool.Execute(args);
                return result;
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = ex.Message
                };
            }
        }
    }

    /// <summary>
    /// Interface for MCP tools.
    /// </summary>
    public interface IMcpTool
    {
        string Name { get; }
        string Description { get; }
        List<McpToolParameter> Parameters { get; }
        Dictionary<string, object> Execute(Dictionary<string, object> args);
        object GetSchema();
    }

    public class McpToolParameter
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }

        [JsonProperty("default")]
        public object Default { get; set; }
    }

    #region Tool Implementations

    public class CreateGameObjectTool : IMcpTool
    {
        public string Name => "create_gameobject";
        public string Description => "Creates a new GameObject in the Unity scene";
        public List<McpToolParameter> Parameters => new List<McpToolParameter>
        {
            new McpToolParameter { Name = "name", Type = "string", Description = "GameObject name", Required = true },
            new McpToolParameter { Name = "parentPath", Type = "string", Description = "Parent path in hierarchy", Required = false },
            new McpToolParameter { Name = "position", Type = "object", Description = "World position {x, y, z}", Required = false },
            new McpToolParameter { Name = "components", Type = "array", Description = "Component type names to add", Required = false }
        };

        public Dictionary<string, object> Execute(Dictionary<string, object> args)
        {
            var name = args.GetValueOrDefault("name")?.ToString() ?? "NewGameObject";
            var parentPath = args.GetValueOrDefault("parentPath")?.ToString();

            Vector3 position = Vector3.zero;
            if (args.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
            {
                float x = posDict.GetValueOrDefault("x") is float fx ? fx : 0f;
                float y = posDict.GetValueOrDefault("y") is float fy ? fy : 0f;
                float z = posDict.GetValueOrDefault("z") is float fz ? fz : 0f;
                position = new Vector3(x, y, z);
            }

            var components = new List<string>();
            if (args.TryGetValue("components", out var compObj) && compObj is List<object> compList)
            {
                foreach (var c in compList)
                    components.Add(c.ToString());
            }

            var command = new Commands.CreateGameObjectCommand(name, parentPath, position, components);
            var result = command.Execute();

            return new Dictionary<string, object>
            {
                ["success"] = result.Success,
                ["data"] = result.Data,
                ["error"] = result.Error
            };
        }

        public object GetSchema() => new
        {
            name = Name,
            description = Description,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string", description = "GameObject name" },
                    parentPath = new { type = "string", description = "Parent path in hierarchy" },
                    position = new { type = "object", description = "World position {x, y, z}" },
                    components = new { type = "array", description = "Component type names to add" }
                }
            }
        };
    }

    public class DeleteGameObjectTool : IMcpTool
    {
        public string Name => "delete_gameobject";
        public string Description => "Deletes a GameObject from the Unity scene";
        public List<McpToolParameter> Parameters => new List<McpToolParameter>
        {
            new McpToolParameter { Name = "path", Type = "string", Description = "GameObject path in hierarchy", Required = true }
        };

        public Dictionary<string, object> Execute(Dictionary<string, object> args)
        {
            var path = args.GetValueOrDefault("path")?.ToString();
            var command = new Commands.DeleteGameObjectCommand(path);
            var result = command.Execute();

            return new Dictionary<string, object>
            {
                ["success"] = result.Success,
                ["data"] = result.Data,
                ["error"] = result.Error
            };
        }

        public object GetSchema() => new
        {
            name = Name,
            description = Description,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "GameObject path in hierarchy" }
                },
                required = new[] { "path" }
            }
        };
    }

    public class AddComponentTool : IMcpTool
    {
        public string Name => "add_component";
        public string Description => "Adds a component to a GameObject";
        public List<McpToolParameter> Parameters => new List<McpToolParameter>
        {
            new McpToolParameter { Name = "gameObjectPath", Type = "string", Description = "GameObject path", Required = true },
            new McpToolParameter { Name = "componentType", Type = "string", Description = "Component type name", Required = true }
        };

        public Dictionary<string, object> Execute(Dictionary<string, object> args)
        {
            var gameObjectPath = args.GetValueOrDefault("gameObjectPath")?.ToString();
            var componentType = args.GetValueOrDefault("componentType")?.ToString();
            var command = new Commands.AddComponentCommand(gameObjectPath, componentType);
            var result = command.Execute();

            return new Dictionary<string, object>
            {
                ["success"] = result.Success,
                ["data"] = result.Data,
                ["error"] = result.Error
            };
        }

        public object GetSchema() => new
        {
            name = Name,
            description = Description,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    gameObjectPath = new { type = "string", description = "GameObject path" },
                    componentType = new { type = "string", description = "Component type name" }
                },
                required = new[] { "gameObjectPath", "componentType" }
            }
        };
    }

    public class WriteScriptTool : IMcpTool
    {
        public string Name => "write_script";
        public string Description => "Writes a C# script file and optionally triggers compilation";
        public List<McpToolParameter> Parameters => new List<McpToolParameter>
        {
            new McpToolParameter { Name = "path", Type = "string", Description = "Script path ( Assets/...)", Required = true },
            new McpToolParameter { Name = "content", Type = "string", Description = "Script content", Required = true },
            new McpToolParameter { Name = "refreshAssets", Type = "boolean", Description = "Refresh asset database", Required = false, Default = true },
            new McpToolParameter { Name = "waitForCompile", Type = "boolean", Description = "Wait for compilation to complete", Required = false, Default = false }
        };

        public Dictionary<string, object> Execute(Dictionary<string, object> args)
        {
            var path = args.GetValueOrDefault("path")?.ToString();
            var content = args.GetValueOrDefault("content")?.ToString();
            var refreshAssets = args.GetValueOrDefault("refreshAssets") is bool r ? r : true;
            var waitForCompile = args.GetValueOrDefault("waitForCompile") is bool w ? w : false;

            var command = new Commands.WriteScriptCommand(path, content, refreshAssets, waitForCompile);
            var result = command.Execute();

            return new Dictionary<string, object>
            {
                ["success"] = result.Success,
                ["data"] = result.Data,
                ["error"] = result.Error
            };
        }

        public object GetSchema() => new
        {
            name = Name,
            description = Description,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Script path (must be in Assets/)" },
                    content = new { type = "string", description = "Script content" },
                    refreshAssets = new { type = "boolean", description = "Refresh asset database" },
                    waitForCompile = new { type = "boolean", description = "Wait for compilation to complete" }
                },
                required = new[] { "path", "content" }
            }
        };
    }

    public class CompileScriptsTool : IMcpTool
    {
        public string Name => "compile_scripts";
        public string Description => "Triggers script compilation";
        public List<McpToolParameter> Parameters => new List<McpToolParameter>();

        public Dictionary<string, object> Execute(Dictionary<string, object> args)
        {
            var command = new Commands.CompileScriptsCommand();
            var result = command.Execute();

            return new Dictionary<string, object>
            {
                ["success"] = result.Success,
                ["data"] = result.Data,
                ["error"] = result.Error
            };
        }

        public object GetSchema() => new
        {
            name = Name,
            description = Description,
            inputSchema = new { type = "object", properties = new { } }
        };
    }

    public class CreateSceneTool : IMcpTool
    {
        public string Name => "create_scene";
        public string Description => "Creates a new Unity scene";
        public List<McpToolParameter> Parameters => new List<McpToolParameter>
        {
            new McpToolParameter { Name = "path", Type = "string", Description = "Scene path", Required = true }
        };

        public Dictionary<string, object> Execute(Dictionary<string, object> args)
        {
            var path = args.GetValueOrDefault("path")?.ToString();
            var command = new Commands.CreateSceneCommand(path);
            var result = command.Execute();

            return new Dictionary<string, object>
            {
                ["success"] = result.Success,
                ["data"] = result.Data,
                ["error"] = result.Error
            };
        }

        public object GetSchema() => new
        {
            name = Name,
            description = Description,
            inputSchema = new
            {
                type = "object",
                properties = new { path = new { type = "string", description = "Scene path" } },
                required = new[] { "path" }
            }
        };
    }

    public class OpenSceneTool : IMcpTool
    {
        public string Name => "open_scene";
        public string Description => "Opens an existing Unity scene";
        public List<McpToolParameter> Parameters => new List<McpToolParameter>
        {
            new McpToolParameter { Name = "path", Type = "string", Description = "Scene path", Required = true }
        };

        public Dictionary<string, object> Execute(Dictionary<string, object> args)
        {
            var path = args.GetValueOrDefault("path")?.ToString();
            var command = new Commands.OpenSceneCommand(path);
            var result = command.Execute();

            return new Dictionary<string, object>
            {
                ["success"] = result.Success,
                ["data"] = result.Data,
                ["error"] = result.Error
            };
        }

        public object GetSchema() => new
        {
            name = Name,
            description = Description,
            inputSchema = new
            {
                type = "object",
                properties = new { path = new { type = "string", description = "Scene path" } },
                required = new[] { "path" }
            }
        };
    }

    public class EnterPlayModeTool : IMcpTool
    {
        public string Name => "enter_play_mode";
        public string Description => "Enters Unity Play Mode";
        public List<McpToolParameter> Parameters => new List<McpToolParameter>
        {
            new McpToolParameter { Name = "waitForLoad", Type = "boolean", Description = "Wait for scene to load", Required = false, Default = true },
            new McpToolParameter { Name = "timeoutSeconds", Type = "number", Description = "Timeout in seconds", Required = false, Default = 30 }
        };

        public Dictionary<string, object> Execute(Dictionary<string, object> args)
        {
            var waitForLoad = args.GetValueOrDefault("waitForLoad") is bool w ? w : true;
            var timeoutSeconds = args.GetValueOrDefault("timeoutSeconds") is float t ? t : 30f;

            var command = new Commands.EnterPlayModeCommand(waitForLoad, timeoutSeconds);
            var result = command.Execute();

            return new Dictionary<string, object>
            {
                ["success"] = result.Success,
                ["data"] = result.Data,
                ["error"] = result.Error
            };
        }

        public object GetSchema() => new
        {
            name = Name,
            description = Description,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    waitForLoad = new { type = "boolean", description = "Wait for scene to load" },
                    timeoutSeconds = new { type = "number", description = "Timeout in seconds" }
                }
            }
        };
    }

    public class ExitPlayModeTool : IMcpTool
    {
        public string Name => "exit_play_mode";
        public string Description => "Exits Unity Play Mode";
        public List<McpToolParameter> Parameters => new List<McpToolParameter>();

        public Dictionary<string, object> Execute(Dictionary<string, object> args)
        {
            var command = new Commands.ExitPlayModeCommand();
            var result = command.Execute();

            return new Dictionary<string, object>
            {
                ["success"] = result.Success,
                ["data"] = result.Data,
                ["error"] = result.Error
            };
        }

        public object GetSchema() => new
        {
            name = Name,
            description = Description,
            inputSchema = new { type = "object", properties = new { } }
        };
    }

    public class TakeScreenshotTool : IMcpTool
    {
        public string Name => "take_screenshot";
        public string Description => "Takes a screenshot of the Game view";
        public List<McpToolParameter> Parameters => new List<McpToolParameter>
        {
            new McpToolParameter { Name = "outputPath", Type = "string", Description = "Output file path", Required = true },
            new McpToolParameter { Name = "width", Type = "number", Description = "Screenshot width", Required = false, Default = 1920 },
            new McpToolParameter { Name = "height", Type = "number", Description = "Screenshot height", Required = false, Default = 1080 },
            new McpToolParameter { Name = "includeUI", Type = "boolean", Description = "Include UI in screenshot", Required = false, Default = true }
        };

        public Dictionary<string, object> Execute(Dictionary<string, object> args)
        {
            var outputPath = args.GetValueOrDefault("outputPath")?.ToString();
            var width = args.GetValueOrDefault("width") is float w ? (int)w : 1920;
            var height = args.GetValueOrDefault("height") is float h ? (int)h : 1080;
            var includeUI = args.GetValueOrDefault("includeUI") is bool u ? u : true;

            var command = new Commands.TakeScreenshotCommand(outputPath, width, height, includeUI);
            var result = command.Execute();

            return new Dictionary<string, object>
            {
                ["success"] = result.Success,
                ["data"] = result.Data,
                ["error"] = result.Error
            };
        }

        public object GetSchema() => new
        {
            name = Name,
            description = Description,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    outputPath = new { type = "string", description = "Output file path" },
                    width = new { type = "number", description = "Screenshot width" },
                    height = new { type = "number", description = "Screenshot height" },
                    includeUI = new { type = "boolean", description = "Include UI in screenshot" }
                },
                required = new[] { "outputPath" }
            }
        };
    }

    public class RunTestsTool : IMcpTool
    {
        public string Name => "run_tests";
        public string Description => "Runs Unity Test Runner tests";
        public List<McpToolParameter> Parameters => new List<McpToolParameter>
        {
            new McpToolParameter { Name = "testSuite", Type = "string", Description = "Test suite (EditMode/PlayMode)", Required = false, Default = "EditMode" },
            new McpToolParameter { Name = "category", Type = "string", Description = "Test category filter", Required = false }
        };

        public Dictionary<string, object> Execute(Dictionary<string, object> args)
        {
            var testSuite = args.GetValueOrDefault("testSuite")?.ToString() ?? "EditMode";
            var category = args.GetValueOrDefault("category")?.ToString();

            var command = new Commands.RunTestsCommand(testSuite, category);
            var result = command.Execute();

            return new Dictionary<string, object>
            {
                ["success"] = result.Success,
                ["data"] = result.Data,
                ["error"] = result.Error
            };
        }

        public object GetSchema() => new
        {
            name = Name,
            description = Description,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    testSuite = new { type = "string", description = "Test suite (EditMode/PlayMode)" },
                    category = new { type = "string", description = "Test category filter" }
                }
            }
        };
    }

    #endregion
}
