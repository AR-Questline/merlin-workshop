using System.Linq;
using System.Text;
using Awaken.Tests.Performance.Preprocessing;
using Awaken.Tests.Performance.Profilers;
using Awaken.Tests.Performance.TestCases;
using Awaken.Utility.Automation;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.Tests.Performance {
    public class AutomatedPerformanceTest : IAutomation {
        const string Name = "performance";
        
        [RuntimeInitializeOnLoadMethod]
        static void Register() {
            Automations.TryRegister(Name, new AutomatedPerformanceTest());
        }
        
        public async UniTask Run(string[] parameters) {
            if (parameters.Length != 3) {
                Log.Critical?.Error("Invalid performance test parameters");
                return;
            }
            
            var registry = await PerformanceTestRegistry.Load().ToUniTask();
            if (registry == null) {
                Log.Critical?.Error("Cannot load performance test registry");
                return;
            }
            
            registry.Init();
            if (!TryGetVariantsFromCommand(registry, parameters[0], out var variants)) {
                return;
            }
            if (!TryGetTestsFromCommand(registry, parameters[1], out var tests)) {
                return;
            }
            if (!TryGetMatricesFromCommand(registry, parameters[2], out var matrices)) {
                return;
            }

            var manager = new PerformanceTestManager(variants, tests, matrices);
            await manager.Run();
        }
 
        public static string ToCommand(IPerformancePreprocessor[] preprocessors, IPerformancePreprocessorVariant[][] preprocessorVariants, IPerformanceTestCase[] tests, IPerformanceMatrix[] profilers) {
            return $"{IAutomation.Prefix}{IAutomation.Separator0}{Name}{IAutomation.Separator0}{ToCommand(preprocessors, preprocessorVariants)}{IAutomation.Separator0}{ToCommand(tests)}{IAutomation.Separator0}{ToCommand(profilers)}";
        }

        // processor are parsed to given string
        // processor1.variant1.variant2:processor2.variant1.variant2.variant3:processor3.variant1
        
        static string ToCommand(IPerformancePreprocessor[] preprocessors, IPerformancePreprocessorVariant[][] variants) {
            var sb = new StringBuilder();
            for (int i = 0; i < variants.Length; i++) {
                if (i > 0) {
                    sb.Append(IAutomation.Separator1);
                }
                sb.Append(preprocessors[i].Name);
                for (int j = 0; j < variants[i].Length; j++) {
                    sb.Append(IAutomation.Separator2);
                    sb.Append(variants[i][j].Name);
                }
            }
            return sb.ToString();
        }
        
        static bool TryGetVariantsFromCommand(PerformanceTestRegistry registry, string command, out IPerformancePreprocessorVariant[][] variants) {
            var variantsCommand = command.Split(IAutomation.Separator1);
            variants = new IPerformancePreprocessorVariant[variantsCommand.Length][];
            for (int i = 0; i < variantsCommand.Length; i++) {
                var splitted = variantsCommand[i].Split(IAutomation.Separator2);
                var preprocessor = registry.Preprocessors.FirstOrDefault(preprocessor => preprocessor.Name == splitted[0]);
                if (preprocessor == null) {
                    Log.Critical?.Error($"Cannot find preprocessor with name {splitted[0]}");
                    return false;
                }
                variants[i] = new IPerformancePreprocessorVariant[splitted.Length - 1];
                for (int j = 1; j < splitted.Length; j++) {
                    var variant = preprocessor.Variants.FirstOrDefault(variant => variant.Name == splitted[j]);
                    if (variant == null) {
                        Log.Critical?.Error($"Cannot find preprocessor variant of {splitted[0]} with name {splitted[j]}");
                        return false;
                    }
                    variants[i][j-1] = variant;
                }
            }

            return true;
        }

        // tests are parsed to given string
        // test1:test2:test3

        static string ToCommand(IPerformanceTestCase[] tests) {
            var sb = new StringBuilder();
            for (int i = 0; i < tests.Length; i++) {
                if (i > 0) {
                    sb.Append(IAutomation.Separator1);
                }
                sb.Append(tests[i].Name);
            }
            return sb.ToString();
        }
        
        static bool TryGetTestsFromCommand(PerformanceTestRegistry registry, string command, out IPerformanceTestCase[] tests) {
            var testsCommand = command.Split(IAutomation.Separator1);
            tests = new IPerformanceTestCase[testsCommand.Length];
            for (int i = 0; i < testsCommand.Length; i++) {
                var test = registry.TestCases.FirstOrDefault(test => test.Name == testsCommand[i]);
                if (test == null) {
                    Log.Critical?.Error($"Cannot find test with name {testsCommand[i]}");
                    return false;
                }
                tests[i] = test;
            }
            return true;
        }

        // matrices are parsed to given string
        // matrix1:matrix2:matrix3
        
        static string ToCommand(IPerformanceMatrix[] matrices) {
            var sb = new StringBuilder();
            for (int i = 0; i < matrices.Length; i++) {
                if (i > 0) {
                    sb.Append(IAutomation.Separator1);
                }
                sb.Append(matrices[i].Name);
            }
            return sb.ToString();
        }

        static bool TryGetMatricesFromCommand(PerformanceTestRegistry registry, string command, out IPerformanceMatrix[] matrices) {
            var matricesCommand = command.Split(IAutomation.Separator1);
            matrices = new IPerformanceMatrix[matricesCommand.Length];
            for (int i = 0; i < matricesCommand.Length; i++) {
                var matrix = registry.Matrices.FirstOrDefault(matrix =>  matrix.Name == matricesCommand[i]);
                if (matrix == null) {
                    Log.Critical?.Error($"Cannot find matrix with name {matricesCommand[i]}");
                    return false;
                }
                matrices[i] = matrix;
            }
            return true;
        }
    }
}