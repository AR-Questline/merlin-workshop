using System;
using Awaken.TG.Main.Localization;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions {
    [Serializable]
    public struct ReputationInfo {
        [SerializeField] public LocString name;
        [SerializeField] public LocString description;
        [SerializeField] public ReputationKind reputationKind;
    }

    [Serializable]
    public struct ReputationRow { 
        [SerializeField] ReputationInfo reputation0;
        [SerializeField] ReputationInfo reputation1;
        [SerializeField] ReputationInfo reputation2;
        [SerializeField] ReputationInfo reputation3;
        
        public ReputationInfo this[int index] {
            get {
                return index switch {
                    0 => reputation0,
                    1 => reputation1,
                    2 => reputation2,
                    3 => reputation3,
                    _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
                };
            }
        }
    }
}