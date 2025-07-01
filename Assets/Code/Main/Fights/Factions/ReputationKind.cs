using System;

namespace Awaken.TG.Main.Fights.Factions {
    [Flags]
    public enum ReputationKind : ushort {
        None = 0,
        /// <summary>Neutral</summary>
        Rep00 = 1 << 0,
        /// <summary>Accepted</summary>
        Rep01 = 1 << 1,
        /// <summary>Liked</summary>
        Rep02 = 1 << 2,
        /// <summary>Idolized</summary>
        Rep03 = 1 << 3,
        
        /// <summary>Shunned</summary>
        Rep10 = 1 << 4,
        /// <summary>Mixed</summary>
        Rep11 = 1 << 5,
        /// <summary>Smiling Troublemaker</summary>
        Rep12 = 1 << 6,
        /// <summary>Good-Natured Rascal</summary>
        Rep13 = 1 << 7,
        
        /// <summary>Hated</summary>
        Rep20 = 1 << 8,
        /// <summary>Sneering Punk</summary>
        Rep21 = 1 << 9,
        /// <summary>Unpredictable</summary>
        Rep22 = 1 << 10,
        /// <summary>Dark Hero</summary>
        Rep23 = 1 << 11,
        
        /// <summary>Vilified</summary>
        Rep30 = 1 << 12,
        /// <summary>Merciful Thug</summary>
        Rep31 = 1 << 13,
        /// <summary>Soft-Hearted Devil</summary>
        Rep32 = 1 << 14,
        /// <summary>Wild Child</summary>
        Rep33 = 1 << 15,

        [UnityEngine.Scripting.Preserve] Positive = Rep01 | Rep02 | Rep03 | Rep12 | Rep13,
        [UnityEngine.Scripting.Preserve] Indifferent = Rep00 | Rep11 | Rep22 | Rep23 | Rep32 | Rep33,
        [UnityEngine.Scripting.Preserve] Negative = Rep10 | Rep20 | Rep21 | Rep30 | Rep31,
    }
}