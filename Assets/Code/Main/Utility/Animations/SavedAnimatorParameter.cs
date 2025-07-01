using Awaken.Utility;
using System;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations {
    [Serializable]
    public partial struct SavedAnimatorParameter {
        public ushort TypeForSerialization => SavedTypes.SavedAnimatorParameter;

        [Saved] public AnimatorControllerParameterType type;
        [ShowIf("@type == UnityEngine.AnimatorControllerParameterType.Bool")]
        [Saved] public bool boolValue;
        [ShowIf("@type == UnityEngine.AnimatorControllerParameterType.Float")]
        [Saved] public float floatValue;
        [ShowIf("@type == UnityEngine.AnimatorControllerParameterType.Int")]
        [Saved] public int intValue;
    }
}