using Awaken.Utility;
using System;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Character;
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
        IPooledInstance _fogVfxInstance;

        public override void InitFromAttachment(CustomCombatAttachment spec, bool isRestored) {
            MistlingCombat copyFrom = (MistlingCombat)spec.CustomCombatBaseClass;
            fogVfx = copyFrom.fogVfx;
            base.InitFromAttachment(spec, isRestored);
        }

        protected override void OnInitialize() {
            InstantiateFogVfx().Forget();
            base.OnInitialize();
        }

        async UniTaskVoid InstantiateFogVfx() {
            if (fogVfx.IsSet) {
                _fogVfxInstance = await PrefabPool.Instantiate(fogVfx, ParentModel.Coords, ParentModel.Rotation);
                PositionConstraint constraint = _fogVfxInstance.Instance.AddComponent<PositionConstraint>();
                constraint.AddSource(new ConstraintSource {
                    sourceTransform = ParentModel.MainView.transform,
                    weight = 1
                });
                constraint.constraintActive = true;
                NpcElement.ListenTo(IAlive.Events.BeforeDeath, () => {
                    Object.Destroy(constraint);
                    VFXUtils.StopVfxAndReturn(_fogVfxInstance, 2.5f);
                }, this);
            }
        }
    }
}