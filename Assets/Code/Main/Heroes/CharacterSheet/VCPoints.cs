using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    public abstract class VCPoints : StatComponent {
        [SerializeField] bool showWhenZero;
        [SerializeField] GameObject availableContent;
        
        protected override IWithStats WithStats => Hero.Current;

        protected override void OnAttach() {
            base.OnAttach();
            
            if (statName == null) return;
            statName.SetText($"{LocTerms.AvailablePoints.Translate()}:");
        }

        protected override void OnStatUpdated() {
            bool availablePoints = showWhenZero || WithStats.Stat(StatType).ModifiedInt > 0;
            availableContent.SetActiveOptimized(availablePoints);
        }
    }
}