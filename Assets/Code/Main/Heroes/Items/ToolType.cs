using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Localization;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.Items {
    public class ToolType : RichEnum {
        public LocString DisplayName { get; }
        public LocString InteractionName { get; }
        public bool CanHeroActionBeDisabled { get; }
        public ProfStatType RelatedProficiency { [UnityEngine.Scripting.Preserve] get; }
        
        protected ToolType(string enumName, string displayName, string interactionName, bool canHeroActionBeDisabled, ProfStatType relatedProficiency = null) : base(enumName) {
            this.RelatedProficiency = relatedProficiency;
            DisplayName = new LocString {ID = displayName};
            InteractionName = new LocString {ID = interactionName};
            CanHeroActionBeDisabled = canHeroActionBeDisabled;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static readonly ToolType
            Mining = new(nameof(Mining), LocTerms.Mining, LocTerms.Mine, true),
            Digging = new(nameof(Digging), LocTerms.Digging, LocTerms.Dig, true),
            Gathering = new(nameof(Gathering), LocTerms.Gathering, LocTerms.Gather, false),
            Fishing = new(nameof(Fishing), LocTerms.Fishing, LocTerms.Fish, true),
            Lumbering = new(nameof(Lumbering), LocTerms.Lumbering, LocTerms.Lumber, true),
            Sketching = new(nameof(Sketching), LocTerms.Sketching, LocTerms.Sketch, false),
            Spyglassing = new(nameof(Spyglassing), LocTerms.Spyglassing, LocTerms.Look, false);
    }
}