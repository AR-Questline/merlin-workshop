using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Main.Heroes.Stats;
using JetBrains.Annotations;
using QFSW.QC;
using Sirenix.Utilities;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class StatTypeParser : BasicQcParser<StatType> {
        public override StatType Parse(string value) {
            var split = value.Split('_');
            return QCStatTypeSuggestor.AllStatTypes.FirstOrDefault(statType => statType.EnumName == split[0] && statType.GetType().Name == split[1]);
        }
    }
    
    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCStatTypeSuggestor : BasicCachedQcSuggestor<StatType> {
        static List<StatType> s_allStatTypes;
        static readonly Type BaseType = typeof(StatType);
        
        public static List<StatType> AllStatTypes => s_allStatTypes ??= GetStatTypes();
        
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.TargetType == BaseType;
        }

        protected override IQcSuggestion ItemToSuggestion(StatType item) {
            return new SimplifiedSuggestion(item.EnumName + "_" + item.GetType().Name, toRemove: "StatType");
        }

        protected override IEnumerable<StatType> GetItems(SuggestionContext context, SuggestorOptions options) {
            return s_allStatTypes ??= GetStatTypes();
        }
        
        static List<StatType> GetStatTypes() {
            var allStatTypes = Assembly.GetAssembly(BaseType).GetExportedTypes()
                                       .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(BaseType));
            // get all static readonly definitions of StatType in allStatTypes and get the id field of each
            
            return allStatTypes.SelectMany(statTypes => statTypes.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy))
                               //.Where(field => field.FieldType.InheritsFrom(BaseType))
                               .Select(field => (StatType) field.GetValue(null))
                               .Distinct()
                               .ToList();
        }
    }
}