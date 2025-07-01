namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    public enum CombatBehaviourCooldown : byte {
        None = 0,
        UntilTimeElapsed = 1,
        UntilEndOfFight = 2,
        Forever = 100,
    }
}