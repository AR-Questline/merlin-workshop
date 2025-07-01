using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class HeroLight : RotateAroundParent {
        Transform _center;
        protected override Transform Center => _center;

        protected override void Start() {
            base.Start();
            Init();
        }

        protected override void Init() {
            _center = Hero.Current.VHeroController.FirePoint;
            base.Init();
        }
    }
}