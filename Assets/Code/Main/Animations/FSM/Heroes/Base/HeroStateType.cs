namespace Awaken.TG.Main.Animations.FSM.Heroes.Base {
    /// <summary>
    /// This states are directly connected with states in animator, each one represents state in which animator can be.
    /// </summary>
    public enum HeroStateType : byte {
        // --- General
        Idle = 0,
        Movement = 1,
        EquipWeapon = 2,
        UnEquipWeapon = 3,
        Empty = 4,
        // --- Heavy Attacks
        HeavyAttackStart = 5,
        HeavyAttackWait = 6,
        HeavyAttackEnd = 7,
        // --- Light Attacks
        LightAttackTired = 9,
        LightAttackForward = 10,
        LightAttackInitial = 11,
        LightAttackFirst = 12,
        LightAttackSecond = 13,
        // --- Blocking
        BlockStart = 14,
        BlockLoop = 15,
        BlockPommel = 16,
        BlockImpact = 17,
        BlockExit = 18,
        // --- Bow
        BowCancelDraw = 19,
        BowPull = 20,
        BowHold = 21,
        BowRelease = 22,
        // --- Magic
        MagicHeavyStart = 23,
        MagicHeavyLoop = 24,
        MagicHeavyEnd = 25,
        // --- Tools
        ToolInteract = 26,
        // --- Helpers
        Invalid = 27,
        // --- Overrides
        None = 28,
        TPose = 29,
        // --- CameraShakes
        ShakeLight = 30,
        ShakeMedium = 31,
        ShakeStrong = 32,
        // --- Dashing
        DashFront = 33,
        DashFrontLeft = 34,
        DashFrontRight = 35,
        DashRight = 36,
        DashLeft = 37,
        DashBack = 38,
        DashBackLeft = 39,
        DashBackRight = 40,
        // --- Weapon Buff
        WeaponBuff = 41,
        // --- Magic New
        MagicCancelCast = 47,
        // --- BackStabbing
        BackStabEnter = 48,
        BackStabLoop = 49,
        BackStabAttack = 50,
        BackStabExit = 51,
        // --- Tools New
        SpyglassExit = 52,
        // --- Interactions
        HeroCustomInteractionAnimation = 53,
        
        BlockParry = 54,
        
        //--- Mount
        PetMount = 55,
        PatMount = 56,
        Whistle = 57,
        //--- Throwables
        ThrowableThrow = 58,
        
        PetSharg = 59,
        HeroPraySuccess = 60,
        
        // --- Jumping
        JumpStart = 61,
        JumpEndLight = 62,
        JumpEndStrong = 63,
        
        // --- Fishing
        FishingThrow = 66,
        FishingIdle = 67,
        FishingCancel = 68,
        FishingBite = 69,
        FishingFail = 70,
        FishingBiteLoop = 71,
        FishingFightStart = 74,
        FishingInspect = 75,
        FishingPullOut = 76,
        FishingTakeFish = 77,
        FishingFight = 78,
        
        // --- New
        BlockStartWithoutShield = 100,
        BlockLoopWithoutShield = 101,
        BlockPommelWithoutShield = 102,
        BlockImpactWithoutShield = 103,
        BlockExitWithoutShield = 104,
        BlockParryWithoutShield = 105,
        
        MagicPerformMidCast = 106,
        MagicHeavyEndAlternate1 = 107,
        MagicHeavyEndAlternate2 = 108,
        MagicHeavyEndAlternate3 = 109,
        
        IdleAlternate = 110,
        MovementAlternate = 111,
        EquipWeaponAlternate = 112,
        UnEquipWeaponAlternate = 113,
        
        MagicLightInitial = 114,
        MagicLightFirst = 115,
        MagicLightSecond = 116,
        
        MagicHeavyChargeLoop = 117,
        MagicHeavyChargeIncrease = 118,
        
        MagicFailedCast = 119,
        
        Finisher = 120,
        
        // --- Hero Knockdown
        KnockdownEnter = 130,
        KnockdownAirLoop = 131,
        KnockdownHitGround = 132,
        KnockdownGroundLoop = 133,
        KnockdownEnd = 134,
        
        // --- Heavy Attack Alternate
        HeavyAttackStartAlternate = 135,
        HeavyAttackWaitAlternate = 136,
        HeavyAttackEndAlternate = 137,
        
        CrouchedIdle = 149,
        CrouchedMovement = 150,
        InAttackMovement = 151,
        
        LegsSlide = 155,
        
        LegsJumpStart = 160,
        LegsJumpLoop = 161,
        LegsJumpEnd = 162,
        LegsJumpEndMedium = 163,
        LegsJumpEndHigh = 164,
        
        HorseRidingMovement = 170,
        
        LegsSwimmingIdle = 180,
        LegsSwimmingMovement = 181,
    }
}