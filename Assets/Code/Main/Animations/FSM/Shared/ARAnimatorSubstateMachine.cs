using Animancer;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Shared {
    public abstract partial class ARAnimatorSubstateMachine<T> : Element<T> where T : ICharacter {
        // === Fields & Properties
        public abstract string ParentLayerName { get; }
        public Animator Animator { get; }
        public AnimancerComponent Animancer { get; }
        public AnimancerLayer AnimancerLayer { get; private set; }
        public bool IsLayerActive { get; private set; }
        public string AnimationClipName => AnimancerLayer.CurrentState?.Clip?.name;
        protected virtual bool CanBeDisabled => true;
        protected abstract AvatarMask AvatarMask { get; }
        protected abstract int LayerIndex { get; }

        // === Constructor
        protected ARAnimatorSubstateMachine(Animator animator, AnimancerComponent animancer) {
            Animator = animator;
            Animancer = animancer;
        }
        
        // === Initialization
        protected override void OnInitialize() {
            // --- Create animancer layer
            AnimancerLayer = Animancer.Layers[LayerIndex];
            AnimancerLayer.SetMask(AvatarMask);
        }
        
        // === Toggling
        public abstract void EnableFSM();
        public abstract void DisableFSM(bool fromDiscard = false);
        
        protected void BaseEnableFSM(TimeDependent.Update update) {
            // --- Attach to update callbacks
            ParentModel.GetOrCreateTimeDependent().WithUpdate(update);
            AnimancerLayer.SetWeight(1);
            IsLayerActive = true;
        }

        protected void BaseDisableFSM(TimeDependent.Update update, bool fromDiscard = false) {
            ParentModel.GetTimeDependent()?.WithoutUpdate(update);
            World.EventSystem.RemoveAllListenersOwnedBy(this);
            AnimancerLayer.SetWeight(0);
            IsLayerActive = false;
        }
        
        // === States Management
        protected virtual void AfterEnable() { }
        protected virtual void OnEnable() { }
        protected virtual void OnDisable(bool fromDiscard) { }
    }
}