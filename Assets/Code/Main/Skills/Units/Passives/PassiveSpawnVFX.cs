using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Heroes;
using Awaken.TG.VisualScripts.Units;
using Cysharp.Threading.Tasks;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(VisualEffect))]
    [UnityEngine.Scripting.Preserve]
    public class PassiveSpawnVFX : PassiveUnit, IGraphElementWithData {
        const int VFXFadeOutDurationMilliseconds = 2500;
        
        RequiredValueInput<ShareableARAssetReference> _vfxEffect;
        OptionalValueInput<ShareableARAssetReference> _playerVFXEffect;
        FallbackValueInput<Transform> _parent;
        FallbackValueInput<Vector3> _position;
        FallbackValueInput<Vector3> _scale;
        FallbackValueInput<Quaternion> _rotation;
        FallbackValueInput<float> _disableDelayOnDeath;

        Data FlowData(Flow flow) => flow.stack.GetElementData<Data>(this);
        
        protected override void Definition() {
            _vfxEffect = RequiredARValueInput<ShareableARAssetReference>(nameof(_vfxEffect));
            _playerVFXEffect = OptionalARValueInput<ShareableARAssetReference>(nameof(_playerVFXEffect));
            _parent = FallbackARValueInput(nameof(_parent), f => this.Skill(f).Owner.ParentTransform);
            _position = FallbackARValueInput(nameof(_position), _ => Vector3.zero);
            _scale = FallbackARValueInput(nameof(_scale), _ => Vector3.one);
            _rotation = FallbackARValueInput(nameof(_rotation), _ => Quaternion.identity);
            _disableDelayOnDeath = FallbackARValueInput(nameof(_disableDelayOnDeath), _ => 0f);
        }

        public override void Enable(Skill skill, Flow flow) {
            AsyncEnable(skill, flow).Forget();
        }

        public async UniTaskVoid AsyncEnable(Skill skill, Flow flow) {
            Data data = FlowData(flow);
            data.disabled = false;

            ShareableARAssetReference vfx;
            if (skill.Owner is Hero && !Hero.TppActive) {
                if (_playerVFXEffect.HasValue) {
                    vfx = _playerVFXEffect.Value(flow);
                } else {
                    return;
                }
            } else {
                vfx = _vfxEffect.Value(flow);
            }
            
            Vector3 pos = _position.Value(flow);
            Vector3 sca = _scale.Value(flow);
            Quaternion rotation = _rotation.Value(flow);
            Transform par = _parent.Value(flow);
            float disableDelayOnDeath = _disableDelayOnDeath.Value(flow);
            IPooledInstance instance = await PrefabPool.Instantiate(vfx, pos, rotation, parent: par, sca);
            data.vfxInstance = instance;
            data.disableDelayOnDeath = (int) (disableDelayOnDeath * 1000);
            if (data.disabled) {
                ReturnVFX(data);
            }
        }

        public override void Disable(Skill skill, Flow flow) {
            AsyncDisable(skill, flow).Forget();
        }

        public async UniTaskVoid AsyncDisable(Skill skill, Flow flow) {
            Data data = FlowData(flow);
            if (skill.Owner is { IsAlive: false } && data.disableDelayOnDeath > 0f) {
                await UniTask.Delay(data.disableDelayOnDeath);
            }
            IPooledInstance vfxInstance = data.vfxInstance;
            if (vfxInstance != null && vfxInstance.Instance != null) {
                //vfxInstance.Instance.GetComponentsInChildren<VisualEffect>().ForEach(VFXUtils.StopVfx);
                await UniTask.Delay(VFXFadeOutDurationMilliseconds);
                ReturnVFX(data);
            } else {
                ReturnVFX(data);
            }
            data.disabled = true;
        }

        static void ReturnVFX(Data data) {
            data.vfxInstance?.Return();
            data.vfxInstance = null;
        }
        
        public IGraphElementData CreateData() {
            return new Data();
        }

        class Data : IGraphElementData {
            public IPooledInstance vfxInstance;
            public bool disabled;
            public int disableDelayOnDeath;
        }
    }
}