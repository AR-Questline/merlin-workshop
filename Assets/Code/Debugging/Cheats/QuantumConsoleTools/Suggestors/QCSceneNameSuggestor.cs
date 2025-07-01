using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Scenes.SceneConstructors;
using JetBrains.Annotations;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class QCSceneNameSuggestorTag : IQcSuggestorTag { }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class SceneNameAttribute : SuggestorTagAttribute {
        static readonly IQcSuggestorTag[] Tags = {new QCSceneNameSuggestorTag()};

        public override IQcSuggestorTag[] GetSuggestorTags() {
            return Tags;
        }
    }
    
    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCSceneNameSuggestor : BasicCachedQcSuggestor<string> {
        static string[] s_sceneNames;
        
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<QCSceneNameSuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new SimplifiedSuggestion(item);
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            s_sceneNames ??= CommonReferences.Get.SceneConfigs.AllScenes.Select(s => s.sceneName).Where(n => !n.StartsWith("CM_")).ToArray();
            return s_sceneNames;
        }
    }
}