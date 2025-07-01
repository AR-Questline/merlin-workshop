using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class CharacterFist : CharacterWeapon {

        [SerializeField] bool mainHand;
        [SerializeField] bool reallySpawnTrailForFist;
        CharacterFist _offHandFist;

        [UnityEngine.Scripting.Preserve] public bool MainHand => mainHand;
        [UnityEngine.Scripting.Preserve] public bool OffHand => !mainHand;

        protected override void OnInitialize() {
            base.OnInitialize();
            transform.localRotation = Quaternion.identity;

            if (mainHand) {
                if (transform.childCount <= 0) {
                    Log.Important?.Error("Fist prefab doesn't have OffHand prefab nested! Fix that!");
                    return;
                }
                _offHandFist = transform.GetChild(0).GetComponent<CharacterFist>();
                World.BindView(Target, _offHandFist);
            }
        }

        protected override void AttachWeaponEventsListener() {
            if (mainHand) {
                base.AttachWeaponEventsListener();
            }
        }

        protected override UniTaskVoid InstantiateWeaponTrail() {
            return reallySpawnTrailForFist ? base.InstantiateWeaponTrail() : new UniTaskVoid();
        }

        protected override void OnAttachedToHero(Hero hero) {
            base.OnAttachedToHero(hero);
            AttachOffHandFist(hero);
        }

        protected override void OnAttachedToNpc(NpcElement npcElement) {
            base.OnAttachedToNpc(npcElement);
            AttachOffHandFist(npcElement);
        }

        void AttachOffHandFist(ICharacter owner) {
            if (!mainHand) {
                transform.SetParent(owner.OffHand);
                transform.localPosition = Vector3.zero;
                transform.localScale = Vector3.one;
            }
        }

        void OnDisable() {
            if (Owner?.Character is Hero) {
                OnWeaponHidden();
            }
        }

        protected override IBackgroundTask OnDiscard() {
            if (_offHandFist != null) {
                _offHandFist.Discard();
                _offHandFist = null;
            }
            return base.OnDiscard();
        }
    }
}