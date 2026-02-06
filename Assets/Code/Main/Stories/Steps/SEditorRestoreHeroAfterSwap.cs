using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Hero: Restore Visuals and Logic after Swap"), NodeSupportsOdin]
    public class SEditorRestoreHeroAfterSwap : EditorStep {
        public bool cleanCurrentInventory = true;
        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SRestoreHeroAfterSwap() {
                cleanCurrentInventory = cleanCurrentInventory,
            };
        }
    }

    public partial class SRestoreHeroAfterSwap : StoryStep {
        public bool cleanCurrentInventory;

        public override StepResult Execute(Story story) {
            var result = new StepResult();
            RestoreHero(story.Hero, result).Forget();
            return result;
        }

        async UniTaskVoid RestoreHero(Hero hero, StepResult result) {
            var cachedData = World.Any<CachedHeroData>();
            if (cachedData == null) {
                Log.Critical?.Error($"Cached Hero Data is missing and can't be restored.");
                return;
            }
            
            AdvancedNotificationBuffer.AllNotificationsSuspended = true;
            
            var bodyFeatures = hero.BodyFeatures();
            cachedData.RestoreVisuals(hero);
            if (!await hero.View<VHeroController>().TryReloadBodyWithEquips()) {
                return;
            }
            bodyFeatures.Reload();
            if (!await TryCleanInventory(hero)) {
                return;
            }
            cachedData.RestoreItems(hero);
            cachedData.RestoreDevelopment(hero);
            cachedData.Discard();

            SummonUtils.DestroyHeroSummonsExceptPets();
            
            AdvancedNotificationBuffer.AllNotificationsSuspended = false;
            result.Complete();
        }

        async UniTask<bool> TryCleanInventory(Hero hero, CachedHeroData cachedData = null) {
            if (cleanCurrentInventory) {
                return await SSwapHero.CleanInventory(hero, cachedData);
            }
            return true;
        }
    }
}