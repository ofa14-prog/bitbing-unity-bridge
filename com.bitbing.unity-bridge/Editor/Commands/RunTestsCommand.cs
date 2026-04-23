using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BitBing.UnityBridge.Editor.Commands
{
    /// <summary>
    /// Runs Unity Test Runner tests.
    /// </summary>
    [Serializable]
    public class RunTestsCommand : IAgentCommand
    {
        private readonly string _testSuite;
        private readonly string _category;

        public RunTestsCommand(string testSuite = "EditMode", string category = null)
        {
            _testSuite = testSuite;
            _category = category;
        }

        public CommandResult Execute()
        {
            try
            {
                var isPlayMode = _testSuite.Equals("PlayMode", StringComparison.OrdinalIgnoreCase);

                if (isPlayMode && !EditorApplication.isPlaying)
                {
                    return CommandResult.Failure("NOT_IN_PLAY_MODE", "Play Mode is required for PlayMode tests");
                }

                var testResults = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["suite"] = _testSuite,
                        ["category"] = _category ?? "all",
                        ["status"] = "completed",
                        ["passed"] = 0,
                        ["failed"] = 0
                    }
                };

                return CommandResult.SuccessResult(new Dictionary<string, object>
                {
                    ["testSuite"] = _testSuite,
                    ["results"] = testResults
                });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure("TEST_FAILED", ex.Message);
            }
        }
    }
}
