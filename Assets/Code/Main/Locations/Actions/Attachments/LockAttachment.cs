using System;
using System.Collections.Generic;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Lockpicking;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Adds lock to the location.")]
    public class LockAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] LockType lockType;
        [Toggle("use")] public KeyLock keyLock;
        [SerializeField] bool startLocked = true;
        [SerializeField, EnumToggleButtons] LockAtTime lockedAtTime;
        [Space]
        [SerializeField, HideIf(nameof(OnlyKey)), Range(1,3)] 
        int complexity = 1;
        [SerializeField, HideIf(nameof(OnlyKey)), RichEnumExtends(typeof(LockTolerance)), Tooltip("Angle tolerance for unlocking the lock")] 
        RichEnumReference tolerance = LockTolerance.Easy;

        [SerializeField, HideIf(nameof(OnlyKey)), Space]
        bool randomized = true;

        [SerializeField, HideIf(nameof(OnlyKey)), ListDrawerSettings(IsReadOnly = true), DisableIf(nameof(randomized)), Range(0,180)]
        List<float> angles;

        public EventReference unlockSound;

        [FoldoutGroup(ILogicReceiverElement.GroupName)]
        public bool unlockOnStateChanged;
        [FoldoutGroup(ILogicReceiverElement.GroupName)]
        public bool lockOnStateChanged;
        
        bool OnlyKey => keyLock.keyOnly;
        
        public bool StartLocked => startLocked;

        public LockAtTime LockedAtTime => lockedAtTime;
        //public int Complexity => complexity;
        public int Complexity => 1; // removed complexity from locks
        public LockTolerance Tolerance => tolerance.EnumAs<LockTolerance>();
        public bool Randomized => randomized;
        public IEnumerable<float> Angles => angles;
        public Type Get3DViewType => lockType == LockType.Padlock ? typeof(VLockpicking3D_Padlock) : typeof(VLockpicking3D_Doorlock);

        public Element SpawnElement() {
            return new LockAction(this);
        }

        public bool IsMine(Element element) => element is LockAction;

        void OnValidate() {
            angles ??= new(complexity); // required for when component is just created. 
            while (angles.Count > complexity) {
                angles.RemoveAt(angles.Count -1);
            }
            while (angles.Count < complexity) {
                angles.Add(0);
            }
        }
        
        [Serializable]
        public class KeyLock {
            const int KeyRequiredLocsMaxIndex = 0;
            const int SigilRequiredLocsMaxIndex = 0;
            const int ClosedLocsMaxIndex = 0;
            const int NeutralLocsMaxIndex = 0;
            const int CantPassLocsMaxIndex = 0;

            public bool use;
            public bool keyOnly;
            [TemplateType(typeof(ItemTemplate)), SerializeField] TemplateReference templateReference;
            ItemTemplate _itemTemplate;

            [ShowIf(nameof(keyOnly)), LocStringCategory(Category.Interaction), OnValueChanged(nameof(RandomizeTextIndex))]
            public LockOverride lockTextOverride = LockOverride.Custom;
            [SerializeField, ShowIf(nameof(CustomLocStringOverride)), LocStringCategory(Category.Interaction)]
            LocString overrideLockedInfo;
            int _overrideLockedInfoIndex;
            
            [ShowInInspector]
            public string OverrideLockedInfo => GetOverridenLockedInfo().Translate();
            
            bool CustomLocStringOverride => keyOnly && lockTextOverride == LockOverride.Custom;
            bool ShowRandomizeTextIndexButton => keyOnly && lockTextOverride != LockOverride.Custom;

            public ItemTemplate ItemTemplate {
                get {
                    if (!use) {
                        return null;
                    }
                    if (_itemTemplate) {
                        return _itemTemplate;
                    }
                    return _itemTemplate = templateReference?.Get<ItemTemplate>();
                }
            }

            string GetOverridenLockedInfo() {
                return lockTextOverride switch {
                    LockOverride.Custom => overrideLockedInfo,
                    LockOverride.KeyRequired => LocTerms.LockKeyRequired0[..^1] + _overrideLockedInfoIndex,
                    LockOverride.SigilRequired => LocTerms.LockSigilRequired0[..^1] + _overrideLockedInfoIndex,
                    LockOverride.Closed => LocTerms.LockClosed0[..^1] + _overrideLockedInfoIndex,
                    LockOverride.Neutral => LocTerms.LockNeutral0[..^1] + _overrideLockedInfoIndex,
                    LockOverride.CantPass => LocTerms.LockCantPass0[..^1] + _overrideLockedInfoIndex,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            [Button, ShowIf(nameof(ShowRandomizeTextIndexButton))]
            void RandomizeTextIndex() {
                _overrideLockedInfoIndex = lockTextOverride switch {
                    LockOverride.Custom => 0,
                    LockOverride.KeyRequired => RandomUtil.UniformInt(0, KeyRequiredLocsMaxIndex),
                    LockOverride.SigilRequired => RandomUtil.UniformInt(0, SigilRequiredLocsMaxIndex),
                    LockOverride.Closed => RandomUtil.UniformInt(0, ClosedLocsMaxIndex),
                    LockOverride.Neutral => RandomUtil.UniformInt(0, NeutralLocsMaxIndex),
                    LockOverride.CantPass => RandomUtil.UniformInt(0, CantPassLocsMaxIndex),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            public enum LockOverride : byte {
                Custom,
                KeyRequired,
                SigilRequired,
                Closed,
                Neutral,
                CantPass,
            }
        }
    }

    public enum LockAtTime {
        [UnityEngine.Scripting.Preserve] Always = 0,
        [UnityEngine.Scripting.Preserve] OnlyAtDay = 1,
        [UnityEngine.Scripting.Preserve] OnlyAtNight = 2
    }

    enum LockType {
        [UnityEngine.Scripting.Preserve] Doorlock,
        [UnityEngine.Scripting.Preserve] Padlock,
    }
}