using System.Linq;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Graphics.VFX.Binders;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Fights.NPCs {
    /// <summary>
    /// Turns NPC into ghost
    /// </summary>
    public partial class NpcGhostElement : Element<NpcElement> {
        public override ushort TypeForSerialization => SavedModels.NpcGhostElement;

        const float TransitionTime = 10f;

        static readonly int StandardBaseMap = Shader.PropertyToID("_BaseColorMap");
        static readonly int StandardNormalMap = Shader.PropertyToID("_NormalMap");
        static readonly int StandardMaskMap = Shader.PropertyToID("_MaskMap");

        static readonly int SkinBaseMap = Shader.PropertyToID("_DiffuseMap");
        static readonly int SkinNormalMap = Shader.PropertyToID("_NormalMap");
        static readonly int SkinMaskMap = Shader.PropertyToID("_MaskMap");
        
        static readonly int Transition = Shader.PropertyToID("_Transition");
        
        static ShareableARAssetReference DefaultGhostVfx => Services.Get<CommonReferences>().defaultGhostVfx;

        ARAssetReference _defaultGhostMaterialReference;
        Material _defaultGhostMaterial;
        Location Location => ParentModel.ParentModel;

        bool _instant;
        [Saved(false)] bool _revertable;
        float _timeElapsed;
        float _transitionTime = TransitionTime;
        IPooledInstance _vfx;

        public bool Revertable => _revertable;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public NpcGhostElement() {}
        
        public NpcGhostElement(bool revertable) : this(TransitionTime, revertable) { }
        
        public NpcGhostElement(float transitionTime, bool revertable) {
            _transitionTime = transitionTime;
            _revertable = revertable;
            if (transitionTime <= 0f) {
                _instant = true;
            }
        }

        protected override void OnInitialize() {
            Init();
        }

        protected override void OnRestore() {
            _instant = true;
            Init();
        }

        void Init() {
            _defaultGhostMaterialReference ??= Services.Get<CommonReferences>().defaultGhostMaterial?.Get();
            if (_defaultGhostMaterialReference == null) {
                Log.Critical?.Error("Default ghost material reference is not set and cannot be loaded!");
                return;
            }

            if (_defaultGhostMaterial == null) {
                _defaultGhostMaterialReference.LoadAsset<Material>().OnComplete(handle => {
                    if (handle.Status != AsyncOperationStatus.Succeeded) {
                        Log.Critical?.Error("Default ghost material could not be loaded!");
                        return;
                    }
                    _defaultGhostMaterial = handle.Result;
                    ParentModel.OnCompletelyInitialized(OnParentInitialized);
                });
                return;
            }

            ParentModel.OnCompletelyInitialized(OnParentInitialized);
        }

        void OnParentInitialized(NpcElement _) {
            StartKandraChanges();
            ConvertRenderersToGhost(ParentModel.ParentTransform.gameObject);
            if (!_instant) {
                Location.GetOrCreateTimeDependent()?.WithUpdate(ProcessUpdate);
            }

            var clothes = Location.Element<NpcClothes>();
            clothes.ListenTo(BaseClothes.Events.ClothEquipped, OnClothLoaded, this);
            clothes.ListenTo(BaseClothes.Events.ClothBeingUnequipped, OnClothUnequipped, this);
            ApplyVfx().Forget();
        }

        void OnClothLoaded(GameObject cloth) {
            ConvertRenderersToGhost(cloth);
        }

        void OnClothUnequipped(GameObject cloth) {
            RemoveGhostRenderers(cloth);
        }

        public async UniTaskVoid RevertChangesAndDiscard() {
            RevertChanges(_transitionTime);
            if (!await AsyncUtil.DelayTime(this, _transitionTime)) {
                return;
            }
            FinishRevertChanges();
        }

        public void RevertChanges(float transitionTime) {
            if (!_revertable) {
                Log.Important?.Error($"Trying to revert change of Ghost {ParentModel} which is not revertable");
            }
            StartKandraRevertChanges();

            _transitionTime = transitionTime;
            _timeElapsed = 0f;
            Location.GetOrCreateTimeDependent().WithoutUpdate(ProcessUpdate).WithUpdate(ProcessUpdateRevert);
            RemoveVfx();
        }
        
        public void FinishRevertChanges() {
            if (!_revertable) {
                Log.Important?.Error($"Trying to revert change of Ghost {ParentModel} which is not revertable");
            }
            FinishKandraRevertChanges();
            FinishDrakeRevertChanges();
            foreach (var dar in Location.ViewParent.GetComponentsInChildren<DissolveAbleRenderer>(true)) {
                dar.RestoreToOriginal();
            }
            Location.GetTimeDependent()?.WithoutUpdate(ProcessUpdateRevert);
            this.Discard();
        }

        void ConvertRenderersToGhost(GameObject go) {
            ConvertRenderersToGhost(go.GetComponentsInChildren<KandraRenderer>(true).Where(r => !r.gameObject.HasComponent<VFXBodyMarker>()));
            ConvertRenderersToGhost(go.GetComponentsInChildren<LinkedEntitiesAccess>(true));
        }
        
        void RemoveGhostRenderers(GameObject go) {
            RemoveGhostRenderers(go.GetComponentsInChildren<KandraRenderer>(true).Where(r => !r.gameObject.HasComponent<VFXBodyMarker>()));
            RemoveGhostRenderers(go.GetComponentsInChildren<LinkedEntitiesAccess>(true));
        }

        void ProcessUpdate(float deltaTime) {
            _timeElapsed += deltaTime;
            float percent = _timeElapsed / _transitionTime;
            UpdateMaterials(percent);

            if (_timeElapsed >= _transitionTime) {
                Location.GetTimeDependent()?.WithoutUpdate(ProcessUpdate);
                _instant = true;
            }
        }
        
        void ProcessUpdateRevert(float deltaTime) {
            _timeElapsed += deltaTime;
            float percent = 1 - _timeElapsed / _transitionTime;
            UpdateMaterials(percent);

            if (_timeElapsed >= _transitionTime) {
                FinishRevertChanges();
            }
        }
        
        void UpdateMaterials(float percent) {
            UpdateKandraMaterials(percent);
            UpdateDrakeMaterials(percent);
        }
        
        Material CreateNewMaterial(Material originalMaterial) {
            Material ghostMaterial = Object.Instantiate(_defaultGhostMaterial);
#if UNITY_EDITOR
            ghostMaterial.name += "_Ghost_" + originalMaterial.GetHashCode();
#endif
            CopyTextures(originalMaterial, ghostMaterial);
            ghostMaterial.SetFloat(Transition, _instant ? 1 : 0);

            return ghostMaterial;
        }

        void CopyTextures(Material originalMaterial, Material targetMaterial) {
            if (targetMaterial.HasTexture(StandardBaseMap)) {
                GetOriginalTextures(originalMaterial, out var baseMap, out var normalMap, out var maskMap);
                targetMaterial.SetTexture(StandardBaseMap, baseMap);
                targetMaterial.SetTexture(StandardNormalMap, normalMap);
                targetMaterial.SetTexture(StandardMaskMap, maskMap);
            }
        }
        
        void GetOriginalTextures(Material originalMaterial, out Texture baseMap, out Texture normalMap, out Texture maskMap) {
            if (originalMaterial.HasTexture(StandardBaseMap)) {
                baseMap = originalMaterial.GetTexture(StandardBaseMap);
                normalMap = originalMaterial.GetTexture(StandardNormalMap);
                maskMap = originalMaterial.GetTexture(StandardMaskMap);
            } else if (originalMaterial.HasTexture(SkinBaseMap)) {
                baseMap = originalMaterial.GetTexture(SkinBaseMap);
                normalMap = originalMaterial.GetTexture(SkinNormalMap);
                maskMap = originalMaterial.GetTexture(SkinMaskMap);
            } else {
                baseMap = null;
                normalMap = null;
                maskMap = null;
            }
        }

        async UniTaskVoid ApplyVfx() {
            var parentTransform = ParentModel.Controller.RootBone;
            _vfx = await PrefabPool.Instantiate(DefaultGhostVfx, Vector3.zero, Quaternion.identity, parentTransform, Vector3.one);
        }
        
        void RemoveVfx(bool instant = false) {
            if (_vfx == null) {
                return;
            }
            
            if (instant) {
                _vfx.Return();
            } else {
                VFXUtils.StopVfxAndReturn(_vfx, 5f);
            }
            _vfx = null;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            RemoveVfx(fromDomainDrop);
            _defaultGhostMaterialReference?.ReleaseAsset();
        }
    }
}