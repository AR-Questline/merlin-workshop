using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations {
    [UnityEngine.Scripting.Preserve, UnityEngine.Scripting.RequireDerived]
    public class ARAnimationEvent : ScriptableObject {
        [BoxGroup("Unity Event")] public ActionType actionType = ActionType.None;
        [BoxGroup("Unity Event")] public WeaponRestriction restriction;
        [BoxGroup("Unity Event")] public AttackType attackType = AttackType.Normal;
        [BoxGroup("Unity Event"), ShowIf(nameof(EDITOR_IsAppear))] public AppearType appearType = AppearType.Character;
        [BoxGroup("Unity Event"), ShowIf(nameof(EDITOR_IsTeleport))] public TeleportType teleportType = TeleportType.InFrontOfTarget;

        [BoxGroup("Audio"), RichEnumExtends(typeof(ARAudioType)), SerializeField]
        RichEnumReference[] audio = Array.Empty<RichEnumReference>();

        [BoxGroup("Magic Casting"), ShowIf(nameof(IsEventInvoke))]
        public bool overrideCastingPerformDelay;

        [BoxGroup("Magic Casting"), ShowIf(nameof(IsEventInvoke)), EnableIf(nameof(overrideCastingPerformDelay)), Range(0f, 2f)]
        public float delayNextCastForSeconds = 0.25f;

        public IEnumerable<ArmorAudioType> ArmorAudio => audio?.Select(a => a.EnumAs<ArmorAudioType>()).WhereNotNull() ?? Array.Empty<ArmorAudioType>();
        public IEnumerable<AliveAudioType> AliveAudio => audio?.Select(a => a.EnumAs<AliveAudioType>()).WhereNotNull() ?? Array.Empty<AliveAudioType>();

        bool IsEventInvoke => actionType == ActionType.EffectInvoke;

        public ARAnimationEventData CreateData() {
            return new ARAnimationEventData(actionType, restriction, attackType, overrideCastingPerformDelay, delayNextCastForSeconds, audio);
        }
        
        public enum ActionType : byte {
            [UnityEngine.Scripting.Preserve] None = 0,
            [UnityEngine.Scripting.Preserve] AttackRelease = 1,
            [UnityEngine.Scripting.Preserve] AttackRecovery = 2,
            [UnityEngine.Scripting.Preserve] AttackEnd = 3,
            [UnityEngine.Scripting.Preserve] DefenceStart = 4,
            [UnityEngine.Scripting.Preserve] DefenceEnd = 5,
            [UnityEngine.Scripting.Preserve] MagicSubmit = 6,
            [UnityEngine.Scripting.Preserve] MagicCancel = 7,
            [UnityEngine.Scripting.Preserve] SpecialAttackStart = 8,
            [UnityEngine.Scripting.Preserve] SpecialAttackTrigger = 9,
            [UnityEngine.Scripting.Preserve] HeroPerfectTimingRelease = 10,
            [UnityEngine.Scripting.Preserve] ShieldRecovery = 11,
            [UnityEngine.Scripting.Preserve] FinisherRelease = 12,
            [UnityEngine.Scripting.Preserve] BackStabRelease = 13,
            [UnityEngine.Scripting.Preserve] AttachWeapon = 14,
            [UnityEngine.Scripting.Preserve] ToolStartInteraction = 15,
            [UnityEngine.Scripting.Preserve] ToolEndInteraction = 16,
            [UnityEngine.Scripting.Preserve] EffectInvoke = 17,
            [UnityEngine.Scripting.Preserve] QuickUseItemUsed = 18,
            [UnityEngine.Scripting.Preserve] Appear = 19,
            [UnityEngine.Scripting.Preserve] Disappear = 20,
            [UnityEngine.Scripting.Preserve] TeleportOut = 21,
            [UnityEngine.Scripting.Preserve] TeleportIn = 22,
        }

        public enum AppearType : byte {
            [UnityEngine.Scripting.Preserve] Character = 0,
            [UnityEngine.Scripting.Preserve] Weapon = 1,
        }
        
        public enum TeleportType : byte {
            InFrontOfTarget = 0,
            Dash = 1
        }
        
        // ReSharper disable once InconsistentNaming
        bool EDITOR_IsTeleport => actionType == ActionType.TeleportOut;
        // ReSharper disable once InconsistentNaming
        bool EDITOR_IsAppear => actionType is ActionType.Appear or ActionType.Disappear;
    }

    [Serializable]
    public struct ARAnimationEventData {
        public ARAnimationEvent.ActionType actionType;
        public WeaponRestriction restriction;
        public AttackType attackType;
        public bool overrideCastingPerformDelay;
        public float delayNextCastForSeconds;
        [SerializeField] RichEnumReference[] audio;
        public bool CanBeInvokedInHitStop => CanActionTypeBeInvokedInHitStop(actionType);

        public ARAnimationEventData(
            ARAnimationEvent.ActionType actionType, 
            WeaponRestriction restriction, 
            AttackType attackType, 
            bool overrideCastingPerformDelay, 
            float delayNextCastForSeconds, 
            RichEnumReference[] audio
        ) {
            this.actionType = actionType;
            this.restriction = restriction;
            this.attackType = attackType;
            this.overrideCastingPerformDelay = overrideCastingPerformDelay;
            this.delayNextCastForSeconds = delayNextCastForSeconds;
            this.audio = audio;
        }

        public IEnumerable<ItemAudioType> ItemAudio => audio?.Select(a => a.EnumAs<ItemAudioType>()).WhereNotNull() ?? Array.Empty<ItemAudioType>();
        public IEnumerable<ArmorAudioType> ArmorAudio => audio?.Select(a => a.EnumAs<ArmorAudioType>()).WhereNotNull() ?? Array.Empty<ArmorAudioType>();
        public IEnumerable<AliveAudioType> AliveAudio => audio?.Select(a => a.EnumAs<AliveAudioType>()).WhereNotNull() ?? Array.Empty<AliveAudioType>();

        static bool CanActionTypeBeInvokedInHitStop(ARAnimationEvent.ActionType a) =>
            a switch {
                ARAnimationEvent.ActionType.AttackRelease => false,
                ARAnimationEvent.ActionType.AttackRecovery => false,
                ARAnimationEvent.ActionType.AttackEnd => false,
                ARAnimationEvent.ActionType.HeroPerfectTimingRelease => false,
                ARAnimationEvent.ActionType.FinisherRelease => false,
                ARAnimationEvent.ActionType.BackStabRelease => false,
                _ => true
            };
    }
}
