using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Fishing;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public class VCHeroDamageDealtStatusResistance : ViewComponent<Hero> {
        const float FadeDuration = 0.5f;
        
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TMP_Text statusResistanceText;

        string _immuneText;
        string _resistanceText;
        Sequence _fadeSequence;
        Hero _hero;

        public static class Events {
            public static readonly Event<Hero, IEnumerable<IFishVolume>> OnDebugFishDataShow = new(nameof(OnDebugFishDataShow));
            public static readonly Event<Hero, Hero> OnDebugFishDataHide = new(nameof(OnDebugFishDataHide));
        }

        protected override void OnAttach() {
            canvasGroup.alpha = 0f;
            canvasGroup.TrySetActiveOptimized(false);
            _hero = Hero.Current;
            _immuneText = LocTerms.Immune.Translate();
            _resistanceText = LocTerms.Resistant.Translate();
            World.EventSystem.ListenTo(EventSelector.AnySource, ICharacter.Events.TriedToDealBuildupStatus, this, OnTryToBuildupStatusOnCharacter);
            World.EventSystem.ListenTo(EventSelector.AnySource, ICharacter.Events.TriedToApplyInvulnerableStatus, this, OnTryToApplyInvulnerableStatus);
            World.EventSystem.ListenTo(EventSelector.AnySource, Events.OnDebugFishDataShow, this, DebugShowAvailableFish);
            World.EventSystem.ListenTo(EventSelector.AnySource, Events.OnDebugFishDataHide, this, DebugHideAvailableFish);
        }
        
        void DebugShowAvailableFish(IEnumerable<IFishVolume> volumes) {
            bool containsGenericFishVolume = false;
            statusResistanceText.text = "";
            
            foreach (var volume in volumes) {
                if (volume == null) {
                    continue;
                }
                
                if (volume is FishVolume fishVolume) {
                    var fishTable = fishVolume.AllFish;
                        
                    foreach (var fish in fishTable.entries) {
                        statusResistanceText.text += string.Format("{0} - {1} {2}", fish.data.name, fish.occurrence.ToString(), "\n");
                    }
                } else if (volume is GenericFishVolume) {
                    containsGenericFishVolume = true;
                }
            }

            if (containsGenericFishVolume) {
                statusResistanceText.text += "Generic Fish Volume\n";
            }
            
            canvasGroup.alpha = 1f;
            canvasGroup.TrySetActiveOptimized(true);
        }

        void DebugHideAvailableFish() {
            statusResistanceText.text = "";
            canvasGroup.alpha = 0f;
            canvasGroup.TrySetActiveOptimized(false);
        }
        
        void OnTryToBuildupStatusOnCharacter(TrialBuildupData data) {
            if (data.buildupDealer == _hero && data.buildupReceiver is NpcElement npcElement) {
                var npcStatValue = npcElement.Stat(data.buildupAttachment?.BuildupStatusType.BuildupStatType);
                string resistanceName = npcStatValue == null ? "" : GetResistanceName(npcStatValue.ModifiedValue, npcElement.Tier);
                
                if (string.IsNullOrEmpty(resistanceName)) {
                    canvasGroup.alpha = 0f;
                    canvasGroup.TrySetActiveOptimized(false);
                } else {
                    Fade();
                }
                
                statusResistanceText.text = resistanceName;
            }
        }
        
        void OnTryToApplyInvulnerableStatus(ICharacter character) {
            if (character == _hero) {
                Fade();
                statusResistanceText.text = _immuneText;
            }
        }

        string GetResistanceName(float currentValue, int tier) {
            if (currentValue < StatusStatsValues.GetThreshold(StatusStatsValues.StatusBuildupThreshold.Resistant, tier)) {
                return "";
            }

            return currentValue < StatusStatsValues.GetThreshold(StatusStatsValues.StatusBuildupThreshold.CantGet, tier)
                ? _resistanceText
                : _immuneText;
        }

        void Fade() {
            canvasGroup.TrySetActiveOptimized(true);
            _fadeSequence.Kill();
            _fadeSequence = DOTween.Sequence().SetUpdate(true)
                .Append(canvasGroup.DOFade(1f, FadeDuration))
                .AppendInterval(FadeDuration)
                .Append(canvasGroup.DOFade(0f, FadeDuration))
                .OnComplete(() => canvasGroup.TrySetActiveOptimized(false));
        }

        protected override void OnDiscard() {
            _fadeSequence.Kill();
            canvasGroup.alpha = 0f;
            canvasGroup.TrySetActiveOptimized(false);
        }
    }
}