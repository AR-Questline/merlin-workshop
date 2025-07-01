using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    public class VCWeaponDissolveController : VCManualDissolveController {
        [SerializeField] bool hideOnSpawn;

        protected override void OnAttach() {
            base.OnAttach();
            
            if (hideOnSpawn) {
                SwitchVisibility(false);
            }
        }
        
        protected override void AttachListeners() {
            Target.Character.ListenTo(ICharacter.Events.SwitchCharacterWeaponVisibility, SwitchVisibility, this);
            if (Target.Character is NpcElement npc) {
                npc.ListenTo(NpcElement.Events.AnimatorExitedAttackState, _ => SwitchVisibility(false), this);
            }
        }
        
        protected override void SpawnAppearVFX() {
            if (appearVFX.IsSet) {
                foreach (var actualRenderer in _actualRenderers) {
                    PrefabPool.InstantiateAndReturn(appearVFX, Vector3.zero, Quaternion.identity, parent: actualRenderer.transform).Forget();
                }
            }
        }

        protected override void SpawnDisappearVFX() {
            if (disappearVFX.IsSet) {
                foreach (var actualRenderer in _actualRenderers) {
                    PrefabPool.InstantiateAndReturn(disappearVFX, Vector3.zero, Quaternion.identity, parent: actualRenderer.transform).Forget();
                }
            }
        }
    }
}