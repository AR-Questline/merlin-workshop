using System;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Utility;
using Awaken.Utility.Maths;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement {
    [TypeIcon(typeof(Transform))]
    public readonly struct CharacterPlace : IEquatable<CharacterPlace> {
        public Vector3 Position { get; }
        public float Radius { get; }
        
        public IGrounded Target { get; }

        public CharacterPlace(Vector3 position, float radius) {
            this.Position = position;
            this.Radius = radius;
            this.Target = null;
        }
        
        public CharacterPlace(IGrounded target, float radius) {
            this.Position = target.Coords;
            this.Radius = radius;
            this.Target = target;
        }

        public bool Contains(Vector3 point) {
            return DistanceSq(point) <= Radius * Radius && Mathf.Abs(Position.y - point.y) <= Radius * 2;
        }

        public float DistanceSq(Vector3 point) {
            return (point.ToHorizontal3() - Position.ToHorizontal3()).sqrMagnitude;
        }

        public static readonly CharacterPlace Default = new CharacterPlace(Vector3.zero, 0.5f);

        public bool ApproximatelyEqual(CharacterPlace b, float approximation) {
            return Radius == b.Radius && Position.EqualsApproximately(b.Position, approximation) && Target == b.Target;
        }
        
        public static bool operator ==(CharacterPlace a, CharacterPlace b) {
            return a.Position == b.Position && Mathf.Approximately(a.Radius, b.Radius);
        }

        public static bool operator !=(CharacterPlace a, CharacterPlace b) {
            return !(a == b);
        }
        
        public bool Equals(CharacterPlace other) {
            return Position.Equals(other.Position) && Radius.Equals(other.Radius);
        }

        public override bool Equals(object obj) {
            return obj is CharacterPlace other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (Position.GetHashCode() * 397) ^ Radius.GetHashCode();
            }
        }
    }
}