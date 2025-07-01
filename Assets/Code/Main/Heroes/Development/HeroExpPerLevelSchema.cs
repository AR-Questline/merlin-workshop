using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Development {
    public class HeroExpPerLevelSchema : ScriptableObject {
        public int[] exp = Array.Empty<int>();
        [Header("After initial list: Exp = A * heroLevel + B")]
        public float a;
        public float b;

        [ShowInInspector]
        public float FollowingLevel => (exp.Length + 2) * a + b;
    }
}
