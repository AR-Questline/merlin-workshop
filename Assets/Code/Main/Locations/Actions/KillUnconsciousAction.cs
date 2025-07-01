using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Locations.Actions {
    public sealed partial class KillUnconsciousAction : AbstractLocationAction {
        public sealed override bool IsNotSaved => true;

        readonly UnconsciousElement _unconsciousElement;
        IllegalActionTracker _illegalActionTracker;
        public override string DefaultActionName => LocTerms.KillUnconscious.Translate();

        public override bool IsIllegal => Crime.Murder(ParentModel.Element<NpcElement>()).IsCrime();

        public KillUnconsciousAction(UnconsciousElement element) {
            _unconsciousElement = element;
        }

        protected override void OnFullyInitialized() {
            _illegalActionTracker = Hero.Current.Element<IllegalActionTracker>();
            _unconsciousElement.ListenTo(Model.Events.BeforeDiscarded, Discard, this);
        }
        
        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return _illegalActionTracker.IsCrouching ? ActionAvailability.Disabled : base.GetAvailability(hero, interactable);
        }

        // === Execution
        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (ParentModel.TryGetElement<KillPreventionElement>(out var element)) {
                element.Discard();
            }
            if (ParentModel.TryGetElement(out NpcElement npc)) {
                CommitCrime.Murder(npc);
            }
            _unconsciousElement.ParentModel.Trigger(UnconsciousElement.Events.UnconsciousKilled, _unconsciousElement);
            ParentModel.Kill(hero);
        }
    }
}
