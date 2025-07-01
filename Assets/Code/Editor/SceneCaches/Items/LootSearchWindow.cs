using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Editor.SceneCaches.Core;
using Awaken.TG.Main.General;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Collections;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.SceneCaches.Items {
    public class LootSearchWindow : OdinEditorWindow {
        const string CsvHeaders = "Item,Scene,Grindable,PredictedAmount";
        
        // === Displayed data
        [ShowInInspector, PropertyOrder(-1), EnableGUI, InlineButton(nameof(Bake))]
        public string LastBake => LootCache.Get.LastBake;

        [Header("INPUT"), HideLabel]
        public ItemTemplate itemToAnalyze;
        public float lootChanceMultiplier = 1f;
        public bool allowConditionals;
        public bool allowStealable;
        
        [Header("OUTPUT"), InlineButton(nameof(Copy))]
        public string rawGeneratedInfo = string.Empty;
        
        [Searchable, UsedImplicitly]
        public List<SceneItemSources> rawSceneSources = new();

        [TableList(NumberOfItemsPerPage = 20, ShowPaging = true), Searchable]
        public List<SceneLootInfo> generatedInfo = new();
        
        // === Creators
        [MenuItem("TG/Design/Loot Finder")]
        static void ShowWindow() {
            CreateWindow();
        }

        public static void OpenWindowOn(ItemTemplate itemTemplate) {
            var window = CreateWindow();
            TemplatesUtil.EDITOR_AssignGuid(itemTemplate, itemTemplate.gameObject);
            window.itemToAnalyze = itemTemplate;
            window.Execute();
        }

        static LootSearchWindow CreateWindow() {
            var window = CreateWindow<LootSearchWindow>("Loot Finder");
            window.Show();
            return window;
        }

        // === Buttons
        void Bake() {
            if (EditorUtility.DisplayDialog("Bake Cache", "Are you sure you want to bake the cache? It will take ~10 mionutes", "Yes", "No")) {
                SceneCacheBaker.Bake();
            }
        }
        
        [Button]
        void Execute() {
            rawGeneratedInfo = string.Empty;
            rawSceneSources = new();
            generatedInfo.Clear();
            
            var output = Execute(itemToAnalyze, lootChanceMultiplier, allowConditionals, allowStealable);
            rawGeneratedInfo = output.rawCsv;
            rawSceneSources = output.sceneSources;
            generatedInfo = output.generatedInfoPerScene.Values.OrderByDescending(v => v.predictedAmount).ToList();
        }
        
        // === Execution Logic
        static ItemSetupOutput Execute(ItemTemplate itemToAnalyze, float lootChanceMultiplier, bool allowConditionals, bool allowStealable) {
            if (itemToAnalyze == null) {
                return new();
            }
            var occurrences = LootCache.Get.FindOccurrencesOf(itemToAnalyze).ToList();
            return Execute(itemToAnalyze, occurrences, lootChanceMultiplier, allowConditionals, allowStealable);
        }
        
        static ItemSetupOutput Execute(ItemTemplate itemToAnalyze, List<SceneItemSources> occurrences, float lootChanceMultiplier, bool allowConditionals, bool allowStealable) {
            ItemSetupOutput output = new();
            if (itemToAnalyze == null) {
                return output;
            }

            output.sceneSources = occurrences;

            foreach (var sceneSources in output.sceneSources) {
                var info = GenerateLootDataForScene(sceneSources, lootChanceMultiplier, allowConditionals, allowStealable);
                output.generatedInfoPerScene[sceneSources.SceneName] = info;
            }
            
            StringBuilder rawBuffer = new();
            rawBuffer.AppendLine(CsvHeaders);
            foreach (var lootInfo in output.generatedInfoPerScene.Values.OrderByDescending(v => v.predictedAmount)) {
                rawBuffer.AppendLine($"{itemToAnalyze.name},{lootInfo.sceneName},{lootInfo.grindable},{lootInfo.predictedAmount}");
            }
            output.rawCsv = rawBuffer.ToString();
            return output;
        }

        static SceneLootInfo GenerateLootDataForScene(SceneItemSources sceneSources, float lootChanceMultiplier, bool allowConditionals, bool allowStealable) {
            SceneLootInfo info = new(sceneSources.SceneName);
            foreach (var source in sceneSources.sources) {
                foreach (var item in source.GetItems()) {
                    if ((!allowConditionals && item.Conditional) 
                        || (!allowStealable && item.IsStealable)) {
                        continue;
                    }
                    
                    AppendItemLootDataToSceneLootInfo(item, info, lootChanceMultiplier);
                }
            }

            info.Setup();
            return info;
        }

        public static void AppendItemLootDataToSceneLootInfo(ItemLootData item, SceneLootInfo info, float lootChanceMultiplier) {
            var itemProbability = item.probability;
            if (item.AffectedByLootChanceMultiplier) {
                itemProbability = Mathf.Clamp01(itemProbability * lootChanceMultiplier);
            }
            Probability probability = Probability.AllProbabilities.First(p => p.range.Contains(itemProbability));
            info.probabilityRanges[probability] += item.IntRange;
            float predictedAmount = item.IntRange.Average() * itemProbability;
            if (item.Grindable) {
                info.grindable += predictedAmount;
            }
            info.predictedAmount += predictedAmount;
                        
            int lowestAmountPossible = probability == Probability.Probability100 ? item.IntRange.low : 0;
            info.amountRangeInt += new IntRange(lowestAmountPossible, item.IntRange.high);
        }

        // === Helpers
        void Copy() {
            GUIUtility.systemCopyBuffer = rawGeneratedInfo;
        }

        static string IntRangeToString(IntRange intRange) {
            if (intRange.low == intRange.high) {
                return intRange.low.ToString();
            } else {
                return $"{intRange.low}-{intRange.high}";
            }
        }

        // === Helper Classes
        public class ItemSetupOutput {
            public string rawCsv = string.Empty;
            public List<SceneItemSources> sceneSources = new();
            public OnDemandCache<string, SceneLootInfo> generatedInfoPerScene = new(sceneName => new SceneLootInfo(sceneName));
            
            public int TotalGrindable => (int)generatedInfoPerScene.Values.Sum(i => i.grindable);
            public int TotalPredictedAmount => (int)generatedInfoPerScene.Values.Sum(i => i.predictedAmount);
            public string LootAmountRange {
                get {
                    IntRange amountRange = new();
                    generatedInfoPerScene.Values.ForEach(v => amountRange += v.amountRangeInt);
                    return IntRangeToString(amountRange);
                }
            }

            public int GetTotalPredictedAmountInRegion(string regionScene) {
                SceneRegion region = ScenesCache.Get.regions.FirstOrDefault(r => r.regionScene.Name == regionScene);
                if (region == null) {
                    return 0;
                }
                return (int)generatedInfoPerScene.Values.Where(v => region.All.Any(s => s.Name == v.sceneName)).Sum(i => i.predictedAmount);
            }
        }

        [Serializable, UsedImplicitly]
        public class SceneLootInfo {
            public string sceneName;

            [VerticalGroup("100%"), HideLabel]
            public string probability100;
            [VerticalGroup("67-100%"), HideLabel]
            public string probability67;
            [VerticalGroup("33-67%"), HideLabel]
            public string probability33;
            [VerticalGroup("5-33%"), HideLabel]
            public string probability5;
            [VerticalGroup("0-5%"), HideLabel]
            public string probability0;
            
            public float grindable;
            public float predictedAmount;
            public string amountRange;
            [HideInInspector]
            public IntRange amountRangeInt;

            public OnDemandCache<Probability, IntRange> probabilityRanges = new(_ => new IntRange());

            public SceneLootInfo(string sceneName) {
                this.sceneName = sceneName;
            }

            public void Setup() {
                grindable = (int)grindable;
                predictedAmount = (int)predictedAmount;
                probability100 = IntRangeToString(probabilityRanges[Probability.Probability100]);
                probability67 = IntRangeToString(probabilityRanges[Probability.Probability67]);
                probability33 = IntRangeToString(probabilityRanges[Probability.Probability33]);
                probability5 = IntRangeToString(probabilityRanges[Probability.Probability5]);
                probability0 = IntRangeToString(probabilityRanges[Probability.Probability0]);
                amountRange = IntRangeToString(amountRangeInt);
            }
        }

        public class Probability {
            public FloatRange range;

            public static readonly Probability 
                Probability100 = new() { range = new FloatRange(1f, 1f) },
                Probability67 = new() { range = new FloatRange(0.67f, 1f) },
                Probability33 = new() { range = new FloatRange(0.33f, 0.67f) },
                Probability5 = new() { range = new FloatRange(0.05f, 0.33f) },
                Probability0 = new() { range = new FloatRange(0f, 0.05f) };
            
            public static readonly Probability[] AllProbabilities = { Probability100, Probability67, Probability33, Probability5, Probability0 };

            public override string ToString() {
                if (range.min == range.max) {
                    return range.min.ToString("P0");
                }
                return $"{range.min:P0}-{range.max:P0}";
            }
        }
    }
}