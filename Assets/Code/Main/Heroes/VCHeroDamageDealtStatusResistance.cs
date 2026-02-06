using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace Awaken.TG.Main.Heroes {
    public class VCHeroDamageDealtStatusResistance : ViewComponent<Hero> {
        const float FadeDuration = 0.5f;
        const float VisibilityDuration = 2f;
        
        [SerializeField] TextMeshProUGUI immuneText;
        [SerializeField] Transform statusesParent;
        [SerializeField] VCImmuneStatusIcon immuneStatusPrefab;

        readonly List<VCImmuneStatusIcon> _displayedStatusesList = new();
        ObjectPool<VCImmuneStatusIcon> _statusesPool;
        Hero _hero;
        Sequence _fadeSequence;

        protected override void OnAttach() {
            InitPool();
            immuneText.TrySetActiveOptimized(false);
            immuneText.color = ARColor.WhiteTransparent;
            immuneText.SetText(LocTerms.Immune.Translate());
            _hero = Hero.Current;
            World.EventSystem.ListenTo(EventSelector.AnySource, ICharacter.Events.TriedToDealBuildupStatus, this, OnTryToBuildupStatusOnCharacter);
            World.EventSystem.ListenTo(EventSelector.AnySource, ICharacter.Events.TriedToApplyInvulnerableStatus, this, OnTryToApplyInvulnerableStatus);
        }

        void InitPool() {
            _statusesPool = new ObjectPool<VCImmuneStatusIcon>(
                createFunc: () => Instantiate(immuneStatusPrefab, statusesParent),
                actionOnRelease: status => status.TrySetActiveOptimized(false),
                actionOnDestroy: status => Destroy(status.gameObject),
                defaultCapacity: 1
            );
        }
        
        void OnTryToBuildupStatusOnCharacter(TrialBuildupData data) {
            if (data.buildupDealer == _hero && data.buildupReceiver is NpcElement npcElement) {
                var npcStatValue = npcElement.Stat(data.buildupAttachment?.BuildupStatusType.BuildupStatType);
                var buildupThreshold = StatusStatsValues.GetThreshold(npcStatValue.ModifiedValue, npcElement.Tier);

                if (buildupThreshold == StatusStatsValues.StatusBuildupThreshold.CantGet) {
                    OnTryToApplyInvulnerableStatus(data.sourceInfo);
                }
            }
        }
        
        void OnTryToApplyInvulnerableStatus(StatusSourceInfo sourceInfo) {
            var character = sourceInfo.GetSourceCharacter;
            if (character == _hero) {
                TryDisplayStatusIcon(sourceInfo);
            }
        }

        void TryDisplayStatusIcon(StatusSourceInfo sourceInfo) {
            FadeImmuneText();
            VCImmuneStatusIcon statusIcon = _displayedStatusesList.FirstOrDefault(s => s.HasSetIcon(sourceInfo.Icon.Get()));
            
            if (statusIcon) {
                statusIcon.RefreshFade(FadeDuration, VisibilityDuration, () => AfterFadeCallback(statusIcon));
                return;
            }
            
            _statusesPool.Get(out statusIcon);
            _displayedStatusesList.Add(statusIcon);
            statusIcon.transform.SetAsLastSibling();
            statusIcon.TrySetActiveOptimized(true);
            statusIcon.TryDisplayImmuneIcon(sourceInfo, FadeDuration, VisibilityDuration, () => AfterFadeCallback(statusIcon));
            
        }

        void FadeImmuneText() {
            _fadeSequence.Kill();
            immuneText.TrySetActiveOptimized(true);
            _fadeSequence = DOTween.Sequence()
                .Append(immuneText.DOFade(1, FadeDuration))
                .AppendInterval(VisibilityDuration)
                .Append(immuneText.DOFade(0, FadeDuration))
                .AppendCallback(() => immuneText.TrySetActiveOptimized(false));
        }

        void AfterFadeCallback(VCImmuneStatusIcon statusIcon) {
            _displayedStatusesList.Remove(statusIcon);
            _statusesPool.Release(statusIcon);
        }

        protected override void OnDiscard() {
            _displayedStatusesList.Clear();
            _statusesPool.Dispose();
        }
    }
}