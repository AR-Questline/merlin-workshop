using Awaken.Utility;
using System;
using Awaken.TG.Utility.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    [Serializable]
    public partial struct ConeDamageParameters {
        public ushort TypeForSerialization => SavedTypes.ConeDamageParameters;

        [Saved] public float angle;
        [Saved] public Vector3 forward;
        [Saved] public SphereDamageParameters sphereDamageParameters;
    }
}