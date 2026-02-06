using Awaken.Utility;
using System;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class MistlingCombat : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.MistlingCombat;

        [SerializeField, ARAssetReferenceSettings(new[] { typeof(GameObject) }, true, AddressableGroup.VFX)]
        ShareableARAssetReference fogVfx;
        
        bool _isInstantiatingFog;
        IPooledInstance _fogVfxInstance;
        PositionConstraint _fogVfxConstraint;
        CancellationTokenSource _fogVfxCancellationTokenSource;
        
        public override void InitFromAttachment(CustomCombatAttachment spec, bool isRestored) {
            MistlingCombat copyFrom = (MistlingCombat)spec.CustomCombatBaseClass;
            fogVfx = copyFrom.fogVfx;
            base.InitFromAttachment(spec, isRestored);
        }

        protected override void OnInitializeInternal() {
            base.OnInitializeInternal();
            InstantiateFogVfx().Forget();
        }

        async UniTaskVoid InstantiateFogVfx() {
            if (_isInstantiatingFog) {
                return;
            }
            
            _isInstantiatingFog = true;
            
            _fogVfxCancellationTokenSource?.Cancel();
            _fogVfxCancellationTokenSource = new CancellationTokenSource();
            
            if (fogVfx.IsSet) {
                _fogVfxInstance = await PrefabPool.Instantiate(fogVfx, ParentModel.Coords, ParentModel.Rotation,
                    cancellationToken: _fogVfxCancellationTokenSource.Token);
                if (_fogVfxInstance == null) {
                    return;
                }
                
                _fogVfxConstraint = _fogVfxInstance.Instance.AddComponent<PositionConstraint>();
                _fogVfxConstraint.AddSource(new ConstraintSource {
                    sourceTransform = ParentModel.MainView.transform,
                    weight = 1
                });
                _fogVfxConstraint.constraintActive = true;
                NpcElement.ListenTo(IAlive.Events.BeforeDeath, () => {
                    ReleaseFogVfx(false);
                }, this);
            }

            _fogVfxCancellationTokenSource = null;
            _isInstantiatingFog = false;
        }
        
        void ReleaseFogVfx(bool instant) {
            _fogVfxCancellationTokenSource?.Cancel();
            _fogVfxCancellationTokenSource = null;
            
            if (_fogVfxConstraint != null) {
                Object.Destroy(_fogVfxConstraint);
            }

            if (instant) {
                _fogVfxInstance?.Release();
            } else {
                VFXUtils.StopVfxAndReturn(_fogVfxInstance, 2.5f);
            }

            _fogVfxInstance = null;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ReleaseFogVfx(true);
            base.OnDiscard(fromDomainDrop);
        }
    }
}