using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Utility.Animations.ARAnimator;

namespace Awaken.TG.Main.Animations {
    public abstract class ARNpcCustomAnimations : ARCustomAnimations {
        public NpcElement Npc { get; }
        
        protected ARNpcCustomAnimations(NpcElement npc, ShareableARAssetReference animations) : this(npc, animations.Get()) { }
        protected ARNpcCustomAnimations(NpcElement npc, ARAssetReference animations) : base(animations) {
            Npc = npc;
        }
        
        protected override ARNpcAnimancer ExtractAnimancer() {
            if (Npc.HasBeenDiscarded) {
                return null;
            }
            return Npc.Controller.ARNpcAnimancer;
        }
    }
    
    public abstract class ARAnimancerCustomAnimations : ARCustomAnimations {
        public ARNpcAnimancer NpcAnimancer { get; }
        
        protected ARAnimancerCustomAnimations(ARNpcAnimancer npcAnimancer, ShareableARAssetReference animations) : this(npcAnimancer, animations.Get()) { }
        protected ARAnimancerCustomAnimations(ARNpcAnimancer npcAnimancer, ARAssetReference animations) : base(animations) {
            NpcAnimancer = npcAnimancer;
        }
        
        protected override ARNpcAnimancer ExtractAnimancer() {
            if (NpcAnimancer == null) {
                return null;
            }
            return NpcAnimancer;
        }
    }
    
    public abstract class ARCustomAnimations {
        readonly ARAssetReference _animations;

        protected ARAsyncOperationHandle<ARStateToAnimationMapping> _loadingHandle;

        public bool IsLoadingOverrides => _loadingHandle.IsValid() && !_loadingHandle.IsDone;

        internal ARCustomAnimations(ARAssetReference animations) {
            _animations = animations;
        }

        public void LoadOverride() {
            var arAnimancer = ExtractAnimancer();
            
            if (arAnimancer != null && (_animations?.IsSet ?? false)) {
                _loadingHandle = _animations.LoadAsset<ARStateToAnimationMapping>();
                _loadingHandle.OnComplete(OnOverridesLoaded);
            }
        }

        public void UnloadOverride() {
            var animancer = ExtractAnimancer();
            if (_loadingHandle.IsValid() && _loadingHandle.IsDone && animancer != null) {
                animancer.RemoveOverrides(this, _loadingHandle.Result, ReleaseOverridesAsset);
            } else {
                ReleaseOverridesAsset();
            }
        }

        void ReleaseOverridesAsset() {
            _loadingHandle.Release();
            _loadingHandle = default;
        }

        protected abstract void OnOverridesLoaded(ARAsyncOperationHandle<ARStateToAnimationMapping> _);
        protected abstract ARNpcAnimancer ExtractAnimancer();
    }
}