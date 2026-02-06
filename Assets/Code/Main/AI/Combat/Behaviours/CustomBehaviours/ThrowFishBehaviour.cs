using Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.VisualGraphUtils;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours {
    public class ThrowFishBehaviour : FireballBehaviour {
        static readonly Vector3 AngularVelocity = new(-1.71634483f, 4.35797739f, -1.7499131f);

        public StoryBookmark storyToTriggerOnHit;
        
        public override bool CanBeUsed => true;
        
        protected override ProjectileWrapper FireProjectile(CombatBehaviourUtils.FireProjectileParams fireParams,
            VGUtils.ShootParams shootParams) {
            var projectileWrapper = base.FireProjectile(fireParams, shootParams);
            projectileWrapper.ConfigureHomingOnContactProjectile(Hero.Current,
                () => {
                    var config = StoryConfig.Base(storyToTriggerOnHit, typeof(VReadablePopupUI));
                    Story.StartStory(config);
                    DiscardOwnerLocation();
                }, DiscardOwnerLocation);
            projectileWrapper.AddAngularVelocityToProjectile(AngularVelocity);
            return projectileWrapper;
        }

        void DiscardOwnerLocation() {
            if (HasBeenDiscarded) {
                return;
            }
            
            if (Npc.TryGetElement(out DuelistElement duelist)) {
                duelist.Defeat(true);
            }

            ParentModel.ParentModel.Discard();
        }
    }
}