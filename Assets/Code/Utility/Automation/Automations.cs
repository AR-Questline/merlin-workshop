using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.Utility.Automation {
    public static class Automations {
        static readonly Dictionary<string, IAutomation> Registry = new();

        static string[] s_arguments;

        public static bool HasAutomation { get; private set; }

        public static void TryRegister(string name, IAutomation automation) {
            Registry.TryAdd(name, automation);
        }

        public static void Prepare() {
            var rawArgs = Environment.GetCommandLineArgs();
            s_arguments = rawArgs.Where(IsAutomationArgument).ToArray();
            HasAutomation = s_arguments.Length > 0;
        }

        public static async UniTask Run() {
            foreach (var argument in s_arguments) {
                if (!IsAutomationArgument(argument)) {
                    continue;
                }
                
                var splitted = argument.Split(IAutomation.Separator0);
                if (splitted.Length < 2) {
                    Log.Critical?.Error($"Invalid automation parameter: {argument}");
                    continue;
                }

                if (!Registry.TryGetValue(splitted[1], out var automation)) {
                    Log.Critical?.Error($"Invalid automation parameter: {argument}");
                    continue;
                }
                
                await automation.Run(splitted[2..]);
            }
        }

        public static void Finish() {
            s_arguments = null;
            HasAutomation = false;
        }

        static bool IsAutomationArgument(string argument) {
            return argument.StartsWith(IAutomation.Prefix + IAutomation.Separator0);
        }
    }
}