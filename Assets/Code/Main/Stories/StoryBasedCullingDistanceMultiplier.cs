using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Stories {
    public partial class StoryBasedCullingDistanceMultiplier : Element<Story>, ICullingDistanceModifier {
        public sealed override bool IsNotSaved => true;

        public float ModifierValue { get; }
        public bool AllowMultiplierClamp { get; }

        public static void Create(Story api, float multiplierValue, bool allowMultiplierClamp) {
            api.RemoveElementsOfType<StoryBasedCullingDistanceMultiplier>();
            var instance = new StoryBasedCullingDistanceMultiplier(multiplierValue, allowMultiplierClamp);
            api.AddElement(instance);
        }

        public static void Remove(Story api) {
            api.RemoveElementsOfType<StoryBasedCullingDistanceMultiplier>();
        }
        
        StoryBasedCullingDistanceMultiplier(float multiplierValue, bool allowMultiplierClamp) {
            ModifierValue = multiplierValue;
            AllowMultiplierClamp = allowMultiplierClamp;
        }

        protected override void OnInitialize() {
            World.Services.Get<CullingDistanceMultiplierService>().RegisterGlobalModifier(this);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            World.Services.Get<CullingDistanceMultiplierService>().UnregisterGlobalModifier(this);
        }
    }
}
