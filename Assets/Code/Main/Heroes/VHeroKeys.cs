using System.Collections.Generic;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Map;
using Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Containers;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    /// <summary>
    /// Additional view for handling keyboard shortcuts.
    /// </summary>
    [NoPrefab]
    public class VHeroKeys : View<Hero>, IUIPlayerInput {
        const float ChangeTppDistanceSpeed = 3f;
        
        bool _ignoreNextPerspectiveChange;
        
        public IEnumerable<KeyBindings> PlayerKeyBindings {
            get {
                yield return KeyBindings.UI.CharacterSheets.CharacterSheet;
                yield return KeyBindings.UI.CharacterSheets.Inventory;
                yield return KeyBindings.UI.CharacterSheets.Journal;
                yield return KeyBindings.UI.CharacterSheets.QuestLog;
                yield return KeyBindings.UI.HUD.OpenSkillTree;
                yield return KeyBindings.UI.CharacterSheets.ToggleMap;
                yield return KeyBindings.UI.HUD.QuickUseWheel;
                yield return KeyBindings.Gameplay.ToggleWeapon;
                yield return KeyBindings.Gameplay.ToggleCameraZoom;
                yield return KeyBindings.HeroItems.EquipFirstItem;
                yield return KeyBindings.HeroItems.EquipSecondItem;
                yield return KeyBindings.HeroItems.EquipThirdItem;
                yield return KeyBindings.HeroItems.EquipFourthItem;
                yield return KeyBindings.HeroItems.UseQuickSlot;
                yield return KeyBindings.HeroItems.NextQuickSlot;
                yield return KeyBindings.Gameplay.ChangeHeroPerspective;
            }
        }

        bool CanZoomBow => Target.Development.CanZoomBow;
        bool HasBowToUse => Target.CanUseEquippedWeapons && Target.MainHandWeapon is { Item: { IsRanged: true } };

        protected override void OnInitialize() {
            World.Only<PlayerInput>().RegisterPlayerInput(this, Target);
        }

        public UIResult Handle(UIEvent evt) {
            if (!Target.IsAlive) return UIResult.Ignore;

            if (evt is UIKeyDownAction uiDownAction) {
                var action = uiDownAction.Name;

                if (action == KeyBindings.UI.CharacterSheets.CharacterSheet) {
                    CharacterSheetUI.ToggleCharacterSheet();
                } else if (action == KeyBindings.UI.CharacterSheets.Inventory) {
                    CharacterSheetUI.ToggleCharacterSheet(CharacterSheetTabType.Inventory);
                } else if (action == KeyBindings.UI.CharacterSheets.Journal) {
                    CharacterSheetUI.ToggleCharacterSheet(CharacterSheetTabType.Journal);
                } else if (action == KeyBindings.UI.CharacterSheets.QuestLog) {
                    CharacterSheetUI.ToggleCharacterSheet(CharacterSheetTabType.Quests);
                } else if (!RewiredHelper.IsGamepad && action == KeyBindings.UI.HUD.OpenSkillTree) {
                    CharacterUI.ToggleCharacterSheet(CharacterSubTabType.Talents).Forget();;
                } else if (action == KeyBindings.UI.HUD.QuickUseWheel) {
                    QuickUseWheelUI.Show();
                } else if (action == KeyBindings.UI.CharacterSheets.ToggleMap && MapUI.IsOnSceneWithMap()) {
                    CharacterSheetUI.ToggleCharacterSheet(CharacterSheetTabType.Map);
                } else if (action == KeyBindings.Gameplay.ToggleWeapon) {
                    Target.Trigger(Target.IsWeaponEquipped ? Hero.Events.HideWeapons : Hero.Events.ShowWeapons, false);
                } else if (action == KeyBindings.HeroItems.EquipFirstItem) {
                    EquipLoadout(0);
                } else if (action == KeyBindings.HeroItems.EquipSecondItem) {
                    EquipLoadout(1);
                } else if (action == KeyBindings.HeroItems.EquipThirdItem) {
                    EquipLoadout(2);
                } else if (action == KeyBindings.HeroItems.EquipFourthItem) {
                    EquipLoadout(3);
                } else if (action == KeyBindings.HeroItems.UseQuickSlot && Target.HeroItems.TryGetSelectedQuickSlotItem(out Item selectedItem)) {
                    UseQuickSlotItem(selectedItem);
                } else if (action == KeyBindings.HeroItems.NextQuickSlot) {
                    Target.HeroItems.SelectNextQuickSlot();
                } else if (action == KeyBindings.Gameplay.ToggleCameraZoom && HasBowToUse && CanZoomBow) {
                    Target.FoV.ApplyBowZoomFoV();
                } else {
                    return UIResult.Ignore;
                }
                // if we got here, we did something with the key
                return UIResult.Accept;
            }

            if (evt is UIKeyHeldAction keyHeldAction) {
                if (RewiredHelper.IsGamepad && Hero.TppActive && keyHeldAction.Name == KeyBindings.Gameplay.ChangeHeroPerspective) {
                    // var input = RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.CameraVertical);
                    // if (math.abs(input) > 0.1f) {
                    //     World.Any<TppCameraDistanceSetting>()?.ChangeValue(input * -1 * ChangeTppDistanceSpeed * Time.deltaTime);
                    //     _ignoreNextPerspectiveChange = true;
                    //     return UIResult.Accept;
                    // }
                }
            }
            
            if (evt is UIKeyUpAction keyUpAction) {
                if (keyUpAction.Name == KeyBindings.Gameplay.ChangeHeroPerspective) {
                    if (_ignoreNextPerspectiveChange) {
                        _ignoreNextPerspectiveChange = false;
                        return UIResult.Ignore;
                    } 
                    
                    if (!World.HasAny<ContainerUI>()) {
                        Target.VHeroController.ChangeHeroPerspective(!Hero.TppActive).Forget();
                        return UIResult.Accept;
                    }
                } 
                
                if (!CanZoomBow) {
                    return UIResult.Ignore;
                }
            
                if (keyUpAction.Name == KeyBindings.Gameplay.ToggleCameraZoom && HasBowToUse) {
                    Target.FoV.EndBowZoomFoV();
                    return UIResult.Accept;
                }
            }

            // ignore non-key events
            return UIResult.Ignore;
        }

        void UseQuickSlotItem(Item selectedItem) {
            selectedItem?.Use();
            Target.Trigger(HeroItems.Events.QuickSlotUsed, Target);
        }

        void EquipLoadout(int index) {
             Target.HeroItems.ActivateLoadout(index);
        }
    }
}