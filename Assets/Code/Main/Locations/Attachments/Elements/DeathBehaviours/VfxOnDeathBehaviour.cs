using System.Threading;
using Awaken.Kandra;
using Awaken.Kandra.VFXs;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public class VfxOnDeathBehaviour : MonoBehaviour, IDeathBehaviour {
        [SerializeField, ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        ShareableARAssetReference deathVFX;
        [SerializeField] float delay = 1f;
        [SerializeField] bool bindVfxSkinnedMeshRenderer;
        [SerializeField] bool useDeathAnim = true;

        CancellationTokenSource _vfxCancellationTokenSource;
        CancellationTokenSource _delayCancellationTokenSource;
        IPooledInstance _vfxInstance;

        public bool UseDeathAnimation => useDeathAnim;
        public NpcDeath.DeathAnimType UseCustomDeathAnimation => NpcDeath.DeathAnimType.Default;
        public void OnVisualLoaded(DeathElement death, Transform transform) {}

        public void OnDeath(DamageOutcome damageOutcome, Location location) {
            _vfxCancellationTokenSource?.Cancel();
            _vfxCancellationTokenSource = null;
            _delayCancellationTokenSource?.Cancel();
            _delayCancellationTokenSource = null;
            
            PrepareVfx().Forget();
            if (delay > 0 || _vfxInstance == null) {
                DelayPlayVfx().Forget();
            } else {
                PlayVfx();
            }
        }

        async UniTaskVoid PrepareVfx() {
            _vfxCancellationTokenSource = new CancellationTokenSource();
            _vfxInstance = await PrefabPool.Instantiate(deathVFX, Vector3.zero, Quaternion.identity, parent: transform.parent, cancellationToken: _vfxCancellationTokenSource.Token, automaticallyActivate: false);
        }

        async UniTaskVoid DelayPlayVfx() {
            if (delay > 0) {
                _delayCancellationTokenSource = new CancellationTokenSource();
                await AsyncUtil.DelayTime(this, delay, source: _delayCancellationTokenSource);
            }
            
            while (_vfxInstance is not { InstanceLoaded: true } && this != null && _vfxCancellationTokenSource is { IsCancellationRequested: false }) {
                await AsyncUtil.DelayFrame(this);
            }
            
            PlayVfx();
        }

        void PlayVfx() {
            if (this == null) {
                _vfxInstance?.Return();
                _vfxInstance = null;
                return;
            }

            if (_vfxInstance != null) {
                VisualEffect vfx = _vfxInstance.Instance.GetComponentInChildren<VisualEffect>();
                
                if (bindVfxSkinnedMeshRenderer) {
                    var binder = _vfxInstance.Instance.GetComponent<VFXKandraRendererBinder>();
                    if (!binder) {
                        Log.Minor?.Error($"VFX instance {deathVFX}[{_vfxInstance.Instance}] does not have {nameof(VFXKandraRendererBinder)} but want to be bound");
                    } else {
                        binder.kandraRenderer = GetComponentInChildren<KandraRenderer>();
                    }
                }
                
                _vfxInstance.Instance.SetActive(true);
                vfx.Play();
                _vfxInstance.Return(3f).Forget();
                _vfxInstance = null;
            }
        }

        void OnDestroy() {
            _vfxCancellationTokenSource?.Cancel();
            _vfxCancellationTokenSource = null;
            _delayCancellationTokenSource?.Cancel();
            _delayCancellationTokenSource = null;
        }
    }
}