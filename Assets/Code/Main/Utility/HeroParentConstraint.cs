using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Settings.Controllers;
using UnityEngine;
using UnityEngine.Animations;

namespace Awaken.TG.Main.Utility {
    public class HeroParentConstraint : StartDependentView<Hero> {
        [SerializeField] ParentConstraint parentConstraint;

        protected override void OnFullyInitialized() {
            var constrainSource = new ConstraintSource {
                weight = 1f,
                sourceTransform = Target.VHeroController.transform,
            };
            parentConstraint.AddSource(constrainSource);
        }
    }
}
