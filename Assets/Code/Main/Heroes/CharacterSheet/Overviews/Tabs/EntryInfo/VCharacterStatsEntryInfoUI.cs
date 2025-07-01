using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterStats;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.StatsSummary;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.EntryInfo {
    [UsesPrefab("CharacterSheet/Stats/RPGStats/" + nameof(VCharacterStatsEntryInfoUI))]
    public class VCharacterStatsEntryInfoUI : FoldingViewUI<CharacterStatsEntryInfoUI> {
        [SerializeField] Transform statsContainer;
        [SerializeField] VCStatsSummaryEntryUI statsSummaryEntryPrefab;
        [SerializeField] CanvasGroup group;
        [SerializeField] float heightAllowance = 20;

        protected override float PreferredHeight { get; set; }
        public override Transform DetermineHost() => StatsEntry.EntriesParent;
        VStatsEntryUI StatsEntry => Target.ParentModel.View<IVEntryParentUI>() as VStatsEntryUI;
        float? _entryPreferredHeight;
        
        protected override void OnMount() {
            base.OnMount();
            
            foreach (var stat in Target.StatEntries) {
                var entry = Instantiate(statsSummaryEntryPrefab, statsContainer);
                entry.Override(stat.Name, stat.Value);
                _entryPreferredHeight ??= entry.GetComponent<LayoutElement>().preferredHeight;
            }
            
            group.DOFade(0, 0).SetUpdate(true);
        }

        public override Sequence Unfold() {
            StatsEntry.SelectionOnFold(false);
            _hideSequence = base.Unfold().Join(group.DOFade(0, ShowDuration));
            return _hideSequence;
        }
        
        public override Sequence Fold() {
            PreferredHeight = ComputeHeight();
            StatsEntry.SelectionOnFold(true);
            _showSequence = base.Fold().Join(group.DOFade(1, ShowDuration));
            return _showSequence;
        }
        
        float ComputeHeight() {
            return _entryPreferredHeight * Target.StatEntries.Count + heightAllowance ?? 0;
        }
    }
}
