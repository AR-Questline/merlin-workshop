using System;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Locations.Pets.Variants {
    public partial class NpcAllyPetVariant : PetVariantBase {
        public override ushort TypeForSerialization => SavedModels.NpcAllyPetVariant;

        NpcElement _petNpc;
        NpcElement PetNpc => ParentModel.TryGetCachedElement(ref _petNpc);
        bool _spawningFromMount;
        
        protected override bool CanReduceTimeLeft => !PetNpc?.HasElement<NpcInvolvementOwner>() ?? true;
        
        protected override void WaitForVisualLoaded(Action callback) {
            if (PetNpc == null) {
                Log.Important?.Error($"NpcAllyPetVariant {ParentModel} created without NpcElement.");
                base.WaitForVisualLoaded(callback);
                return;
            }
            
            SetUpNpcSpawnAnimation();
            PetNpc.OnCompletelyInitialized(_ => callback());
        }

        void SetUpNpcSpawnAnimation() {
            _spawningFromMount = TransformationVariant is MountPetVariant;
            
            if (!_spawningFromMount) {
                PetNpc.StartInSpawn = true;
                PetNpc.ListenToLimited(NpcElement.Events.AnimatorEnteredSpawnState, OnNpcInSpawn, this);
            }
        }

        void OnNpcInSpawn() {
            if (TransformationVariant is PetVariant petVariant) {
                var state = PetNpc.GetAnimatorSubstateMachine(NpcFSMType.GeneralFSM).CurrentAnimatorState.CurrentState;
                petVariant.Pet.Controller.Animancer.SyncAnimationWithState(state, ARPetAnimancer.State.Transition);
            }
        }

        protected override void OnBeforeSpawn() {
            if (PetNpc == null) {
                return;
            }
            
            PetNpc.RefreshDistanceBand(0);
            if (_spawningFromMount) {
                PetNpc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.PetVariantTransitionLarge);
            }
        }

        protected override void OnSpawned() {
            if (PetNpc == null) {
                return;
            }
            
            PetNpc.OverrideFaction(PetOwner.GetFactionTemplateForSummon(), FactionOverrideContext.Summon);
            PetNpc.AddElement(new NpcHeroPetAlly(PetOwner));
        }

        protected override void OnPet() {
            PetOwner.Trigger(Hero.Events.HideWeapons, true);
            PetOwner.Trigger(ToolInteractionFSM.Events.PatMount, PetOwner);
            PetNpc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.PetVariantPet);
        }

        protected override void OnFed() {
            PetNpc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.PetVariantFeed);
        }

        protected override void OnBeforeEnd() {
            PetNpc.Movement.InterruptState(new NoMove());
            ParentModel.RemoveElementsOfType<AbstractLocationAction>();

            var stateToEnter = TransformationVariant is MountPetVariant
                ? NpcStateType.PetVariantTransitionLarge
                : NpcStateType.PetVariantTransition;
            
            PetNpc.SetAnimatorState(NpcFSMType.GeneralFSM, stateToEnter);
        }

        protected override void OnEnded() {
            PetNpc?.Discard();
        }
    }
}