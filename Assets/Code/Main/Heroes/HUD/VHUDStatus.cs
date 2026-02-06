using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.BuildUp;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD {
    [UsesPrefab("HUD/VHUDStatus")]
    public class VHUDStatus : View<CharacterStatuses> {
        const float FadeDuration = 0.5f;
        static readonly int Grayscale = Shader.PropertyToID("_Grayscale");

        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Image statusIcon;
        [SerializeField] Image buildupProgress;
        [SerializeField] Image statusProgress;
        [SerializeField] TMP_Text stackLvl;
        [SerializeField] Image buffBackground;
        [SerializeField] Image debuffBackground;
        [SerializeField] Image buildupBackground;
        [SerializeField] Image buffFrame;
        [SerializeField] Image debuffFrame;
        [SerializeField] Image buildupFrame;
        [SerializeField] Material iconMaterial;
        [SerializeField] GameObject resistantIcon;
        [SerializeField] GameObject vulnerableIcon;

        bool _wasInitialized = false;
        Material _statusIconMaterial;
        BuildupStatus _buildup;

        StatusSourceInfo _sourceInfo;
        StatusType _statusType;
        Status _status;
        Sequence _fadeSequence;
        string _currentStackText;

        bool StatusHasBeenDiscarded => _status?.HasBeenDiscarded ?? true;
        StatusDuration StatusDuration => StatusHasBeenDiscarded ? null : _status?.DurationWrapper;
        bool IsStatusActive => StatusDuration != null;
        bool IsStatusInfinite => Duration?.IsInfinite ?? true;
        float TimeLeftNormalized => Duration?.TimeLeftNormalized ?? 1;
        bool IsDebuff => _statusType == StatusType.Debuff || _statusType == StatusType.Curse || _statusType == StatusType.Sin;

        public TimeDuration Duration => StatusDuration?.Duration as TimeDuration;
        public Status TargetStatus => _status;

        public void Init(StatusSourceInfo sourceInfo, Status status) {
            canvasGroup.alpha = 0;
            if (_sourceInfo != null) {
                Log.Important?.Error("Status source info already assigned!");
                return;
            }

            _sourceInfo = sourceInfo;
            _statusType = status.Type;
            _status = status;

            if (status.HasDuration) {
                statusProgress.fillAmount = 1 - TimeLeftNormalized;
            }

            InitialSetup();
        }

        public void AssignTargetStatus(Status status) {
            ClearListeners();

            _status = status;
            _statusType = status.Type;

            if (status.HasDuration) {
                statusProgress.fillAmount = 1 - TimeLeftNormalized;
            }

            if (_wasInitialized) OnTargetStatusChanged();
        }

        void InitialSetup() {
            DisableResistantIcons();

            if (_sourceInfo == null) {
                Log.Important?.Error("Status source info not assigned!");
                return;
            }

            _wasInitialized = true;

            _statusIconMaterial = new Material(iconMaterial);
            statusIcon.material = _statusIconMaterial;

            ShareableSpriteReference shareableSpriteReference = _sourceInfo.Icon;
            if (shareableSpriteReference is { IsSet: true }) {
                shareableSpriteReference.RegisterAndSetup(this, statusIcon);
            }

            OnTargetStatusChanged();
            Fade();
        }

        void OnTargetStatusChanged() {
            ResetGameObjectStates();
            if (_status is BuildupStatus buildupStatus) {
                BuildupStatusAttached(buildupStatus);
                return;
            }

            RegularStatusAttached();
        }

        void BuildupStatusAttached(BuildupStatus buildupStatus) {
            buildupBackground.TrySetActiveOptimized(true);
            buildupFrame.TrySetActiveOptimized(true);

            _status.ListenTo(Model.Events.AfterElementsCollectionModified, TryListenToStatusBuildup, this);
            TryListenToStatusBuildup(buildupStatus);

            if (!buildupStatus.Active) {
                _statusIconMaterial.SetFloat(Grayscale, 1);
                debuffBackground.TrySetActiveOptimized(false);
                debuffFrame.TrySetActiveOptimized(false);
                buffBackground.TrySetActiveOptimized(false);
                buffFrame.TrySetActiveOptimized(false);
            } else {
                _statusIconMaterial.SetFloat(Grayscale, 0);
                debuffBackground.TrySetActiveOptimized(IsDebuff);
                debuffFrame.TrySetActiveOptimized(IsDebuff);
                buffBackground.TrySetActiveOptimized(!IsDebuff);
                buffFrame.TrySetActiveOptimized(!IsDebuff);
            }
        }

        void RegularStatusAttached() {
            statusProgress.gameObject.SetActive(true);

            _statusIconMaterial.SetFloat(Grayscale, 0);
            debuffBackground.TrySetActiveOptimized(IsDebuff);
            debuffFrame.TrySetActiveOptimized(IsDebuff);
            buffBackground.TrySetActiveOptimized(!IsDebuff);
            buffFrame.TrySetActiveOptimized(!IsDebuff);

            _status.ListenTo(Model.Events.AfterElementsCollectionModified, TryListenToStatusBuildup, this);
            var statusBuildup = _status.TryGetElement<BuildupStatus>();
            TryListenToStatusBuildup(statusBuildup);
        }

        void ResetGameObjectStates() {
            _statusIconMaterial.SetFloat(Grayscale, 0);
            gameObject.SetActiveOptimized(true);
            buildupProgress.TrySetActiveOptimized(false);
            statusProgress.TrySetActiveOptimized(false);
            stackLvl.TrySetActiveOptimized(false);

            buildupBackground.TrySetActiveOptimized(false);
            buildupFrame.TrySetActiveOptimized(false);

            debuffBackground.TrySetActiveOptimized(false);
            debuffFrame.TrySetActiveOptimized(false);
            buffBackground.TrySetActiveOptimized(false);
            buffFrame.TrySetActiveOptimized(false);
        }

        /// <summary>
        /// Both buildup and regular status can have buildup
        /// </summary>
        void TryListenToStatusBuildup(Model elementAddedRemoved) {
            if (elementAddedRemoved == null || elementAddedRemoved.HasBeenDiscarded) return;
            if (elementAddedRemoved is not BuildupStatus newBuildup) return;

            if (_buildup is { HasBeenDiscarded: false }) {
                _buildup.Discard();
            }

            _buildup = newBuildup;
            _buildup.ListenTo(Model.Events.AfterDiscarded, OnBuildUpDiscarded, this);
            _buildup.ListenTo(Model.Events.AfterChanged, OnBuildupChanged, this);

            bool isStatusActive = IsStatusActive;
            bool useStatusProgress = isStatusActive || _buildup.Active;

            statusProgress.gameObject.SetActive(useStatusProgress);
            buildupProgress.gameObject.SetActive(!useStatusProgress);

            if (isStatusActive) {
                statusProgress.fillAmount = 1 - TimeLeftNormalized;
            } else {
                if (useStatusProgress) {
                    statusProgress.fillAmount = 1 - _buildup.BuildupProgress;
                } else {
                    buildupProgress.fillAmount = _buildup.BuildupProgress;
                }
            }
        }

        void ClearListeners() {
            if (_status != null) {
                World.EventSystem.RemoveAllListenersBetween(this, _status);
                if (_buildup != null) {
                    World.EventSystem.RemoveAllListenersBetween(this, _buildup);
                    _buildup = null;
                }
            }
        }

        void OnBuildupChanged(Model buildup) {
            DisableResistantIcons();

            if (_buildup.Active) {
                statusProgress.TrySetActiveOptimized(true);
                buildupProgress.TrySetActiveOptimized(false);
                buildupBackground.TrySetActiveOptimized(false);
                buildupFrame.TrySetActiveOptimized(false);

                _statusIconMaterial.SetFloat(Grayscale, 0);
                bool isDebuff = _statusType == StatusType.Debuff;
                debuffBackground.TrySetActiveOptimized(isDebuff);
                debuffFrame.TrySetActiveOptimized(isDebuff);
                buffBackground.TrySetActiveOptimized(!isDebuff);
                buffFrame.TrySetActiveOptimized(!isDebuff);
            } else if (_buildup.Character is NpcElement npc) {
                var npcStatValue = npc.Stat(_buildup.BuildupStatusStatusType.BuildupStatType);
                SetResistantIcons(npcStatValue.ModifiedValue, npc.Tier);
            }
        }

        void OnBuildUpDiscarded(Model buildup) {
            if (buildup == _buildup) {
                _buildup = null;
            }
        }

        void Update() {
            if (_status.CanStack || _status.StackLevel > 0) {
                stackLvl.TrySetActiveOptimized(true);
                _currentStackText = (_status.StackLevel + 1).ToString();
                if (!string.IsNullOrEmpty(_currentStackText) && stackLvl.text != _currentStackText) {
                    stackLvl.text = _currentStackText;
                }
            } else {
                stackLvl.TrySetActiveOptimized(false);
            }

            if (IsStatusActive) {
                if (!IsStatusInfinite) {
                    statusProgress.fillAmount =
                        _status.Template.invertProgressUI ? TimeLeftNormalized : 1 - TimeLeftNormalized;
                }

                return;
            }

            if (_buildup is { HasBeenDiscarded: false }) {
                if (_buildup.Active) {
                    statusProgress.fillAmount = 1 - _buildup.BuildupProgress;
                } else {
                    buildupProgress.fillAmount = _buildup.BuildupProgress;
                }
            }
        }

        void SetResistantIcons(float currentValue, int tier) {
            var buildupThreshold = StatusStatsValues.GetThreshold(currentValue, tier);

            switch (buildupThreshold) {
                case StatusStatsValues.StatusBuildupThreshold.Weak:
                    SetResistantIcons(false, true);
                    return;
                case StatusStatsValues.StatusBuildupThreshold.Normal:
                case StatusStatsValues.StatusBuildupThreshold.CantGet:
                    DisableResistantIcons();
                    return;
                case StatusStatsValues.StatusBuildupThreshold.Resistant:
                    SetResistantIcons(true, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void SetResistantIcons(bool resistantEnabled, bool vulnerableEnabled) {
            resistantIcon.SetActiveOptimized(resistantEnabled);
            vulnerableIcon.SetActiveOptimized(vulnerableEnabled);
        }

        void DisableResistantIcons() {
            SetResistantIcons(false, false);
        }

        void Fade() {
            _fadeSequence.Kill();
            _fadeSequence = DOTween.Sequence()
                .Append(canvasGroup.DOFade(1f, FadeDuration))
                .AppendInterval(FadeDuration);
        }

    }
}
