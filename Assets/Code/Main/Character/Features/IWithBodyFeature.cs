using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Character.Features {
    public interface IWithBodyFeature : IItemOwner {
        IBaseClothes<IItemOwner> Clothes { get; }
        IView BodyView { get; }
    }

    public static class WithBodyFeatureExtension {
        public static BodyFeatures BodyFeatures(this IWithBodyFeature self) {
            return self.Element<BodyFeatures>();
        }

        public static Gender GetGender(this IWithBodyFeature self) {
            return self.TryGetElement<BodyFeatures>()?.Gender ?? Gender.None;
        }
    }
}