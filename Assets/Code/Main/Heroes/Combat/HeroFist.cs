using Awaken.TG.Assets;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class HeroFist : CharacterWeapon {
        [SerializeField] bool mainHand;
        
        [UnityEngine.Scripting.Preserve] public bool MainHand => mainHand;
        [UnityEngine.Scripting.Preserve] public bool OffHand => !mainHand;

        protected override string[] LayersToEnable => layersToEnable;
        protected override ARAssetReference AnimatorControllerRef => Hero.TppActive ? animatorControllerRefTpp : animatorControllerRef;
        
        protected override void OnAttachedToHero(Hero hero) {
            base.OnAttachedToHero(hero);
            if (!mainHand) {
                transform.SetParent(hero.OffHand);
                transform.localPosition = Vector3.zero;
                transform.localScale = Vector3.one;
            }
        }
    }
}