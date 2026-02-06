using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Main.Utility.Patchers.Dlc {
    public abstract class DlcPatcher {
        protected abstract DlcCategory RequiredDlcCategory { get; }
        bool HasDlc => SocialService.Get.HasDlc(RequiredDlcCategory);

        public bool CanPatch(DlcCategoryFlags previouslyActiveDlcCategories) {
            bool wasDlcActiveLastTime = previouslyActiveDlcCategories.HasFlagFast(RequiredDlcCategory.ToFlags());
            return HasDlc != wasDlcActiveLastTime;
        }

        public void AfterGameLoadedPatch() {
            if (HasDlc) {
                bool forTheFirstTime = HeroDlcHandler.IsActiveForTheFirstTime(RequiredDlcCategory);
                Log.Marking?.Warning($"DLC Patcher Activated {(forTheFirstTime ? "for the First Time" : "")}: {GetType()}");
                OnDlcActivated(forTheFirstTime);
            } else {
                Log.Marking?.Warning($"DLC Patcher Deactivated: {GetType()}");
                OnDlcDeactivated();
            }
        }

        protected abstract void OnDlcActivated(bool forTheFirstTime);
        protected abstract void OnDlcDeactivated();
        
        // === Conditions
        
        protected static bool FlagCondition(string flag) {
            return StoryFlags.Get(flag);
        }

        protected static bool QuestCompletedCondition(string questGuid) {
            var state = World.Services.Get<GameplayMemory>().Context(questGuid).Get("state", QuestState.NotTaken);
            return state == QuestState.Completed;
        }
        
        protected static bool ItemRequiredCondition(string itemGuid) {
            foreach (var item in Hero.Current.HeroItems.Items) {
                if (item.Template.GUID.Equals(itemGuid)) {
                    return true;
                }
            }
            var storage = Hero.Current.Storage;
            if (storage.IsStashed) {
                foreach (var item in storage.StashedItems) {
                    if (item.ItemTemplate.GUID.Equals(itemGuid)) {
                        return true;
                    }
                }
            } else {
                foreach (var item in storage.Items) {
                    if (item.Template.GUID.Equals(itemGuid)) {
                        return true;
                    }
                }
            }
            
            return false;
        }

        protected static bool WyrdStalkerDeadCondition() {
            return Hero.Current.HeroWyrdNight.WyrdStalker.WyrdStalkerDead;
        }
        
        // === Operations
        
        protected static void GrantItemSet(string[] itemGuids) {
            ItemTemplate[] templates = new ItemTemplate[itemGuids.Length];
            for (int i = 0; i < itemGuids.Length; i++) {
                templates[i] = (new TemplateReference(itemGuids[i])).Get<ItemTemplate>();
            }
            foreach (var template in templates) {
                Hero.Current.HeroItems.AddWithoutNotification(new Item(template));
            }
        }
    }
}