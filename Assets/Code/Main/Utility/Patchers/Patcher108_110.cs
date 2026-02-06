using System;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Patchers {
    public sealed class Patcher108_110 : Patcher_RestoreOnFastTravelOrSpawn {
        protected override Version MaxInputVersion => new(1, 10, 1);
        protected override Version FinalVersion => new(1, 10, 1);

        public Patcher108_110() : base(new[] {
            CampaignMapForlorn,
            
            SceneByGuid("8257c3562a58a67418c0c91fc73492af"), // Dunegon_CobaltArena, (potwierdzone przez LDLA)
            SceneByGuid("89b79f029daa41e4180082b86dd04c86"), // Dungeon_WarriorGrave,
            SceneByGuid("e15d3f0f734b5d2489d68f15db3b39a6"), // Dungeon_Frig, (potwierdzone przez LDLA)
            SceneByGuid("b754b8c9ffc1a4b409abcef051f65438"), // Dungeon_ElevatorPass, (potwierdzone przez LDLA)
            SceneByGuid("bb12a53963e566e4f89e5a1bef28d125"), // Dungeon_ElevatorPass_02, (potwierdzone przez LDLA)
            SceneByGuid("5e31b93b1a10cc84186385752e505c2f"), // Dungeon_BarrowOfTheHornedShadow,
            SceneByGuid("8ca81510f06df144886f54c1baa010bd"), // Dungeon_Eira.
            SceneByGuid("9c8a0a87405501a4f92babce36d99445"), // Dungeon_PictishHideout,
            SceneByGuid("37dae9061ddf90f4c85dd14c13bb5d68"), // Dungeon_Dodheimsgard2,
            SceneByGuid("5bd1834795d153d49a92e5884666231c"), // Dungeon_WyrdOighreata (potwierdzone przez LDLA)
            SceneByGuid("1281d333972a10e4da649bd1af9300ba"), // Dungeon_Weaver
            SceneByGuid("1831495d94576884cbfba7960f96ed84"), // Dungeon_VolkerCliff (potwierdzone przez LDLA)
            SceneByGuid("af687398f67308c4fa56b270bc75d5a1"), // Dungeon_Ulfr_Interior
            SceneByGuid("7745e73847d00834a8aaece9e39b58bb"), // Dungeon_TricksterAndSympathy
            SceneByGuid("7a47b94ac169cd14c81e4fe631ab041d"), // Dungeon_ThirdGiant
            SceneByGuid("b11dbac45685adb4ab88bb2d206c4097"), // Dungeon_Sveinn_FurdadCave
            SceneByGuid("db03d7cfbc06c7a4fa9f55cdbad595fd"), // Dungeon_KingNoOne
            SceneByGuid("1b9c64e97437d32459579c9cfca9d3a8"), // Dungeon_GrindylowLair
            SceneByGuid("f12895105ac3f944c8d9da5bb503ab9e"), // Dungeon_GiantsBarrow (potwierdzone przez LDLA)
            SceneByGuid("020260f48fa444540a2357cfb7a7b62b"), // Dungeon_Foredwellers (potwierdzone przez LDLA)
            SceneByGuid("f7cc3165ca1c3a9458880244a2b13cad"), // Ending_AlchemistGarden
            SceneByGuid("51533f4a7e0fc034fa541072bc972917"), // Dungeon_Skarnot
            SceneByGuid("d31b2c71115be184cbd26cf5207ba161"), // Dungeon_KnightTomb (potwierdzone przez LDLA)
            SceneByGuid("5a96b302091abef4bb9bee5f4730ad98"), // Dungeon_OmenInFlesh
            SceneByGuid("db6764ff04a75154b8258cd7197bf68c"), // Dungeon_GalahadCrypt
            SceneByGuid("144695d998938824b8794f27f64ce111"), // Dungeon_MinePass
            SceneByGuid("4a2f8a6584b24bf45bcfeb7595d5e6dd"), // Dungeon_Forlorn_OldWyrdstoneMine (potwierdzone przez LDLA)
            SceneByGuid("773bc12faf90f5a44804dc4ca5370c91"), // Dungeon_GrottoOfTheFirstMen (potwierdzone przez LDLA)
            SceneByGuid("e1fe8ea97b0188443a4cd029d21be9b5"), // Dungeon_Sveinn_StagfatherDirt (potwierdzone przez LDLA)
        }) { }
        
        public override async UniTask CheckAllSaveSlots(IProgress<float> progress) {
            if (Configuration.GetBool("saving.ignore_ng_ready_search")) {
                return;
            }
            await NewGamePlusUtils.FindAndFixSavesNotMarkedAsNgReady(progress);
        }
        
        public override bool AfterDeserializedModel(Model model) {
            if (!base.AfterDeserializedModel(model)) {
                return false;
            }
            if (model is GameRealTime gameRealTime) {
                const int MaxYear = 9000;
                if (gameRealTime.WeatherTime.Year > MaxYear) {
                    var currentDate = gameRealTime.WeatherTime.Date;
                    Debug.LogException(new Exception($"GameRealTime.WeatherTime.Year is over {MaxYear}, resetting to {MaxYear}. Current value: {currentDate}. Playtime {gameRealTime.PlayRealTime}"));
                    gameRealTime.DateTimeOverride = new DateTime(MaxYear, currentDate.Month, currentDate.Day, 
                        currentDate.Hour, currentDate.Minute, currentDate.Second, currentDate.Millisecond);
                }
                return true;
            }
            return true;
        }

        public override void AfterGameLoadedPatch() {
            var hero = Hero.Current;
            if (hero == null) {
                Debug.LogException(new Exception("Hero.Current is null in AfterGameLoadedPatch of " + nameof(Patcher108_110)));
                return;
            }
            
            hero.Storage.RequestItems();

            {
                Patcher_ItemUpgradeReverting.Patch110.Apply();

                MarksAllReadableAsRead(hero);
                AddCurrentHeroItemsToKnownItems(hero);
                StatTweak.CleanupObsoleteStatTweaks();

                World.Services.TryGet<UniqueNpcStash>()?.StashAllUnused();

                CommonReferences.Get.OverEncumbranceStatus.TryGet(out StatusTemplate status);
                hero.Statuses.RemoveAllStatus(status);
                hero.Element<HeroEncumbered>().ApplyStatusState(status);
            }
            
            hero.Storage.ReleaseItems();
        }

        static void AddCurrentHeroItemsToKnownItems(Hero hero) {
            // Items were not included in the KnownItems list before this patch.
            // To work around this, we added all the current hero items to the list.
            var heroItems = hero.HeroItems;
            var allHeroItems = heroItems.Items.Concat(hero.Storage.Items);
            
            foreach (var item in allHeroItems) {
                heroItems.KnownItems.Add(item.Template.GUID);
            }
        }
        
        static void MarksAllReadableAsRead(Hero hero) {
            // Assumes that the player has already read any readable items currently in the inventory.
            var heroItems = hero.HeroItems.Items.Concat(hero.Storage.Items);
            
            foreach (var item in heroItems) {
                if (item.TryGetElement(out ItemRead readable) && readable.IsUnread) {
                    readable.MarkAsRead();
                }
            }
        }
    }
}