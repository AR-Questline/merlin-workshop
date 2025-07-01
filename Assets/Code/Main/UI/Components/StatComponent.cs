using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using Awaken.TG.Utility;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    /// <summary>
    /// MonoBehaviour that is attached to components displaying Stats with their upkeep deltas.
    /// Needs to be initialized by parent with correct IWithStats model to work.
    /// </summary>
    public abstract class StatComponent : ViewComponent, IWithTooltip {
        [SerializeField] bool asFloat;
        [SerializeField] bool asPercents;
        [SerializeField] bool showText = true;
        [SerializeField, ShowIf(nameof(showText))] protected TextMeshProUGUI statName;
        [SerializeField, ShowIf(nameof(showText))] protected TextMeshProUGUI text;
        [SerializeField, ShowIf(nameof(showText))] bool useStatColor = true;
        [SerializeField, ShowIf(nameof(showText))] bool showUpperLimit = true;
        [Space(10f)]
        [SerializeField] bool showBar;
        [SerializeField, ShowIf(nameof(showBar))] Bar bar;
        [Space(10f)]
        [SerializeField] bool showTooltip = false;
        [SerializeField, ShowIf(nameof(showTooltip))] RectTransform tooltipHost;

        public Stat Stat { get; private set; }

        IWithStats _target;

        // === Init

        protected override void OnAttach() {
            _target = WithStats;

            _target.ListenTo(Stat.Events.StatChanged(StatType), _ => UpdateStat(), this);

            Stat = _target.Stat(StatType);
            UpdateStat();

            if (statName != null) {
                statName.text = Stat.Type.DisplayName;
            }
        }
        
        protected abstract IWithStats WithStats { get; }
        protected abstract StatType StatType { get; }

        // === Update
        void UpdateStat() {
            string textValue = (asFloat ? Stat.ModifiedValue : Stat.ModifiedInt).ToString(asPercents ? "P0" : "");
            
            if (Stat is LimitedStat limitedStat) {
                if (showUpperLimit) {
                    textValue += $"/{(asFloat ? limitedStat.UpperLimit : limitedStat.UpperLimitInt).ToString(asPercents ? "P0" : "")}";
                }
                if (bar != null) {
                    bar.SetPercent(limitedStat.Percentage);
                }
            }

            if (showText) {
                text.text = useStatColor ? textValue.ColoredText(Stat.Type.Color, 0.4f) : textValue;
            }
            
            OnStatUpdated();
        }
        protected virtual void OnStatUpdated() { }

        // === Tooltip

        public UIResult Handle(UIEvent evt) {
            return showTooltip ? UIResult.Ignore : UIResult.Accept;
        }

        TooltipConstructor TooltipWithText => $"{Stat.Type.Tooltip.Translate()}";
        public TooltipConstructor TooltipConstructor => TooltipWithText?.WithStaticPositioningOf(tooltipHost);
    }

    public abstract class StatComponent<TModel> : StatComponent where TModel : IModel {
        public TModel Target => (TModel) GenericTarget;
    }
}
