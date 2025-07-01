namespace Awaken.TG.Main.Fights.DamageInfo {
    public enum DamageType : byte {
        None = 0,
        PhysicalHitSource = 1,
        MagicalHitSource = 2,
        Status = 3,
        Fall = 4,
        Interact = 5,
        Environment = 6,
        Trap = 7
    }
    
    public enum DamageSubType : byte {
        Default = 0,
        Pure = 1,
        Wyrdness = 2,
        //3-9 for more special
        GenericPhysical = 10,
        Slashing = 11,
        Piercing = 12,
        Bludgeoning = 13,
        //14-19 for more physicals
        GenericMagical = 20,
        Fire = 21,
        Cold = 22,
        Poison = 23,
        Electric = 24,
    }
}