using System.Collections.Generic;
using System.Threading;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.Utility.Extensions;
using Awaken.Utility.Maths;
using Awaken.Utility.SerializableTypeReference;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    public class VCManualDissolveController : VCDissolveControllerBase<IDissolveAble>, IDissolveAbleDissolveController {
        [SerializeField, MaterialPropertyComponent] SerializableTypeReference disappearSerializedType;
        [SerializeField] List<DissolveAbleRenderer> renderers;
        [ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        public ShareableARAssetReference appearVFX;
        [ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        public ShareableARAssetReference disappearVFX;
        
        protected bool _visible = true;

        protected override void OnAttach() {
            foreach (var dissolveAbleRenderer in renderers) {
                if (dissolveAbleRenderer) {
                    AddRenderer(dissolveAbleRenderer);
                }
            }

            AttachListeners();
            base.OnAttach();
        }

        protected virtual void AttachListeners() {
            if (Target.Character is not { } character) {
                return;
            }
            character.ListenTo(ICharacter.Events.SwitchCharacterVisibility, SwitchVisibility, this);
            if (character is NpcElement npc) {
                npc.ListenTo(NpcElement.Events.AnimatorExitedAttackState, _ => SwitchVisibility(true), this);
            }
        }

        public void SwitchVisibility(bool visible) {
            if (visible && !_visible) {
                SpawnAppearVFX();
                Appear();
                _visible = true;
            } else if (!visible && _visible) {
                SpawnDisappearVFX();
                Disappear().Forget();
                _visible = false;
            }
        }

        protected virtual void SpawnAppearVFX() {
            if (appearVFX.IsSet) {
                PrefabPool.InstantiateAndReturn(appearVFX, Vector3.zero, Quaternion.identity, parent: transform).Forget();
            }
        }

        protected virtual void SpawnDisappearVFX() {
            if (disappearVFX.IsSet) {
                PrefabPool.InstantiateAndReturn(disappearVFX, Vector3.zero, Quaternion.identity, parent: transform).Forget();
            }
        }

        protected override void BeforeDissolveStarted(IDissolveAble renderer, float startingTransitionValue) {
            BeforeDissolveStartedAsync(renderer, startingTransitionValue).Forget();
        }

        async UniTaskVoid BeforeDissolveStartedAsync(IDissolveAble renderer, float startingTransitionValue) {
            // We can't change materials in every moment in the lifecycle to avoid "MipmapsStreamingMasterMaterials is in writing mode"
            await UniTask.WaitForEndOfFrame();
            if (gameObject == null) {
                return;
            }
            if (renderer.IsInDissolvableState) {
                return;
            }
            renderer.ChangeToDissolveAble();
            renderer.InitPropertyModification(disappearSerializedType, startingTransitionValue);
        }

        protected override void UpdateEffects(float transition) {
            if (!isInEffect) {
                return;
            }
            
            foreach (var dissolveAbleRenderer in _actualRenderers) {
                if (!dissolveAbleRenderer.IsInDissolvableState) {
                    continue;
                }
                dissolveAbleRenderer.UpdateProperty(disappearSerializedType, transition);
            }
        }
        
        protected override void AfterDissolveEnded(float endValue, CancellationToken ct) {
            if (ct.IsCancellationRequested) {
                return;
            }
            
            if (mathExt.Approximately(endValue, Invisible)) {
                return;
            }
            
            foreach (var dissolveAbleRenderer in _actualRenderers) {
                if (!dissolveAbleRenderer.IsInDissolvableState) {
                    continue;
                }
                dissolveAbleRenderer.RestoreToOriginal();
                dissolveAbleRenderer.FinishPropertyModification(disappearSerializedType);
            }
        }
        
        protected override void OnRendererAdded(IDissolveAble dissolveable) {
            dissolveable.AssignController(this);
        }

        protected override bool CanBeDissolved(IDissolveAble dissolvable) {
            if (!dissolveType.HasFlagFast(DissolveType.Weapon) && dissolvable.IsWeapon) {
                return false;
            }

            if (!dissolveType.HasFlagFast(DissolveType.Cloth) && dissolvable.IsCloth) {
                return false;
            }

            return true;
        }
    }
}