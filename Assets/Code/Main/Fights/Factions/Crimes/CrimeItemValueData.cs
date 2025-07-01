using System;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public enum CrimeItemValue : byte {
        None,
        Low = 4,
        Medium = 8,
        High = 12,
    }

    public enum CrimeNpcValue : byte {
        None,
        Low = 4,
        Medium = 8,
        High = 12,
    }

    [Serializable]
    public struct CrimeItemValueData {
        public int theft;
        public float pickpocketLengthMultiplier;

        public CrimeItemValueData(int theft, float pickpocketLengthMultiplier) {
            this.theft = theft;
            this.pickpocketLengthMultiplier = pickpocketLengthMultiplier;
        }

        public static readonly CrimeItemValueData None = new(0, 0);
    }

    [Serializable]
    public struct CrimeNpcValueData {
        public int pickpocketMultiplier;
        public int pickpocketProficiencyBoost;
        [UnityEngine.Scripting.Preserve] public float pickpocketLengthMultiplier;
        public int murder;

        public CrimeNpcValueData(int pickpocketMultiplier, int pickpocketProficiencyBoost, float pickpocketLengthMultiplier, int murder) {
            this.pickpocketMultiplier = pickpocketMultiplier;
            this.pickpocketProficiencyBoost = pickpocketProficiencyBoost;
            this.pickpocketLengthMultiplier = pickpocketLengthMultiplier;
            this.murder = murder;
        }

        public static readonly CrimeNpcValueData None = new(1, 1000, 0f, 0);
    }

    public interface IWithCrimeNpcValue : IElement<Location> {
        CrimeNpcValue CrimeValue { get; }
    }
}