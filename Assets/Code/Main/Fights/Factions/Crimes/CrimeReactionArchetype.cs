namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public enum CrimeReactionArchetype : byte {
        /// <summary> no special behaviour </summary>
        None = 0,
        /// <summary> intervene when hero has bounty </summary>
        Guard = 1,
        /// <summary> initiate combat when he saw hero committed crime </summary>
        Defender = 2,
        /// <summary> initiate combat when hero has bounty </summary>
        Vigilante = 3,
        /// <summary> fear when when danger nearby, flee if in direct danger </summary>
        FleeingPeasant = 4,
        /// <summary> always flee when danger nearby </summary>
        AlwaysFleeing = 5,
    }
}