using Awaken.TG.Main.Character;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public struct DeflectedProjectileParameters {
        public readonly ICharacter newOwner;
        public readonly Vector3 newDirection;
            
        public DeflectedProjectileParameters(ICharacter newOwner, Vector3 newDirection) {
            this.newOwner = newOwner;
            this.newDirection = newDirection;
        }
    }
}