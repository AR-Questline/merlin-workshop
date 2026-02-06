using System.Collections.Generic;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Utility.VFX {
    public class VCWeaponVFXController : ViewComponent<Item> {
        [SerializeField] bool hideOnSpawn;
        [SerializeField] List<VisualEffect> effects;
        [SerializeField] List<GameObject> objectsToEnable;
        
        protected override void OnAttach() {
            if (Target.Character == null) {
                return;
            }
            AttachListeners();
            if (hideOnSpawn) {
                SwitchVisibility(false);
            }
        }
        
        void AttachListeners() {
            Target.Character.ListenTo(ICharacter.Events.SwitchCharacterWeaponVisibility, SwitchVisibility, this);
            if (Target.Character is NpcElement npc) {
                npc.ListenTo(NpcElement.Events.AnimatorExitedAttackState, _ => SwitchVisibility(false), this);
            }
        }
        
        void SwitchVisibility(bool visible) {
            if (visible) {
                ActivateVfxs();
            } else {
                DeactivateVfxs();
            }
        }
        
        void ActivateVfxs() {
            foreach (var effect in effects) {
                effect.Play();
            }

            foreach (var objectToEnable in objectsToEnable) {
                objectToEnable.SetActive(true);
            }
        }
        
        void DeactivateVfxs() {
            foreach (var effect in effects) {
                VFXUtils.StopVfx(effect);
            }
            
            foreach (var objectToEnable in objectsToEnable) {
                objectToEnable.SetActive(false);
            }
        }
    }
}