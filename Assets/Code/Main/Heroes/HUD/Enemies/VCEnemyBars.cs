using System;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD.Enemies {
    public class VCEnemyBars : ViewComponent<Hero> {
        [SerializeField] Bar healthBar;
        [SerializeField] Bar staminaBar;

        void Awake() {
            gameObject.SetActiveOptimized(false);
        }

        public void UpdateHP(Location location) {
            if (HasBeenDiscarded) {
                return;
            }
            
            if (location.TryGetElement<IWithHealthBar>(out var withHealthBar) && !HasHealthBarBlocker(location, withHealthBar)) {
                healthBar.SetPercent(withHealthBar.HealthStat.Percentage);
                
                if (withHealthBar is NpcElement npcElement) {
                    UpdateStaminaBar(location, npcElement);
                } else {
                    DisableStaminaBar();
                }
            } else {
                healthBar.SetPercent(0);
                DisableStaminaBar();
            }
        }
        
        public void SetBars(float hpPercent, float staminaPercent, bool instant = false) {
            if (instant) {
                healthBar.SetPercentInstant(hpPercent);
                staminaBar.SetPercentInstant(staminaPercent);
            } else {
                healthBar.SetPercent(hpPercent);
                staminaBar.SetPercent(staminaPercent);
            }
        }

        static bool HasHealthBarBlocker(Location location, [CanBeNull] IWithHealthBar iWithHealthBar) {
            return location.HasElement<IHealthBarHiddenMarker>() || (iWithHealthBar?.HasElement<IHealthBarHiddenMarker>() ?? false);
        }

        void UpdateStaminaBar(Location location, NpcElement npcElement) {
            if (!location.TryGetElement(out EnemyBaseClass enemyBaseClass)) {
                staminaBar.gameObject.SetActive(true);
                return;
            }

            if (!enemyBaseClass.CanBeStaggered) {
                staminaBar.gameObject.SetActive(false);
                return;
            }

            staminaBar.SetPercent(enemyBaseClass.Staggered
                ? enemyBaseClass.StaggerDurationElapsedNormalized
                : npcElement.CharacterStats.Stamina.Percentage);

            staminaBar.gameObject.SetActive(true);
        }

        void DisableStaminaBar() {
            staminaBar.SetPercent(0);
            staminaBar.gameObject.SetActive(false);
        }
    }
}
