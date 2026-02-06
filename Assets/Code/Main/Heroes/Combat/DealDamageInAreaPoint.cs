using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class DealDamageInAreaPoint : Model, IGrounded {
        public override Domain DefaultDomain => Domain.Gameplay;
        public override bool IsNotSaved => true;
        
        public Vector3 Coords { get; }
        public Quaternion Rotation => Quaternion.identity;
        
        public DealDamageInAreaPoint(Vector3 coords) {
            Coords = coords;
        }
    }
}