using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Awaken.TG.Main.AI.Idle.Data.Runtime {
    public sealed partial class InteractionOverride : InteractionSceneSpecificSource {
        public override ushort TypeForSerialization => SavedModels.InteractionOverride;

        [Saved] StoryBookmark _callback;
        [Saved] bool _forgetOnSceneChange;
        IInteractionFinder _fallbackFinder;
        bool _useFallback;

        public override IInteractionFinder Finder => _useFallback ? FallbackFinder : base.Finder;
        IInteractionFinder FallbackFinder => _fallbackFinder ??= new InteractionSpecificFinder(new StayInAbyssInteraction());

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public InteractionOverride() {}

        public InteractionOverride(DeterministicInteractionFinder finder, StoryBookmark callback, string sceneName = null, bool forgetOnSceneChange = false, 
            InteractionStartReason? overridenStartReason = null) : base(finder, sceneName, overridenStartReason) {
            _callback = callback;
            _forgetOnSceneChange = forgetOnSceneChange;
        }

        protected override void OnDifferentSceneEntered() {
            _useFallback = true;
            if (_forgetOnSceneChange) {
                DiscardAndRefresh();
            }
        }

        protected override void OnCorrectSceneEnteredWithoutInteraction() {
            _useFallback = true;
            base.OnCorrectSceneEnteredWithoutInteraction();
        }

        protected override void OnCorrectSceneEnteredWithInteraction(INpcInteraction interaction, bool firstCheck) {
            _useFallback = false;
            base.OnCorrectSceneEnteredWithInteraction(interaction, firstCheck);
        }
        
        protected override void OnInteractionProperlyBooked() {}
        
        protected override void OnInteractionEnded() {
            if (_callback is { IsValid: true }) {
                SendCallback(Location, _callback).Forget();
            }
        }

        static async UniTaskVoid SendCallback(Location location, StoryBookmark callback) {
            await AsyncUtil.DelayFrame(location);
            Story.StartStory(StoryConfig.Location(location, callback, typeof(VDialogue)));
        }
    }
}