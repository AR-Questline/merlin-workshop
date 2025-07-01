using Awaken.TG.Main.Heroes.Statuses.BuildUp;
using Awaken.TG.Main.Utility.RichEnums;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Statuses.Attachments {
    public class BuildupAttachment : MonoBehaviour {
        [SerializeField] bool startActivated;
        [SerializeField] BuildupType buildupType;
        [SerializeField] BuildupConsumptionType buildupConsumptionType;
        [SerializeField, ShowIf(nameof(ShowNextStatus))] StatusTemplate nextStatusTemplate;
        [SerializeField, RichEnumExtends(typeof(BuildupStatusType))] RichEnumReference buildupStatusType;
        
        [FoldoutGroup("Advanced"), SerializeField] bool buildupDurationOverride;
        [FoldoutGroup("Advanced"), SerializeField, ShowIf(nameof(buildupDurationOverride))] float buildupDuration = BuildupStatus.DefaultDecayRateDuration;
        [FoldoutGroup("Advanced"), SerializeField] bool buildupGainOverride;
        [FoldoutGroup("Advanced"), SerializeField, ShowIf(nameof(buildupGainOverride))] float buildupGainMultiplier = 1f;
        [InfoBox("Effect Modifier should be used to modify power of effects in VS of status,\nbut buildup decay can use it to modify it's speed if no other modification is valid.\nWorks only when status is active (to modify buildup use buildup resistance not effect modifier).")]
        [FoldoutGroup("Advanced"), SerializeField] bool isDecayUsingEffectModifier = true;
        
        public bool StartActivated => startActivated;
        public BuildupType BuildupType => buildupType;
        public BuildupConsumptionType BuildupConsumptionType => buildupConsumptionType;
        public StatusTemplate NextStatusTemplate => nextStatusTemplate;
        public BuildupStatusType BuildupStatusType => buildupStatusType.EnumAs<BuildupStatusType>();
        
        public float BuildupDuration => buildupDurationOverride ? buildupDuration : BuildupStatus.DefaultDecayRateDuration;
        public float BuildupGainMultiplier => buildupGainOverride ? buildupGainMultiplier : 1f;
        public bool IsDecayUsingEffectModifier => isDecayUsingEffectModifier;

        bool ShowNextStatus => buildupType == BuildupType.ChangeStatusToDifferent;
    }

    public enum BuildupType {
        ActivateThisStatus,
        ChangeStatusToDifferent,
    }

    public enum BuildupConsumptionType {
        ConsumeMax,
        ClampToMax,
        Clear,
    }
}