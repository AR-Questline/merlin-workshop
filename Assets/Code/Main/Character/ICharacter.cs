using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Character {
    public interface ICharacter : IAlive, ITagged, IWithFaction, IEquipTarget, IWithHealthBar, FightingPair.ILeft, FightingPair.IRight {
        string Name { get; }
        bool IsBlocking { get; }
        bool IsBlinded { get; }
        float Height { get; }
        int Tier { get; }

        CharacterStats CharacterStats { get; }
        CharacterStats.ITemplate CharacterStatsTemplate { get; }
        StatusStats StatusStats { get; }
        StatusStats.ITemplate StatusStatsTemplate { get; }
        ProficiencyStats ProficiencyStats { get; }

        CharacterStatuses Statuses { get; }
        ICharacterSkills Skills { get; }
        ICharacterView CharacterView { get; }
        new ICharacterInventory Inventory { get; }
        IInventory IItemOwner.Inventory => Inventory;
        ICharacter IItemOwner.Character => this;
        IEquipTarget IItemOwner.EquipTarget => this;
        bool IItemOwner.CanUseAdditionalHands => AdditionalMainHand != null && AdditionalOffHand != null;
        
        CrimeOwners GetCurrentCrimeOwnersFor(CrimeArchetype type);
        /// <summary>
        /// You should use <see cref="GetCurrentCrimeOwnersFor"/> in most cases as it takes into account regions
        /// </summary>
        CrimeOwnerTemplate DefaultCrimeOwner { get; }
        IAIEntity AIEntity { get; }

        Transform MainHand { get; }
        [CanBeNull] Transform AdditionalMainHand => null;
        Transform OffHand { get; }
        [CanBeNull] Transform AdditionalOffHand => null;
        Transform Head { get; }
        Transform Torso { get; }
        Transform Hips { get; }
        VFXBodyMarker VFXBodyMarker { get; }

        Transform IWithItemSockets.HeadSocket => Head;
        Transform IWithItemSockets.MainHandSocket => MainHand;
        Transform IWithItemSockets.AdditionalMainHandSocket => AdditionalMainHand;
        Transform IWithItemSockets.OffHandSocket => OffHand;
        Transform IWithItemSockets.AdditionalOffHandSocket => AdditionalOffHand;
        Transform IWithItemSockets.HipsSocket => Hips;
        Transform IWithItemSockets.RootSocket => ParentTransform;

        float Radius { get; }
        bool DisableTargetRecalculation { get; set; }
        bool CanDealDamageToFriendlies { get; }
        Vector3 HorizontalVelocity { get; }
        float RelativeForwardVelocity { get; }
        [UnityEngine.Scripting.Preserve] float RelativeRightVelocity { get; }
        
        // --- Fighting
        public ref FightingPair.LeftStorage PossibleTargets { get; }
        public ref FightingPair.RightStorage PossibleAttackers { get; }
        
        ref FightingPair.LeftStorage FightingPair.ILeft.Storage => ref PossibleTargets;
        ref FightingPair.RightStorage FightingPair.IRight.Storage => ref PossibleAttackers;
        
        // --- WeaponHandling
        void AttachWeapon(CharacterHandBase characterHand);
        void DetachWeapon(CharacterHandBase characterHand);
        CharacterHandBase MainHandWeapon { get; }
        CharacterHandBase OffHandWeapon { get; }
        CharacterDealingDamage CharacterDealingDamage { get; }

        IView IWithBodyFeature.BodyView => CharacterView;
        
        // Staggering
        void EnterParriedState();

        // === Utils
        
        public static bool IsCharacterAlive(ICharacter character) {
            return character != null && !character.WasDiscarded && character.IsAlive;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static bool IsCharacterAlive(ICharacter character, bool countDyingAsAlive) {
            return IsCharacterAlive(character) && (countDyingAsAlive || !character.IsDying);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static bool IsDead(ICharacter character, bool countDyingAsDied) {
            return !IsCharacterAlive(character) || (countDyingAsDied && character.IsDying);
        }
        
        // === Events

        public new static class Events {
            public static readonly Event<ICharacter, ICharacter> CombatEntered = new(nameof(CombatEntered));
            public static readonly HookableEvent<ICharacter, ICharacter> TryingToExitCombat = new(nameof(TryingToExitCombat));
            public static readonly Event<ICharacter, ICharacter> CombatExited = new(nameof(CombatExited));
            public static readonly Event<ICharacter, ICharacter> CombatVictory = new(nameof(CombatVictory));
            public static readonly Event<ICharacter, ICharacter> CombatDisengagement = new(nameof(CombatDisengagement));
            public static readonly Event<ICharacter, AttackParameters> OnAttackStart = new(nameof(OnAttackStart));
            public static readonly Event<ICharacter, AttackParameters> OnAttackEnd = new(nameof(OnAttackEnd));
            public static readonly Event<ICharacter, AttackParameters> OnFailedAttackEnd = new(nameof(OnFailedAttackEnd));
            public static readonly Event<ICharacter, AttackParameters> OnSuccessfulAttackEnd = new(nameof(OnSuccessfulAttackEnd));
            public static readonly Event<ICharacter, ICharacter> OnRangedWeaponFullyDrawn = new(nameof(OnRangedWeaponFullyDrawn));
            public static readonly Event<ICharacter, DamageDealingProjectile> OnFiredProjectile = new(nameof(OnFiredProjectile));
            public static readonly Event<ICharacter, ICharacter> OnBowZoomStart = new(nameof(OnBowZoomStart));
            public static readonly Event<ICharacter, ICharacter> OnBowZoomEnd = new(nameof(OnBowZoomEnd));
            public static readonly Event<ICharacter, ICharacter> OnBowDrawStart = new(nameof(OnBowDrawStart));
            public static readonly Event<ICharacter, ICharacter> OnBowDrawEnd = new(nameof(OnBowDrawEnd));
            public static readonly Event<ICharacter, ICharacter> OnHeavyAttackHoldStarted = new(nameof(OnHeavyAttackHoldStarted));
            public static readonly Event<ICharacter, ICharacter> OnBlockBegun = new(nameof(OnBlockBegun));
            public static readonly Event<ICharacter, ICharacter> OnBlockEnded = new(nameof(OnBlockEnded));
            public static readonly Event<ICharacter, ICharacter> OnParryBegun = new(nameof(OnParryBegun));
            public static readonly Event<ICharacter, CastSpellData> CastingBegun = new(nameof(CastingBegun));
            public static readonly Event<ICharacter, CastSpellData> CastingFailed = new(nameof(CastingFailed));
            public static readonly Event<ICharacter, CastSpellData> CastingCanceled = new(nameof(CastingCanceled));
            public static readonly Event<ICharacter, CastSpellData> CastingEnded = new(nameof(CastingEnded));
            public static readonly Event<ICharacter, ARAnimationEventData> OnAttackRelease = new(nameof(OnAttackRelease));
            public static readonly Event<ICharacter, ARAnimationEventData> OnAttackRecovery = new(nameof(OnAttackRecovery));
            public static readonly Event<ICharacter, ARAnimationEventData> OnQuickUseItemUsed = new(nameof(OnQuickUseItemUsed));
            public static readonly Event<ICharacter, ARAnimationEventData> OnEffectInvokedAnimationEvent = new(nameof(OnEffectInvokedAnimationEvent));
            public static readonly Event<ICharacter, Item> OnEffectInvoked = new(nameof(OnEffectInvoked));
            public static readonly Event<ICharacter, MagicGauntletData> OnMagicGauntletHit = new(nameof(OnMagicGauntletHit));
            public static readonly Event<ICharacter, EnvironmentHitData> HitEnvironment = new(nameof(HitEnvironment));
            public static readonly Event<ICharacter, bool> HitAliveWithMeleeWeapon = new(nameof(HitAliveWithMeleeWeapon));
            public static readonly Event<ICharacter, ICharacter> ForceEnterStateIdle = new(nameof(ForceEnterStateIdle));
            [UnityEngine.Scripting.Preserve] public static readonly Event<ICharacter, ICharacter> ForceFlee = new(nameof(ForceFlee));
            public static readonly Event<ICharacter, bool> SwitchCharacterVisibility = new(nameof(SwitchCharacterVisibility));
            public static readonly Event<ICharacter, bool> SwitchCharacterWeaponVisibility = new(nameof(SwitchCharacterWeaponVisibility));
            public static readonly Event<ICharacter, TrialBuildupData> TriedToDealBuildupStatus = new(nameof(TriedToDealBuildupStatus));
            public static readonly Event<ICharacter, ICharacter> TriedToApplyInvulnerableStatus = new(nameof(TriedToApplyInvulnerableStatus));
        }
    }
}
