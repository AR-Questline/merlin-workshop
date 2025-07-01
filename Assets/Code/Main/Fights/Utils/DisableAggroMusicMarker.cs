using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Fights.Utils {
    /// <summary>
    /// Marker class for disabling temporarily combat music.
    /// </summary>
    public partial class DisableAggroMusicMarker : Element<NpcElement> {
        public sealed override bool IsNotSaved => true;

        public new static class Events {
            public static readonly Event<NpcElement, NpcElement> AggroSettingsChanged = new(nameof(AggroSettingsChanged));
        }

        protected override void OnInitialize() {
            var ai = ParentModel.NpcAI;
            if (ai) {
                World.Only<HeroCombat>().UnregisterNearNpcAI(ai);
            }
            ParentModel.Trigger(Events.AggroSettingsChanged, ParentModel);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (ParentModel.HasBeenDiscarded) {
                return;
            }

            ParentModel.Trigger(Events.AggroSettingsChanged, ParentModel);

            var ai = ParentModel.NpcAI;
            // So much checks because safety first, no time to analyse all possible states space
            if (ai && !ai.HasBeenDiscarded && ParentModel.IsAlive && ai.Working && ParentModel.Template.CanTriggerAggroMusic) {
                World.Only<HeroCombat>().RegisterNearNpcAI(ai);
            }
        }
    }
}