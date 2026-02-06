using Awaken.TG.Main.SocialServices;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroDlcHandler : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroDlcHandler;

        DlcCategoryFlags _previousCategoriesAtInitialize;
        [Saved] DlcCategoryFlags _dlcCategoriesAtInitialize;
        [Saved] DlcCategoryFlags _allDlcThatWereActiveAtLeastOnce;
        DlcCategoryFlags _firstTimeActive;
        
        static HeroDlcHandler TryGetOrCreate() => Hero.Current is { } hero 
                                                        ? hero.TryGetElement(out HeroDlcHandler handler) 
                                                            ? handler 
                                                            : hero.AddElement(new HeroDlcHandler()) 
                                                        : null;

        public static DlcCategoryFlags PreviousCategoriesAtInitialize => TryGetOrCreate()?._previousCategoriesAtInitialize ?? DlcCategoryFlags.None;
 
        protected override void OnInitialize() {
            _previousCategoriesAtInitialize = _dlcCategoriesAtInitialize;
            _dlcCategoriesAtInitialize = DlcCategoryExtensions.AllCurrentlyActive();
            UpdateDlcsThatWereActiveAtLeastOnce();
        }
        
        void UpdateDlcsThatWereActiveAtLeastOnce() {
            var beforeCache = _allDlcThatWereActiveAtLeastOnce;
            _allDlcThatWereActiveAtLeastOnce |= _dlcCategoriesAtInitialize;
            _firstTimeActive = _allDlcThatWereActiveAtLeastOnce & ~beforeCache;
        }

        public static DlcCategoryFlags GetDlcsActiveInLastPlaythrough() {
            var dlcHandler = TryGetOrCreate();
            if (dlcHandler == null) {
                return DlcCategoryExtensions.AllCurrentlyActive();
            }
            return DlcCategoryExtensions.AllCurrentlyActive() | dlcHandler._dlcCategoriesAtInitialize;
        }

        public static bool IsActiveForTheFirstTime(DlcCategory category) {
            var dlcHandler = TryGetOrCreate();
            if (dlcHandler == null) {
                return false;
            }
            return dlcHandler._firstTimeActive.HasFlagFast(category.ToFlags());
        }
    }
}