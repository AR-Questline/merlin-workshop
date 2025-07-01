using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Statuses.Attachments;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Statuses {
    public class StatusTemplate : Template {
        [SerializeField, RichEnumExtends(typeof(StatusType))] 
        RichEnumReference statusType;
        public StatusType StatusType => statusType.EnumAs<StatusType>();

        public bool IsPositive => StatusType.IsPositive;

        [ShowAssetPreview] [ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true, AddressableGroup.StatusEffects)]
        public ShareableSpriteReference iconReference;

        [LocStringCategory(Category.Status)]
        public LocString displayName;
        [LocStringCategory(Category.Status)]
        public LocString description;

        [RichEnumExtends(typeof(Keyword)), SerializeField] 
        List<RichEnumReference> keywords = new();

        public bool notSaved; //TODO: Make 3 new events for statuses for restoring. This is a hack because most combat statuses can't be restored.
        [Space(10f)] 
        public bool hiddenOnUI;
        [HideIf(nameof(hiddenOnUI))]
        public bool hiddenOnHUD;

        public bool alwaysShowSeparately;
        public bool invertProgressUI;

        [SerializeField, Tooltip("If true, status add type will be changed to Add if the status has different source item.")]
        bool overrideToAddForDifferentItems;
        
        [SerializeField] 
        StatusAddType addType;

        [SerializeField, ShowIf("@" + nameof(addType) + " == StatusAddType.Stack"),
         ValidateInput(nameof(ValidateStackingAddType), "A stacking status can only use <b>Prolong</b> and <b>Renew</b>")]
        StatusAddType addTypeOnStacking = StatusAddType.Renew;

        [SerializeField, ShowIf("@" + nameof(addType) + " == StatusAddType.Stack")]
        bool timerShouldDeStack = false;

        [SerializeField, ShowIf(nameof(Upgradeable)), TemplateType(typeof(StatusTemplate))]
        TemplateReference upgradeReference;

        public int fightValueFactor;
        
        [Header("Skill")]
        [Space(5f), HideLabel] public SkillReference skill;

        public StatusTemplate UpgradeReference => upgradeReference.Get<StatusTemplate>();
        public IEnumerable<Keyword> Keywords => keywords.Select(k => k.EnumAs<Keyword>());

        public bool OverrideToAddForDifferentItems => overrideToAddForDifferentItems;
        public StatusAddType AddType => addType;
        public StatusAddType AddTypeOnStacking => addTypeOnStacking;
        public bool TimerShouldDeStackInsteadOfCancelingEffect => timerShouldDeStack;
        public bool IsBuildupAble => GetComponent<BuildupAttachment>();
        public bool Upgradeable => AddType is StatusAddType.Upgrade;
        [UnityEngine.Scripting.Preserve] public bool HiddenOnUI => hiddenOnUI;
        public bool CanBeApplied => GetComponent<StatusWithChanceAttachment>()?.CanBeApplied ?? true;
        public IDuration TryGetDuration() => GetComponent<IDurationAttachment>()?.SpawnDuration();
        
        // === EDITOR
#if UNITY_EDITOR
        [Button("Add to hero")]
        void EDITOR_AddPassive() {
            TemplatesUtil.EDITOR_AssignGuid(this, gameObject);
            CharacterStatuses.AddResult? addStatus = Hero.Current?.Statuses.AddStatus(this, StatusSourceInfo.FromStatus(this));
            if (addStatus != null) {
                Log.Important?.Info(addStatus.ToString());
            } else {
                Log.Important?.Error("Failed to add status, no hero found.");
            }
        }
#endif

        // === Odin
        bool ValidateStackingAddType(StatusAddType newType) {
            bool validValue;
            switch (newType) {
                case StatusAddType.Renew:
                case StatusAddType.Prolong:
                case StatusAddType.None:
                    validValue = true;
                    break;
                default:
                    validValue = false;
                    break;
            }

            if (!validValue) {
                addTypeOnStacking = StatusAddType.None;
            }

            return validValue;
        }
    }
}