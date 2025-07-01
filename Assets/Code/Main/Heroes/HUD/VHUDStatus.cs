using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.BuildUp;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Debugging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD {
    [UsesPrefab("HUD/VHUDStatus")]
    public class VHUDStatus : View<CharacterStatuses> {
        static readonly int Grayscale = Shader.PropertyToID("_Grayscale");
        
        [SerializeField] Image statusIcon;
        [SerializeField] Image buildupProgress;
        [SerializeField] Image statusProgress;
        [SerializeField] TMP_Text stackLvl;
        [SerializeField] Image buffBackground, debuffBackground, buildupBackground;
        [SerializeField] Material iconMaterial;

        bool _wasInitialized = false;
        Material _statusIconMaterial;
        BuildupStatus _buildup;
        
        StatusSourceInfo _sourceInfo;
        StatusType _statusType;
        Status _status;
        
        bool StatusHasBeenDiscarded => _status?.HasBeenDiscarded ?? true;
        StatusDuration StatusDuration => StatusHasBeenDiscarded ? null : _status?.DurationWrapper;
        bool IsStatusActive => StatusDuration != null;
        bool IsStatusInfinite => Duration?.IsInfinite ?? true;
        float TimeLeftNormalized => Duration?.TimeLeftNormalized ?? 1;
        
        public TimeDuration Duration => StatusDuration?.Duration as TimeDuration;
        public Status TargetStatus => _status;
        
        public void Init(StatusSourceInfo sourceInfo, Status status) {
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
            if (_sourceInfo == null) {
                Log.Important?.Error("Status source info not assigned!");
                return;
            }
            _wasInitialized = true;
            
            _statusIconMaterial = new Material(iconMaterial);
            statusIcon.material = _statusIconMaterial;
            
            ShareableSpriteReference shareableSpriteReference = _sourceInfo.Icon;
            if (shareableSpriteReference is {IsSet: true}) {
                shareableSpriteReference.RegisterAndSetup(this, statusIcon);
            }
            
            OnTargetStatusChanged();
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
            buildupBackground.gameObject.SetActive(true);
            
            _status.ListenTo(Model.Events.AfterElementsCollectionModified, TryListenToStatusBuildup, this);
            TryListenToStatusBuildup(buildupStatus);

            if (!buildupStatus.Active) {
                _statusIconMaterial.SetFloat(Grayscale, 1);
                debuffBackground.gameObject.SetActive(false);
                buffBackground.gameObject.SetActive(false);
            } else {
                _statusIconMaterial.SetFloat(Grayscale, 0);
                bool isDebuff = _statusType == StatusType.Debuff;
                debuffBackground.gameObject.SetActive(isDebuff);
                buffBackground.gameObject.SetActive(!isDebuff);
            }
        }

        void RegularStatusAttached() {
            statusProgress.gameObject.SetActive(true);
            
            _statusIconMaterial.SetFloat(Grayscale, 0);
            bool isDebuff = _statusType == StatusType.Debuff;
            debuffBackground.gameObject.SetActive(isDebuff);
            buffBackground.gameObject.SetActive(!isDebuff);
            
            _status.ListenTo(Model.Events.AfterElementsCollectionModified, TryListenToStatusBuildup, this);
            var statusBuildup = _status.TryGetElement<BuildupStatus>();
            TryListenToStatusBuildup(statusBuildup);
        }
        
        void ResetGameObjectStates() {
            _statusIconMaterial.SetFloat(Grayscale, 0);
            gameObject.SetActive(true);
            buffBackground.gameObject.SetActive(false);
            debuffBackground.gameObject.SetActive(false);
            buildupBackground.gameObject.SetActive(false);
            buildupProgress.gameObject.SetActive(false);
            statusProgress.gameObject.SetActive(false);
            stackLvl.gameObject.SetActive(false);
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
            if (_buildup.Active) {
                statusProgress.gameObject.SetActive(true);
                buildupProgress.gameObject.SetActive(false);
                buildupBackground.gameObject.SetActive(false);
            
                _statusIconMaterial.SetFloat(Grayscale, 0);
                bool isDebuff = _statusType == StatusType.Debuff;
                debuffBackground.gameObject.SetActive(isDebuff);
                buffBackground.gameObject.SetActive(!isDebuff);
            }
        }

        void OnBuildUpDiscarded(Model buildup) {
            if (buildup == _buildup) {
                _buildup = null;
            }
        }

        void Update() {
            if (_status.StackLevel > 0) {
                stackLvl.gameObject.SetActive(true);
                stackLvl.text = _status.StackLevel.ToString();
            } else {
                stackLvl.gameObject.SetActive(false);
            }
            
            if (IsStatusActive) {
                if (!IsStatusInfinite) {
                    statusProgress.fillAmount = _status.Template.invertProgressUI ? TimeLeftNormalized :  1 - TimeLeftNormalized;
                }
                return;
            }
            
            if (_buildup is {HasBeenDiscarded: false}) {
                if (_buildup.Active) {
                    statusProgress.fillAmount = 1 - _buildup.BuildupProgress;
                } else {
                    buildupProgress.fillAmount = _buildup.BuildupProgress;
                }
            }
        }
    }
}
