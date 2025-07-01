namespace Awaken.TG.Main.Animations.FSM.Heroes.Base {
    /// <summary>
    /// In what logical state animator is currently in. This is not directly connected to state in animator but rather to what is animator 'doing' at current time.
    /// So for example, when we are in Block state, we can be raising block, pommeling, getting impact etc. but all this states are in GeneralState - Block.
    /// </summary>
    public enum HeroGeneralStateType : byte {
        UnEquip = 0,
        General = 1,
        HeavyAttack = 2,
        LightAttack = 3,
        Block = 4,
        BowDraw = 5,
        
        Interaction = 7,
        BackStab = 8,
        
        MagicCastLight = 9,
        MagicCastHeavy = 10,
        
        Finisher = 11,
        Jumping = 12,
        Sliding = 13,
    }
    
    public enum HeroLayerType : byte {
        MainHand = 0,
        OffHand = 1,
        BothHands = 2,
        DualMainHand = 3,
        DualOffHand = 4,
        Tools = 5,
        Fishing = 6,
        Spyglass = 7,
        Overrides = 8,
        Legs = 9,
        
        HeadMainHand = 10,
        HeadOffHand = 11,
        HeadBothHands = 12,
        HeadTools = 13,
        HeadFishing = 14,
        HeadSpyglass = 15,
        HeadOverrides = 16,
        
        ActiveMainHand = 18,
        ActiveOffHand = 19,
        
        CameraShakes = 20,
    }
}