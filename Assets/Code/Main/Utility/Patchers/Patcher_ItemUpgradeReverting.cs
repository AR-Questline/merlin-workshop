using System;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.NewGamePlus;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Utility.Patchers {
    // Reverts the level of items from which we removed crit gains and returns the ingredients to the player.
    public static class Patcher_ItemUpgradeReverting {
        public static class Patch110 {
            public static void Apply() {
                Hero.Current.Storage.RequestItems();
                {
                    RevertUpgradesFor(ItemsToPatch110, nameof(Patch110));
                }
                Hero.Current.Storage.ReleaseItems();
            }

            static readonly string[] ItemsToPatch110 = {
                "9b18d714664362c4e868043ba5b18ff1", // ItemTemplate_Armor_Light_T2_Head_ChildsOfMorriganMask.prefab
                "f06dd5dfe3b471846978d4ad3b497572", // ItemTemplate_Armor_Light_T4_Feet_ChildrenOfMorriganBoots.prefab
                "076cda7e26a3e334cb5debbc93e09de8", // ItemTemplate_Armor_Light_T4_Legs_ChildrenOfMorriganTrousers.prefab
                "715c382d9ccf21c41ac1cff6848cc8e7", // ItemTemplate_Armor_Light_T5_Feet_ChildrenOfMorriganBoots.prefab
                "612cfac67e024ae47a7676e9926b0269", // ItemTemplate_Armor_Light_T5_Legs_ChildrenOfMorriganTrousers.prefab
                "f2588c7580780ec44af3c5ad74ba14ca", // ItemTemplate_Armor_Medium_T4_Back_VolkerBerserkerTotem.prefab
                "cffb8bac2980a884ab81a69a946f42d6", // ItemTemplate_Armor_Light_T4_Arms_TheHornedWardensGloves.prefab
                "5b533831c2216b64ca8f23da8e3986d6", // ItemTemplate_Armor_Light_T4_Head_TheHornedWardensMask.prefab
                "73c8d3ad38da59748aeb655f14d4e0bd", // ItemTemplate_Armor_Light_T4_Legs_TheHornedWardensBreeches.prefab
                "0b0633cdd27c9e7408bc752d111296cb", // ItemTemplate_Armor_Medium_T5_Arms_ArchdruidsGloves.prefab
                "4e09a96e76a795f4c90b6784eab25958", // ItemTemplate_Armor_Medium_T5_Back_DuelKnightCape.prefab
                "57a4625c096681048a41b696d9496920", // ItemTemplate_Armor_Light_T3_Legs_DuelKnightTrousers.prefab
                "8ec2e9ceb976e474780cf18cfcc93c2d", // ItemTemplate_Armor_Medium_T3_Back_DuelKnightCape.prefab
                "6183f97a43165bc48928356fcc396ffc", // ItemTemplate_Armor_Medium_T3_Arms_CorpsebinderGauntlets.prefab
                "e0286ed67fc39354aa85569ee19418db", // ItemTemplate_Armor_Medium_T3_Head_CorpsebinderHelm.prefab
                "281f78c7213e5cc4aa1e760ffccb79bb", // ItemTemplate_Armor_Medium_T5_Back_WingedCavalierBackplate.prefab
                "21009c5b13b7ef94ebb813c173eef017", // ItemTemplate_Armor_Medium_T5_Back_ThornsOfTheNetherbloom.prefab
                "51fb841d21bfe634cb37e4eb0e09062d", // ItemTemplate_Armor_Heavy_T2_Back_CamouflageNet.prefab
                "e6d5c7d584dd6bc49a7b46f551e71485", // ItemTemplate_Armor_Light_T3_Body_BattlemageTunic.prefab
                "46ec6a1610d32cf42a4334abf637a9ed", // ItemTemplate_Armor_Heavy_T3_Arms_BerserkersGauntlets.prefab
                "a79fe59b42aac1647a46466fe1da4086", // ItemTemplate_Armor_Heavy_T3_Body_BerserkersArmor.prefab
                "f63dbf75601aa6a48a023ee92bf1ae74", // ItemTemplate_Armor_Heavy_T3_Head_BerserkersHelmet.prefab
                "f9eb73740b910534d8655b432baafa36", // ItemTemplate_Armor_Light_T3_Feet_BerserkersBoots.prefab
                "77265ddd709f7824cace927a49c1ac44", // ItemTemplate_Armor_Light_T3_Legs_BerserkerKilt.prefab
                "14053be0cb5343c48a1005bf5a1c3e14", // ItemTemplate_Armor_Light_T3_Body_TheProphetsShroud.prefab
                "ef88a3ae37858d24aa6fa005267296e9", // ItemTemplate_Armor_Light_T3_Body_ElusiveRobe.prefab
                "1cec0e5404c911c43923a375ab80a19a", // ItemTemplate_Armor_Light_T3_Head_ElusiveCowl.prefab
                "1038fb3510d138948a0c019a2c3b571d", // ItemTemplate_Armor_Medium_T3_Legs_ElusiveLegs.prefab
                "5dfbe3afc101c6f4aab0d72f6ae8120f", // ItemTemplate_Armor_Light_T3_Body_WorkerBeesRobe.prefab
                "14a5d1dad127cad4cb0b99c6412f3df6", // ItemTemplate_Armor_Light_T4_Arms_FrithirCorruptedOracleAdornments.prefab
                "5bb4657824a0de64da683254e928e2c8", // ItemTemplate_Armor_Light_T4_Back_FrithirCorruptedOracleWings.prefab
                "031ea256ec8d88b46870e5e1311abdd6", // ItemTemplate_Armor_Light_T4_Body_FrithirCorruptedOracleVestments.prefab
                "5bb7b43fbe8bff24fbd1f761c381d600", // ItemTemplate_Armor_Light_T4_Head_FrithirCorruptedOracleHeaddress.prefab
                "3837d4e9b79905e46a6df2800d05e194", // ItemTemplate_Armor_Light_T4_Legs_FrithirCorruptedOracleBelt.prefab
                "8205bf551ef820043bb33739d1168c67", // ItemTemplate_Armor_Light_T4_Head_UmbralShroud.prefab
                "b48628f3596168740b151392069fe6c7", // ItemTemplate_Armor_Medium_T5_Head_FarsightHood.prefab
                "db6bc48db781bf549b7fe4237de979a8", // ItemTemplate_Armor_Medium_T5_Back_AshenVeilCloak.prefab
                "c9958a7abc4d8a141852ebd1797d605c", // ItemTemplate_Armor_Light_T5_Head_UsurpersCrown.prefab 40
                "df8417fa541f68c489821dcccd25e974", // ItemTemplate_Armor_Heavy_T6_Arms_KingArthursGauntlets.prefab
                "35f8c60a4a258b24297dc3c12be8a07e", // ItemTemplate_Armor_Heavy_T6_Legs_KingArthursGreaves.prefab
                "c294876bdeeecd34db080bc47e949351", // ItemTemplate_Armor_Medium_T4_Head_FoolsBeak.prefab
                "506c39f39e1d6b141855250a59441437", // ItemTemplate_Armor_Medium_T4_Head_TalonTongue.prefab
                "bf6298f1dca82bc45976c4c713e8c653", // ItemTemplate_Armor_Medium_T4_Head_TheTwinDirgeVeil.prefab
                "b79de5055b514cd4e98b81ca5f96dd43", // ItemTemplate_Weapon_1H_Sword_Light_Tier1_WolfsSong.prefab 50
                "dd13fadc4e0317245b3c412d2974cc05", // ItemTemplate_Weapon_2H_Sword_Heavy_Tier1_WolfsHowl.prefab
                "082f10465d7fe7342a2e08e7d8551e92", // ItemTemplate_Weapon_1H_Sword_Medium_Tier3_OldsteelSword.prefab
                "5bf78d5fe1693574691b545c0daeb710", // ItemTemplate_Weapon_1H_Sword_Medium_Tier4_PoisonedKnightsGladius.prefab
                "f515baf597f5ea0498b497a817138538", // ItemTemplate_Weapon_2H_Sword_Heavy_Tier4_PoisonedKnightsGreatsword.prefab
                "392480ad54ab7c2439c0a9e70f7a9a1b", // ItemTemplate_Weapon_Wand_Tier4_OrinsServantRod.prefab
                "c68a7b5d28677534b8ea8eb33878063b", // ItemTemplate_Weapon_Greatbow_Tier4_Heavy_DrevanBow.prefab 56

                "b044f68d3b865fc49a8f75366a7364ff", // ItemTemplate_Jewelry_Tierable_Amulet_StonewardensTalisman
                "d3c661850fed66845bd31501f83b75f9", // ItemTemplate_Armor_Heavy_T6_Body_KingArthursBreastplate
                "29897c9deefea5a46bb8d8b0b9f654e7", // ItemTemplate_Armor_Heavy_T5_Body_UlfrWarswornChestplate
                "5aec49ad6a3fc17499059732d20e0f10", // ItemTemplate_Jewelry_Static_Ring_MarksmansTreasure
                "6c462a66409aa384d8d2f5fdf10c51d0", // ItemTemplate_Jewelry_Tierable_Ring_PerilousOrb
                "93a65acf46e592141bb259311a10b81f", // ItemTemplate_Jewelry_Static_Amulet_TheudProtectionCharm
                "f13505fd16f622041828e95d1fde3128", // ItemTemplate_Jewelry_Static_Amulet_LuckyAmulet
                "7b845428288bcba4a8fb7ae00fa0bf25", // ItemTemplate_Jewelry_Static_Amulet_ColmsFingerNecklace
                "855c7beb6ccb3a5478eeb00a38eab7bc", // ItemTemplate_Gem_Tierable_Weapon_Hastefang
                "11d97c37ccd21c84a845eac05ee62ff9", // ItemTemplate_Armor_Heavy_T5_Legs_VolkerWarbornTrousers
                "0e8f294163ba83540855ae7378d5a21f", // ItemTemplate_Armor_Light_T5_Legs_TheudDreamwalkerTrousers
                "07b961e5a4fce2e4a81b5a5051624695", // ItemTemplate_Armor_Heavy_T6_Head_SirGawainsHelm_Dummy
                "577c12661e37e824d99205f291f78952", // ItemTemplate_Weapon_1H_Sword_Medium_Tier3_Sting
                "a02adf48f80a454478dd683a03bb77f3", // ItemTemplate_Weapon_2H_Sword_Heavy_Tier4_OathOfFamine
                "5cecc0d0b7f2999449f21b0de3db4bf7", // ItemTemplate_SoulCube_Tier5_HollowReprieve
                "57fbecf61e791434e98f20be96f418bf", // ItemTemplate_Weapon_1H_Sword_Light_Tier1_SpineSplinter
                "d7fabef7dd58946489db77eb44b4b408", // ItemTemplate_Jewelry_Tierable_Ring_PerilousOrb_Tier2
                "6211baa2136b73e47990876b543ac804", // ItemTemplate_Armor_Heavy_T6_Arms_DagonetGloves
                "aa229c7d00512c44b9754c80602b3007", // ItemTemplate_Alchemy_Fireball
                "c9958a7abc4d8a141852ebd1797d605c", // ItemTemplate_Armor_Light_T5_Head_UsurpersCrown
                "6c255776dfae9394fa48cf1821bb959e", // ItemTemplate_Armor_Medium_T3_Feet_KeepersBootsPlayer
                "7441eb8f1e2b35842944492b3a97c45b", // ItemTemplate_Armor_Light_T1_Head_HelmOfCriticalStrikes
                "1fafa66e64bd8bc4caa1c5664e472533", // ItemTemplate_Armor_Medium_T5_Head_FrozenSeersCrown
                "8f547a0316a62ed48a386226f35aa9f5", // ItemTemplate_Jewelry_Static_Amulet_ArchdruidsAmulet
                "3a25fc93837e18b48b5da74e51abd3dc", // ItemTemplate_Jewelry_Static_Amulet_AmuletOfTheWaningMoon
                "f1432fa99d5da1c48a94c0d791401cf9", // ItemTemplate_Jewelry_Static_Amulet_TheHornedWardensAmulet
                "88ccf6d27d259324cae9d2da52d9aa28", // ItemTemplate_Armor_Light_T0_Head_LootersMask
                "95f11a0594eedf54fb16a63897a91be8", // ItemTemplate_Weapon_2H_Sword_Heavy_Tier6_ParadeBlade
                "e6b397ef6ff9dc741b87299e39e230a3", // ItemTemplate_Armor_Light_T5_Head_TheQueensCrown
                "690f7cb49de2798408d5e03b7df63c40", // ItemTemplate_Armor_Light_T5_Legs_RoyalBreeches
                "460540f3b0184784e9396d1d66c9e120", // ItemTemplate_Armor_Light_T5_Arms_RoyalGloves
                "3653505e0deac764eb1bee9bf1d7a810", // ItemTemplate_Armor_Light_T5_Body_RoyalGown
                "a8b20adb9a12b6d44b521dae9289f33b", // ItemTemplate_Armor_Light_T5_Feet_RoyalSlippers
                "24d21aa71b006cf4aaee5d9e5d78fdb5", // ItemTemplate_Armor_Light_T4_Head_QueensProtectorCrown
                "7a503af5cf4c01640b744ca1a4f4ab9c", // ItemTemplate_Armor_Light_T4_Head_NightstalkersMask
                "83fa41bfaac4fd743a72fbf7680c8f13", // ItemTemplate_Armor_Light_T4_Legs_NightstalkersTrousers
                "a3fecb6f3c8b8a34ca49b4ad149bffe8", // ItemTemplate_Armor_Light_T4_Body_NightstalkersTunic
                "7f1bbd032c330024686119e5aa1336f4", // ItemTemplate_Armor_Medium_T5_Legs_ArchdruidsBreeches
                "7763ea00d39d89e4aa2449c4b40a3f05", // ItemTemplate_Weapon_1H_Dagger_Tier5_Light_CrimsonOracleDagger
                "60595632b768c2b47a7b6f501129e908", // ItemTemplate_Armor_Heavy_T6_Body_SirLancelotsChestplate
                "3dea141a4cb0ab04eb8d9661574ee0a5", // ItemTemplate_Armor_Medium_T5_Feet_CrystalWalkerBoots
                "9ba011d284bc96b46adf92c9af8ce9c1", // ItemTemplate_Armor_Light_T4_Head_FungalEnvoysMycelium
                "69458c11592f912439ee11a59847d372", // ItemTemplate_Armor_Light_T5_Legs_DuelKnightTrousers
                "a3b7015480fa5b940a67a2a0c76ab8f4", // ItemTemplate_Weapon_2H_Hammer_VeryHeavy_Tier6_GalahadsMaceOriginal
                "79a0bfb7c46431c48a8629499d7d868b", // ItemTemplate_Weapon_1H_Sword_Light_Tier4_DuelistsBlade
                "9703fd4846c86a646bb81586e0d69579", // ItemTemplate_SoulCube_InsatiableHeart
                "c2691505ba059624392baa51f984af0a", // ItemTemplate_Jewelry_Tierable_Ring_PerilousOrb_Tier1
                "b59e35fc64567bd40b36dbd7a97ef979", // ItemTemplate_Alchemy_PrecisionPotion
                "66f164c0b2be6f643a52532cfe0af966", // ItemTemplate_Armor_Light_T3_Legs_CrowsTrousers
                "c745f68be41e01a48aef6c6774c82289", // ItemTemplate_Armor_Light_T3_Body_CrowsTunic
                "05070eeff7409de46b9f33663f147d1c", // ItemTemplate_Armor_Light_T3_Feet_CrowsBoots
                "7499e60bbfa72524fb63255bb6ccd8ca", // ItemTemplate_Armor_Light_T3_Head_CrowsMask
                "215222a1a397bbd45b4f472a5c047bc8", // ItemTemplate_Armor_Light_T3_Arms_CrowsGloves
                "2592fcdf224a79241bca0e490e3aec9d", // ItemTemplate_SoulCube_BeorsNewHeart
            };
        }

        public static class Patch113 {
            public static void Apply() {
                Hero.Current.Storage.RequestItems();
                {
                    RevertUpgradesFor(ItemsToPatch113, nameof(Patch113));
                }
                Hero.Current.Storage.ReleaseItems();
            }
            
            static readonly string[] ItemsToPatch113 = {
                "3f6e886784e3a3146ab38d934b14c9d5", // herbalists cape
                "b69c001c2aa1e9e47a09af2ae8d3acb9", // priestess cape
                "2816c958b916cd049820c0f1f46dbe7b", // spellgorger wand
            };
        }
    
        
        static void RevertUpgradesFor(in string[] itemGuids, string patchName) {
            foreach (var item in Hero.Current.Inventory.Items.Concat(Hero.Current.Storage.Items).ToArray()) {
                if (item is {HasBeenDiscarded: true} or not {IsFullyInitialized: true}) continue;
                if (item.Template == null || !item.IsEquippable) continue;
                if (Array.IndexOf(itemGuids, item.Template.GUID) < 0) continue;

                int ngPlusBonus = NewGamePlusSystem.CalculateBonusItemLevelValue(item.NewGamePlusLevel);
                float baseLevel = item.Level.BaseValue - ngPlusBonus;
                if (baseLevel <= 0) { // no items to return
                    continue;
                }

                var config = item.Template.ItemUpgradeConfigConfig;
                for (int i = 0; i < baseLevel; i++) {
                    foreach (var itemData in config.GetIngredients(i)) {
                        Hero.Current.Storage.Add(new Item(itemData.itemTemplate, itemData.quantity));
                    }
                    Hero.Current.Cobweb.IncreaseBy(config.GetPrice(CurrencyType.Cobweb, i));
                    Hero.Current.Wealth.IncreaseBy(config.GetPrice(CurrencyType.Money, i));
                }

                item.Level.SetTo(ngPlusBonus);
                Log.Important?.Warning($"{patchName}: Item {item.Template.name} was patched to level {ngPlusBonus} and ingredients were returned to inventory");
            }
        }
    }
}