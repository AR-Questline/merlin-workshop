using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.Grounds {
    /// <summary>
    /// Used to encapsulate target position, that might either be a moving IGrounded object, or a static position. 
    /// </summary>
    public class GroundedPosition {
        const float DefaultCharacterHeadHeight = 2f;
        
        readonly IGrounded _grounded;
        readonly Vector3 _position;
        readonly bool _hadGrounded;

        public Vector3 Coords => _grounded is { HasBeenDiscarded: false } ? _grounded.Coords : _position;

        public Vector3 HeadPosition {
            get {
                if (_grounded is ICharacter character && character.Head != null) {
                    return character.Head.position;
                }
                return Coords + new Vector3(0, DefaultCharacterHeadHeight, 0);
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public bool IsInvalid => _hadGrounded && _grounded.HasBeenDiscarded;
        public IGrounded Grounded => _grounded;

        public static GroundedPosition ByGrounded(IGrounded grounded) {
            if (grounded == null || grounded.HasBeenDiscarded) {
                return null;
            }
            return new GroundedPosition(grounded, Vector3.zero);
        }

        public static GroundedPosition ByPosition(Vector3 pos) {
            return new GroundedPosition(null, pos);
        }

        GroundedPosition(IGrounded grounded, Vector3 position) {
            _grounded = grounded;
            _position = position;
            _hadGrounded = _grounded is { HasBeenDiscarded: false };
        }

        public bool IsEqualTo(IGrounded another) {
            return _grounded == another;
        }
        
        public override bool Equals(object obj) {
            if (obj is GroundedPosition position) {
                return Coords.EqualsApproximately(position.Coords, 0.001f);
            }
            return false;
        }

        public override int GetHashCode() {
            return Coords.GetHashCode();
        }

        public string DebugName() {
            return _grounded switch {
                null => $"Position {_position}",
                Hero hero => $"Hero {hero.Coords}",
                NpcElement npc => $"npc.Name {npc.Coords}",
                _ => "Other"
            };
        }

        public static GroundedPosition HeroPosition => ByGrounded(Hero.Current);
    }
}