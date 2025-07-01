using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Graphics.VFX.ShaderControlling;
using Awaken.TG.Main.Locations;
using Awaken.TG.VisualScripts.Units.VFX;
using Cysharp.Threading.Tasks;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Heroes.Combat {
    public class MagicVFXWrapper {
        IPooledInstance _pooledInstance;
        VisualEffect _visualEffect;
        MaterialGatherer _materialGatherer;
        List<IApplicableToVFX> _applicableToVfxCache = new();
        bool _return;
        
        public MagicVFXWrapper(UniTask<IPooledInstance> vfxInstanceUniTask) {
            WaitForVfxInstanceLoad(vfxInstanceUniTask).Forget();
        }

        public void UpdateMagicVfxParams(MagicVFXParam magicVFXParam) {
            ApplyToVFX(magicVFXParam);
        }

        public void UpdateShaderControllerMediator(MagicVFXShaderMediatorParam param) {
            ApplyToVFX(param);
        }

        public void SendEvent(string eventName, VFXEventAttributeData attributeData) {
            ApplyToVFX(new VFXEvent(eventName, attributeData));
        }
        
        public void SendEvent(string eventName) {
            ApplyToVFX(new VFXEvent(eventName));
        }
        
        [UnityEngine.Scripting.Preserve]
        public void AttachViewComponents(Location location) {
            ApplyToVFX(new VFXAttachVC(location));
        }
        
        void ApplyToVFX(IApplicableToVFX applicableToVfx) {
            if (_visualEffect == null) {
                _applicableToVfxCache.Add(applicableToVfx);
                return;
            }

            ApplyToVFXInternal(applicableToVfx);
        }

        void ApplyToVFXInternal(IApplicableToVFX applicableToVfx) {
            if (_pooledInstance == null) {
                return;
            }
            
            applicableToVfx.ApplyToVFX(_visualEffect, _pooledInstance.Instance);

            if (applicableToVfx is IApplicableToVFXWithMaterial withMaterial && _materialGatherer != null) {
                foreach (var material in _materialGatherer.Materials) {
                    withMaterial.ApplyToShaderMaterial(material, _pooledInstance.Instance);
                }
            }
        }

        [UnityEngine.Scripting.Preserve]
        public void DestroyVFX(float delay = 0) {
            if (_pooledInstance == null) {
                _return = true;
                return;
            }

            if (_materialGatherer != null) {
                _materialGatherer.Release();
                _materialGatherer = null;
            }
            
            if (delay <= 0) {
                _pooledInstance.Return();
            } else {
                VFXUtils.StopVfxAndReturn(_pooledInstance, delay);
            }

            _pooledInstance = null;
        }

        async UniTaskVoid WaitForVfxInstanceLoad(UniTask<IPooledInstance> vfxInstanceUniTask) {
            _pooledInstance = await vfxInstanceUniTask;
            if (_pooledInstance == null) {
                return;
            }

            if (_return) {
                _pooledInstance.Return();
                _pooledInstance = null;
                return;
            } 
            
            _visualEffect = _pooledInstance.Instance.GetComponentInChildren<VisualEffect>();
            
            _materialGatherer = _pooledInstance.Instance.GetComponentInChildren<MaterialGatherer>();
            if (_materialGatherer) {
                _materialGatherer.Gather();
            }

            foreach (var applicable in _applicableToVfxCache) {
                ApplyToVFXInternal(applicable);
            }
            _applicableToVfxCache.Clear();
        }
    }
}