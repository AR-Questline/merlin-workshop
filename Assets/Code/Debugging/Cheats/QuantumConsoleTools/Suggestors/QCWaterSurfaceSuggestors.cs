using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QFSW.QC;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class QCWaterSurfaceSuggestorTag : IQcSuggestorTag { }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class WaterSurfaceNameAttribute : SuggestorTagAttribute {
        public static readonly IQcSuggestorTag[] Tags = { new QCWaterSurfaceSuggestorTag() };
        public override IQcSuggestorTag[] GetSuggestorTags() => Tags;
    }
    
    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCWaterSurfaceSuggestors : BasicCachedQcSuggestor<string> {
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<QCWaterSurfaceSuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new SimplifiedSuggestion(item);
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            foreach (var waterSurface in GameObject.FindObjectsByType<WaterSurface>(FindObjectsSortMode.None)) {
                yield return waterSurface.name;
            }
        }
    }
    
    public sealed class QCWaterSurfacePropertySuggestorTag : IQcSuggestorTag { }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class WaterSurfacePropertyNameAttribute : SuggestorTagAttribute {
        public static readonly IQcSuggestorTag[] Tags = { new QCWaterSurfacePropertySuggestorTag() };
        public override IQcSuggestorTag[] GetSuggestorTags() => Tags;
    }
    
    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCWaterSurfacePropertyNameSuggestor : BasicCachedQcSuggestor<string> {
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<QCWaterSurfacePropertySuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new SimplifiedSuggestion(item);
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            return QCWaterSurfaceDebugTools.PropertyNames;
        }
    }
}