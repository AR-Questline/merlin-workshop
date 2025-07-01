using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Combat;
using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Fights.Duels {
    public partial class NpcDuelistElement : DuelistElement, INpcCombatLeaveBlocker {
        DuelistCrimeDisabler _crimeDisabler;
        bool _removedKilledPrevention;
        NpcElement _parentNpc;
        InteractionOverride _interactionOverride;
        
        public bool BlocksCombatExit => !Defeated;
        public NpcElement NpcElement => _parentNpc;
        NpcElement IElement<NpcElement>.ParentModel => ParentModel as NpcElement;
        
        public NpcDuelistElement(DuelistsGroup group, DuelistSettings settings) : base(group, settings) { }

        protected override void OnInitialize() {
            _parentNpc = ParentModel as NpcElement;
            base.OnInitialize();
            _crimeDisabler = NpcElement.ParentModel.AddElement(new DuelistCrimeDisabler(Settings.fightToDeath, DuelController));
            if (Settings.fightToDeath && NpcElement.ParentModel.TryGetElement<KillPreventionElement>(out var killPrevention)) {
                killPrevention.Discard();
                _removedKilledPrevention = true;
            }
            NpcElement.ListenTo(ICharacter.Events.TryingToExitCombat, OnCombatExitTry, this);
        }

        protected override void InitDeathListener() {
            ParentModel.ListenTo(IAlive.Events.BeforeDeath, OnBeforeDeath, this);
        }
        
        public void ForceIdlePosition(IdlePosition position, IdlePosition forward) {
            var finder = new InteractionStandFinder(position, forward, null);
            _interactionOverride = new InteractionOverride(finder, null, null, true);
            NpcElement.Behaviours.AddOverride(_interactionOverride);
            
        }
        
        protected override void OnDuelStarted() {
            if (!ValidateNpc()) {
                return;
            }
            
            var target = NpcElement.GetOrSearchForTarget();
            if (target == null && GroupId != 0) {
                target = DuelController.FindFirstTargetForNpc(NpcElement, GroupId);
            }
            
            if (target != null) {
                NpcElement.NpcAI.EnterCombatWith(target, true);
            } else {
                NpcElement.NpcAI.AlertStack?.NewPoi(AlertStack.AlertStrength.Max, NpcElement.Coords);
            }
        }

        protected override void OnDefeat(bool forceDefeat) {
            if (forceDefeat || !Settings.fightToDeath) {
                this.AddElement<DefeatedNpcDuelistElement>();
            }
        }

        void OnCombatExitTry(HookResult<ICharacter, ICharacter> hook) {
            if (Defeated || Victorious) {
                return;
            }
            
            var target = NpcElement.GetOrSearchForTarget();
            if (target != null) {
                hook.Prevent();
                return;
            }

            if (DuelController.FindAnyTargetForNpc(NpcElement, GroupId)) {
                hook.Prevent();
                return;
            }
        }
        
        protected override void AfterDuelCleanup() {
            base.AfterDuelCleanup();
            _crimeDisabler?.Discard();
            _interactionOverride?.Discard();
            if (_removedKilledPrevention && NpcElement is { IsAlive: true, HasBeenDiscarded: false, ParentModel: { HasBeenDiscarded: false } location }) {
                location.AddElement<KillPreventionElement>();
            }
        }

        protected override void StopFight() {
            base.StopFight();
            var npcAI = NpcElement.NpcAI;
            if (npcAI.InCombat) {
                npcAI.ExitCombat(true, true, false);
            } else {
                npcAI.AlertStack.Reset();
            }
        }

        bool ValidateNpc() {
            if (Defeated) {
                return false;
            }
            if (ParentModel is null || NpcElement is not { HasBeenDiscarded: false, NpcAI: { Working: true } }) {
                Defeat();
                return false;
            }
            return true;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            _parentNpc = null;
        }
    }
}
