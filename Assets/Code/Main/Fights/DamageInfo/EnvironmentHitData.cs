using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Views;
using UnityEngine;

namespace Awaken.TG.Main.Fights.DamageInfo {
    public struct EnvironmentHitData {
        public VLocation Location { get; init; }
        public Item Item { get; init; }
        public Rigidbody Rigidbody { get; init; }
        public Vector3 Position { get; init; }
        public Vector3 Direction { get; init; }
        public float RagdollForce { get; init; }
    }
}