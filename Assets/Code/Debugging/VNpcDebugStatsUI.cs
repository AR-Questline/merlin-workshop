using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Debugging {
    [UsesPrefab("CharacterSheet/Overview/" + nameof(VNpcDebugStatsUI))]
    public class VNpcDebugStatsUI : View<DebugStatsUI> {
        const string StatsFormat = "HP: {0:F2}\nSP: {1:F2}\nMP: {2:F2}\n<size=80%>{3}";
        
        [SerializeField] TextMeshProUGUI statsText;

        NpcElement _npcElement;
        HealthElement _npcHealthElement;

        bool PreventUpdate => _npcElement == null ||
                          _npcElement.HasBeenDiscarded ||
                          _npcHealthElement == null ||
                          HasBeenDiscarded;
        static Hero Hero => Hero.Current;

        public override Transform DetermineHost() => Hero.View<VHeroHUD>().centerBars;

        protected override void OnInitialize() {
            Hero.ListenTo(VCHeroRaycaster.Events.PointsTowardsIWithHealthBar, OnPointingTowardsLocation, this);
            ResetStats();
        }

        void Update() {
            if (PreventUpdate) {
                _npcElement = null;
                _npcHealthElement = null;
                ResetStats();
                return;
            }
            
            UpdateStats();
        }

        void OnPointingTowardsLocation(Location location) {
            _npcElement = location.TryGetElement<NpcElement>();
            if (_npcElement == null) {
                return;
            }
            
            _npcHealthElement = _npcElement.TryGetElement<HealthElement>();
            if (_npcHealthElement == null) {
                return;
            }
            
            UpdateStats();
        }

        void UpdateStats() {
            statsText.SetText(string.Format(StatsFormat,
                _npcHealthElement.Health.ModifiedValue,
                _npcElement.CharacterStats.Stamina.ModifiedValue,
                _npcElement.CharacterStats.Mana.ModifiedValue,
                _npcElement.ParentModel.DisplayName
            ));
        }
        
        void ResetStats() {
            statsText.SetText(string.Format(StatsFormat, 0, 0, 0, string.Empty));
        }
    }
}