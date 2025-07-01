using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using JetBrains.Annotations;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class QCPortalNameSuggestorTag : IQcSuggestorTag {
        public PortalType type;
        public QCPortalNameSuggestorTag(PortalType type) {
            this.type = type;
        }
    }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class PortalNameAttribute : SuggestorTagAttribute {
        readonly IQcSuggestorTag[] _tags;

        public PortalNameAttribute(PortalType type) {
            _tags = new IQcSuggestorTag[] {new QCPortalNameSuggestorTag(type)};
        }
        public override IQcSuggestorTag[] GetSuggestorTags() {
            return _tags;
        }
    }
    
    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCPortalNameSuggestor : BasicCachedQcSuggestor<string> {
        static SceneReference s_sceneReference;
        static string[][] s_portalNamesByType;
        
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<QCPortalNameSuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new SimplifiedSuggestion(item, toRemove: "Debug_");
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            SceneReference activeSceneRef = World.Services.Get<SceneService>().ActiveSceneRef;
            if (s_sceneReference != activeSceneRef || s_portalNamesByType == null) {
                s_sceneReference = activeSceneRef;
                s_portalNamesByType = GatherPortals(s_sceneReference);
            }

            return s_portalNamesByType[(int) context.GetTag<QCPortalNameSuggestorTag>().type] ?? Enumerable.Empty<string>();
        }

        string[][] GatherPortals(SceneReference sceneReference) {
            var portals = World.All<Portal>()
                               .Where(p => p.CurrentDomain == sceneReference.Domain)
                               .GroupBy(p => p.Type)
                               .ToArray();
            
            
            if (portals.Length == 0) {
                return null;
            }
            
            string[][] result = new string[Enum.GetValues(typeof(PortalType)).Length][];
            
            for (int i = 0; i < portals.Length; i++) {
                result[(int) portals[i].Key] = portals[i].Select(p => p.LocationDebugName).ToArray();
            }

            return result;
        }
    }
}