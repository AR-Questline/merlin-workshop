using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using JetBrains.Annotations;
using QFSW.QC;
using Sirenix.Utilities;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class TemplateSuggestorTag : IQcSuggestorTag {
        public Type Type { get; }
        public TemplateTypeFlag Flag { get; }
        public TemplateSuggestorTag(Type type, TemplateTypeFlag flag = TemplateTypeFlag.Regular) {
            Type = type;
            Flag = flag;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class TemplateSuggestionAttribute : SuggestorTagAttribute {
        readonly IQcSuggestorTag[] _tags;

        public override IQcSuggestorTag[] GetSuggestorTags() => _tags;

        public TemplateSuggestionAttribute(Type type, TemplateTypeFlag flag = TemplateTypeFlag.Regular) {
            _tags = new IQcSuggestorTag[] {new TemplateSuggestorTag(type, flag)};
        }
    }

    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCTemplateSuggestor : BasicCachedQcSuggestor<ITemplate> {
        static readonly OnDemandCache<(Type, TemplateTypeFlag), List<ITemplate>> Templates = new(FactoryFunction);

        static List<ITemplate> FactoryFunction((Type type, TemplateTypeFlag templateType) type) {
            return World.Services.Get<TemplatesProvider>()
                .GetAllOfType(type.type, type.templateType)
                .Where(t => !t.IsAbstract)
                .ToList();
        }

        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<TemplateSuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(ITemplate item) {
            return new SimplifiedSuggestion(item.DebugName, true, "Template_");
        }

        protected override IEnumerable<ITemplate> GetItems(SuggestionContext context, SuggestorOptions options) {
            var templateSuggestorTag = context.GetTag<TemplateSuggestorTag>();
            return Templates[(templateSuggestorTag.Type, templateSuggestorTag.Flag)];
        }
    }
}