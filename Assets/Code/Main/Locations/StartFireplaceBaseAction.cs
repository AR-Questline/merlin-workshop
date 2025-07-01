using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations {
    public abstract partial class StartFireplaceBaseAction : AbstractLocationAction {
        const float HeroDistance = 2.5f;
        const float HeroPositionToleranceSqr = 0.2f * 0.2f;
        
        protected bool _heroInteracting;
        protected TabSetConfig _cookingTabSetConfig;
        protected TabSetConfig _alchemyTabSetConfig;

        [UnityEngine.Scripting.Preserve] protected abstract bool ManualRestTime { get; }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            _heroInteracting = true;
            GetPositions(hero, out var heroPos);
            PlayHeroAnimation(hero, heroPos);
            
            InitUI();
        }

        protected abstract void InitUI();

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return hero.IsInCombat() ? ActionAvailability.Disabled : base.GetAvailability(hero, interactable);
        }
        
        void PlayHeroAnimation(Hero hero, Vector3 heroPos) {
            var vHeroController = hero.VHeroController;
            if (!vHeroController.IsCrouching) {
                vHeroController.StoryBasedCrouch(true, 1f);
            }
            SnapHero(hero, vHeroController, heroPos).Forget();
        }

         void EndHeroAnimation(Hero hero, bool instant) {
            var vHeroController = hero.VHeroController;
            if (!vHeroController.IsCrouching) {
                vHeroController.StoryBasedCrouch(false, instant ? 0f : 1f);
            }
         }
        
        async UniTaskVoid SnapHero(Hero hero, VHeroController vHeroController, Vector3 newPos) {
            var newPosAs2D = new Vector2(newPos.x, newPos.z);
            var heroCoordsAs2D = new Vector2(hero.Coords.x, hero.Coords.z);
            while (_heroInteracting && Mathf.Abs((heroCoordsAs2D - newPosAs2D).sqrMagnitude) > HeroPositionToleranceSqr) {
                if (Time.deltaTime != 0) {
                    vHeroController.MoveTowards(newPos - hero.Coords);
                }
                if (!await AsyncUtil.DelayFrame(this)) {
                    return;
                }
                heroCoordsAs2D = new Vector2(hero.Coords.x, hero.Coords.z);
            }
        }
        
        protected void EndFireplaceInteraction(bool instant) {
            if (!_heroInteracting) {
                return;
            }
            
            EndHeroAnimation(Hero.Current, instant);
            FinishInteraction(Hero.Current, ParentModel);
            _heroInteracting = false;
        }
        
        void GetPositions(Hero hero, out Vector3 heroPos) {
            Vector3 dir = new Vector3(ParentModel.Coords.x - hero.Coords.x, 0, ParentModel.Coords.z - hero.Coords.z).normalized;
            heroPos = ParentModel.Coords - (dir.normalized * HeroDistance);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            EndFireplaceInteraction(fromDomainDrop);
            base.OnDiscard(fromDomainDrop);
        }
    }
}