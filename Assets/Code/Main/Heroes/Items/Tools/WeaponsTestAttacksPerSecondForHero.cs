using System;
using System.Linq;
using Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using QFSW.QC;
using UnityEngine;
using Log = Awaken.Utility.Debugging.Log;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Heroes.Items.Tools {
    public static class WeaponsTestAttacksPerSecondForHero {
        const int TestAttacksCount = 10;
        const float BreakDuration = 5f;
        const int ClickFrameInterval = 3;
        const float TestTmeScale = 1f;

        static Location s_target;
        static SaveBlocker s_saveBlocker;
        static float s_staminaCache, s_manaCache, s_healthCache;

        static void PrepareTest() {
            var rotation = Quaternion.LookRotation(Hero.Current.Forward());
            var position = Hero.Current.Coords + Hero.Current.Forward() * 1.5f;
            s_target = CommonReferences.Get.TestDummy.SpawnLocation(position, rotation);
            s_target.Element<AliveLocation>().HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, outcome => outcome.Target.Health.SetToFull(), s_target);

            s_saveBlocker = World.Add(new SaveBlocker("AttackPerSecondTest"));
            
            var hero = Hero.Current;
            s_staminaCache = hero.MaxStamina.BaseValue;
            hero.MaxStamina.SetTo(99999f);
            hero.Stamina.SetTo(99999f);
            s_manaCache = hero.MaxMana.BaseValue;
            hero.MaxMana.SetTo(99999f);
            hero.Mana.SetTo(99999f);
            s_healthCache = hero.MaxHealth.BaseValue;
            hero.MaxHealth.SetTo(99999f);
            hero.Health.SetTo(99999f);
        }

        static void CleanupAfterTest() {
            s_target?.Discard();
            s_target = null;
            s_saveBlocker?.Discard();
            s_saveBlocker = null;
            
            var hero = Hero.Current;
            hero.MaxStamina.SetTo(s_staminaCache);
            hero.MaxMana.SetTo(s_manaCache);
            hero.MaxHealth.SetTo(s_healthCache);
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("TG/Assets/Weapons Attack per Second/Test All (and update templates)")]
        static void EngineTestAndUpdateAllWeapons() {
            if (!Application.isPlaying) {
                return;
            } 
            TestAllWeapons(true).Forget();
        }
        
        [UnityEditor.MenuItem("TG/Assets/Weapons Attack per Second/Test All (just log differences)")]
        public static void EngineTestAllWeapons() {
            if (!Application.isPlaying) {
                return;
            } 
            TestAllWeapons(false).Forget();
        }
        
        [UnityEditor.MenuItem("TG/Assets/Weapons Attack per Second/Test Current (log result)")]
        public static void EngineTestWeapon() {
            if (!Application.isPlaying) {
                return;
            }
            TestCurrentWeapon().Forget();
        }
                
        [UnityEditor.MenuItem("TG/Assets/Weapons Attack per Second/Test Current x10 (log results and compare them)")]
        public static void EngineTestWeapon10Times() {
            if (!Application.isPlaying) {
                return;
            }
            TestCurrentWeaponNTimes(10).Forget();
        }
#endif
        
        [Command("test.all-weapons-attack-speed", "Equips and test attack speed of all melee weapons to verify or update their attack speed")][UnityEngine.Scripting.Preserve]
        public static async UniTask TestAllWeapons(bool editTemplate) {
            PrepareTest();
            
            var itemTemplates = World.Services.Get<TemplatesProvider>().GetAllOfType<ItemTemplate>(TemplateTypeFlag.All).Where(ValidWeapon).ToList();
            int testedAmount = 0;
            int exceptionAmount = 0;
            int templatesAmount = itemTemplates.Count;
            Log.Important?.Info($"[APS Test] Testing {templatesAmount} weapons");
            foreach (var itemTemplate in itemTemplates) {
                testedAmount++;
                try {
                    float result = await TestAndUpdateWeapon(itemTemplate, editTemplate);
                    Log.Important?.Info($"[APS Test] {testedAmount}/{templatesAmount} Tested {itemTemplate.name}: {result:F3}");
                } catch (Exception e) {
                    exceptionAmount++;
                    Log.Important?.Error($"[APS Test] Failed Test for {itemTemplate.name}");
                    Debug.LogException(e);
                }
            }
            
            Log.Important?.Info($"[APS Test] {testedAmount}/{templatesAmount} Tested all weapons, {exceptionAmount} exceptions caught");
            CleanupAfterTest();
        }

        static async UniTask<float> TestAndUpdateWeapon(ItemTemplate itemTemplate, bool editTemplate) {
            float result = await TestWeapon(itemTemplate);
            var stats = itemTemplate.GetAttachment<ItemStatsAttachment>();
#if UNITY_EDITOR
            if (editTemplate) {
                stats.attacksPerSecond = result;
                UnityEditor.EditorUtility.SetDirty(stats);
                return result;
            }
#endif
            if (Math.Abs(stats.attacksPerSecond - result) > 0.02f) {
                Log.Important?.Error($"[APS Test] Tested {itemTemplate.name}: result {result:F3} is different than saved value {stats.attacksPerSecond}");
            }
            return result;
        }

        [Command("test.weapon-attack-speed", "Equips and tests attack speed of weapon based on their item template name")][UnityEngine.Scripting.Preserve]
        public static async UniTaskVoid TestWeaponByTemplateName([TemplateSuggestion(typeof(ItemTemplate))] ItemTemplate itemTemplate) {
            PrepareTest();
            await TestWeapon(itemTemplate);
            CleanupAfterTest();
        }
        
        static async UniTask<float> TestWeapon(ItemTemplate itemTemplate) {
            var hero = Hero.Current;
            itemTemplate.ChangeQuantity(hero.Inventory, 1);
            var item = hero.Inventory.Items.First(i => i.Template == itemTemplate);
            hero.Inventory.Equip(item);
            
            await AsyncUtil.DelayTime(Hero.Current, BreakDuration); 
            return await TestCurrentWeaponInternal();
        }

        [Command("test.current-weapon-attack-speed-n-times", "Tests attack speed of currently equipped weapon N times and compare results")][UnityEngine.Scripting.Preserve]
        public static async UniTaskVoid TestCurrentWeaponNTimes(int n) {
            PrepareTest();
            await TestCurrentWeaponInternal();
            CleanupAfterTest();
        }

        [UnityEngine.Scripting.Preserve]
        public static async UniTaskVoid TestCurrentWeaponNTimesInternal(int n) {
            float[] results = new float[n];
            for (int i = 0; i < n; i++) {
                results[i] = await TestCurrentWeaponInternal();
                await AsyncUtil.DelayTime(Hero.Current, BreakDuration);
            }
            
            double avg = results.Average();
            double deviation = M.StandardDeviation(results);
            Log.Important?.Info($"[APS Test] Tested {n} times => APS: {avg:F3} Standard Deviation: {deviation:F3} ({deviation / avg:P4})");
        }

        [Command("test.current-weapon-attack-speed", "Tests attack speed of currently equipped weapon")][UnityEngine.Scripting.Preserve]
        public static async UniTaskVoid TestCurrentWeapon() {
            PrepareTest();
            await TestCurrentWeaponInternal();
            CleanupAfterTest();
        }

        static async UniTask<float> TestCurrentWeaponInternal() {
            //Prepare Hero
            var hero = Hero.Current;
            hero.MaxStamina.SetTo(99999f);
            hero.Stamina.SetTo(99999f);
            
            //Prepare Attack Counter
            float previousAttackTime = Time.time;
            float attackTimeSum = 0f;
            int attacksPerformed = -1;
            
            //Prepare Attack Listener
            var attackListener = hero.ListenTo(ICharacter.Events.OnAttackRelease, _ => {
                if (attacksPerformed >= 0) {
                    attackTimeSum += Time.time - previousAttackTime;
                }
                previousAttackTime = Time.time;
                attacksPerformed++;
            }, hero);

            //Set timeScale
            Time.timeScale = TestTmeScale;

            //Perform attacks till tests end
            var mouseKey = World.Only<PlayerInput>().MouseDownActions[0];
            while (attacksPerformed < TestAttacksCount) {
                mouseKey.UpdateValue(true);
                if (Time.time - previousAttackTime > 20f) {
                    Log.Important?.Error($"[APS Test] Test failed for {hero.MainHandItem?.Template?.name} and {hero.OffHandItem?.Template?.name}. Timed out.");
                    return -1;
                }
                await AsyncUtil.DelayFrame(hero, ClickFrameInterval);
            }

            //Reset state
            Time.timeScale = 1f;
            World.EventSystem.DisposeListener(ref attackListener);
            
            float aps = 1 / (attackTimeSum / attacksPerformed);
            Log.Important?.Info($"[APS Test] Test result => APS: {aps:F3}");
            return aps;
        }
        
        static bool ValidWeapon(ItemTemplate template) {
            if (!template.IsWeapon || template.IsAbstract) {
                return false;
            }
            
            if (template.IsShield || template.IsRanged || template.IsMagic) {
                return false;
            }

            //ignore hands because most of animal/enemy items is based on hands (zombie_explode etc)
            if (template.tags.Contains("type:unarmed")) {
                return false;
            }

            var mobItems = template.GetAttachment<ItemEquipSpec>()?.RetrieveMobItemsInstance();
            if (mobItems == null) {
                return false;
            }

            if (ItemEquip.GetDebugHeroItem(mobItems) == null) {
                return false;
            }

            return true;
        }
    }
}
