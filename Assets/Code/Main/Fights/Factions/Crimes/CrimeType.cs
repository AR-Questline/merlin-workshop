using System;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public enum SimpleCrimeType : byte {
        None = 0,
        Trespassing = 1,
        Theft = 2,
        Pickpocket = 3,
        Combat = 4,
        Murder = 5,
        Lockpicking = 6,
        Custom = 7,
    }
    
    [Flags]
    public enum CrimeType : ushort {
        [UnityEngine.Scripting.Preserve] None = 0,
        [UnityEngine.Scripting.Preserve] Trespassing = 1 << (SimpleCrimeType.Trespassing - 1),
        [UnityEngine.Scripting.Preserve] Theft = 1 << (SimpleCrimeType.Theft - 1),
        [UnityEngine.Scripting.Preserve] Pickpocket = 1 << (SimpleCrimeType.Pickpocket - 1),
        [UnityEngine.Scripting.Preserve] Combat = 1 << (SimpleCrimeType.Combat - 1),
        [UnityEngine.Scripting.Preserve] Murder = 1 << (SimpleCrimeType.Murder - 1),
        [UnityEngine.Scripting.Preserve] Lockpicking = 1 << (SimpleCrimeType.Lockpicking - 1),
        [UnityEngine.Scripting.Preserve] Custom = 1 << (SimpleCrimeType.Custom - 1),
        [UnityEngine.Scripting.Preserve] All = ushort.MaxValue
    }
    
    public static class CrimeTypeUtils {
        public static CrimeType ToCrimeType(this SimpleCrimeType simple) {
            return simple == SimpleCrimeType.None ? CrimeType.None : (CrimeType) (1 << (int) (simple - 1));
        }
    }
}