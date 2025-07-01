using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Heroes.Crosshair {
    public partial class HeroCrosshair : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        CrosshairTargetType _currentLocationType = CrosshairTargetType.Default;
        
        public CrosshairTargetType CurrentLocationType => _currentLocationType;
        public Hero Hero => ParentModel;

        public new static class Events {
            public static readonly Event<HeroCrosshair, CrosshairTargetType> CrosshairLocationTypeChanged = new(nameof(CrosshairLocationTypeChanged));
        }
        
        protected override void OnFullyInitialized() {
            AddPart<DefaultCrosshairPart>();
            foreach (var slotType in EquipmentSlotType.All) {
                var equippedItem = Hero.HeroItems.EquippedItem(slotType);
                if (equippedItem != null) {
                    OnEquippedChanged(slotType, equippedItem);
                }

                var slotTypeCapture = slotType;
                Hero.HeroItems.ListenTo(ICharacterInventory.Events.SlotEquipped(slotType), i => OnEquippedChanged(slotTypeCapture, i), this);
            }

            Hero.ListenTo(Hero.Events.HeroCrouchToggled, OnCrouchToggled, this);
            Hero.ListenTo(VCHeroRaycaster.Events.PointsTowardsIWithHealthBar, OnPointingTowardsLocationWithHP, this);
            Hero.ListenTo(VCHeroRaycaster.Events.StoppedPointingTowardsLocation, () => OnPointingTowardsLocationWithHP(null), this);
#if DEBUG
            Hero.ListenTo(HealthElement.Events.OnModifiedDamageDealt, OnModifiedDamageDealt, this);
#endif
        }

        public void HeroPerspectiveChanged(bool tppActive) {
            DefaultCrosshairPart defaultCrosshairPart = TryGetElement<DefaultCrosshairPart>();
            if (defaultCrosshairPart != null) {
                defaultCrosshairPart.MainView.GetComponent<RectTransform>().localScale = Vector3.one * (tppActive ? 2f : 1.5f);
            }
        }
        
        void OnCrouchToggled(bool isCrouching) {
            if (isCrouching) {
                AddPart<CrouchCrosshairPart>();
            } else {
                RemovePartsOfType<CrouchCrosshairPart>();
            }
        }

        void OnEquippedChanged(EquipmentSlotType slotType, Item itemInSlot) {
            CrosshairPart part = itemInSlot.Template.GetAttachment<CustomCrosshairAttachment>()?.SpawnCustomCrosshairPart();
            if (part != null) {
                AddPart(part);
            } else if (itemInSlot.IsRanged) {
                part = AddPart<BowCrosshairPart>();
            } else if (itemInSlot.IsMelee) {
                part = AddPart<MeleeCrosshairPart>();
            }

            if (part != null) {
                Hero.HeroItems.ListenTo(ICharacterInventory.Events.SlotUnequipped(slotType), i => {
                    if (i == itemInSlot) {
                        RemovePart(part);
                    }
                }, part);
            }
        }

        /// <summary>
        /// Debug method to temporarily show that we dealt critical damage.
        /// </summary>
        /// <param name="critInfo"></param>
        void OnModifiedDamageDealt(DamageModifiersInfo critInfo) {
            GameObject go = new("Critical");
            go.transform.SetParent(Services.Get<ViewHosting>().OnMainCanvas());
            TextMeshProUGUI textComponent = go.AddComponent<TextMeshProUGUI>();
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            if (critInfo.IsCritical) {
                textComponent.text += "<color=#FF0000>Critical!</color>\n";
            } 
            if (critInfo.IsSneak) {
                textComponent.text += "<color=#00FF00>Sneaky Hit!</color>\n";
            } 
            if (critInfo.IsWeakSpot) {
                textComponent.text += "<color=#FFEE00>WeakSpot Hit!</color>";
            }
            RectTransform rt = textComponent.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, 0);
            Object.Destroy(go, 1.5f);
        }

        void OnPointingTowardsLocationWithHP(Location location) {
            CrosshairTargetType locationType = CrosshairTargetType.Default;
            if (location != null && location.TryGetElement<NpcElement>(out var npc)) {
                if (npc.IsHostileTo(Hero)) {
                    locationType = CrosshairTargetType.Hostile;
                } else {
                    locationType = CrosshairTargetType.NonHostile;
                }
            }

            if (_currentLocationType != locationType) {
                _currentLocationType = locationType;
                this.Trigger(Events.CrosshairLocationTypeChanged, _currentLocationType);
            }
        }

        void Refresh() {
            RefreshLayer(CrosshairLayer.OverridingLayer0);
            RefreshLayer(CrosshairLayer.OverridingLayer1);
            RefreshLayer(CrosshairLayer.OverridingLayer2);
        }

        void RefreshLayer(CrosshairLayer layer) {
            CrosshairPart prioritized = null;
            int priority = -100;
            foreach (var part in Elements<CrosshairPart>()) {
                if ((part.Layer & layer) == 0) {
                    continue;
                }
                part.SetActive(false);
                if (part.Priority > priority) {
                    prioritized = part;
                    priority = part.Priority;
                }

                if (part.SpawnAsLast) {
                    part.MainView.transform.SetAsLastSibling();
                }
            }
            prioritized?.SetActive(true);
        }

        T AddPart<T>() where T : CrosshairPart, new() {
            if (!HasElement<T>()) {
                var part = new T();
                AddPart(part);
                return part;
            }
            return null;
        }

        void AddPart(CrosshairPart part) {
            AddElement(part);
            Refresh();
        }

        void RemovePartsOfType<T>() where T : CrosshairPart, new() {
            RemoveElementsOfType<T>();
            Refresh();
        }

        void RemovePart(CrosshairPart part) {
            part.Discard();
            Refresh();
        }
    }
}