using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QFSW.QC;

#if !AR_DEBUG && !DEBUG
using Log = Awaken.Utility.Debugging.Log;
#endif

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class VFXSuggestorTag : IQcSuggestorTag { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class VFXNameAttribute : SuggestorTagAttribute {
        static readonly IQcSuggestorTag[] Tags = {new VFXSuggestorTag()};

        public override IQcSuggestorTag[] GetSuggestorTags() {
            return Tags;
        }
    }

    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCDebugVFXCollectionSuggestor : BasicCachedQcSuggestor<string> {
        static List<string> s_vfxTemplates;

        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
#if !AR_DEBUG && !DEBUG
            Log.Important?.Once(Log.Utils.DebugVFXNameSuggestor)?.Error("VFX parameter auto-completion disabled - requires AR_DEBUG or DEBUG environment to be defined"); 
#endif
            // Intentionally still returns true when the VFXSuggestorTag is present (even without AR_DEBUG / DEBUG) so we can emit the explanatory error once the user requests suggestions.
            return context.HasTag<VFXSuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new SimplifiedSuggestion(item, true, null, "VFX");
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
#if !AR_DEBUG && !DEBUG
            Log.Important?.Error("VFX suggestion list loading disabled - requires AR_DEBUG or DEBUG environment to be defined");
            return s_vfxTemplates = new List<string>();
#endif
            if (s_vfxTemplates != null) {
                return s_vfxTemplates;
            }
            
            s_vfxTemplates = new List<string>();
                
            try {
                var vfxCollection = DebugVFXCollectionUtils.DEBUG_GetVfxCollectionWithWaitForCompletion();
                    
                if (vfxCollection) {
                    if (vfxCollection.customPrefabs != null) {
                        foreach (var prefab in vfxCollection.customPrefabs) {
                            if (prefab && !string.IsNullOrEmpty(prefab.name)) {
                                s_vfxTemplates.Add(prefab.name);
                            }
                        }
                    }
                        
                    if (vfxCollection.vfxPrefabs != null) {
                        foreach (var prefab in vfxCollection.vfxPrefabs) {
                            if (prefab && !string.IsNullOrEmpty(prefab.name)) {
                                s_vfxTemplates.Add(prefab.name);
                            }
                        }
                    }
                        
                    if (vfxCollection.vfxAssets != null) {
                        foreach (var asset in vfxCollection.vfxAssets) {
                            if (asset && !string.IsNullOrEmpty(asset.name)) {
                                s_vfxTemplates.Add(asset.name);
                            }
                        }
                    }
                }
                    
                s_vfxTemplates = s_vfxTemplates.Distinct().OrderBy(x => x).ToList();
            } catch {
                s_vfxTemplates = new List<string>();
            }

            return s_vfxTemplates;
        }
    }
}