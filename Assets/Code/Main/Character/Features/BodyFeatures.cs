using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character.Features.Config;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Serialization;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public sealed partial class BodyFeatures : Element<IWithBodyFeature> {
        public override ushort TypeForSerialization => SavedModels.BodyFeatures;

        [Saved] public bool BlockRandomization { get; set; }

        [Saved] Gender _gender;
        [Saved] GeneralBodyState _bodyState;
        [Saved] SkinColorFeature _skinColor;
        [Saved] BodySkinTexturesFeature _bodySkin;
        [Saved] FaceSkinTexturesFeature _faceSkin;

        [Saved] EyebrowFeature _eyebrows;
        [Saved] EyeColorFeature _eyes;
        [Saved] TeethFeature _teeth;
        
        [Saved] BlendShapesFeature _shapesFeature;
        [Saved] MeshFeature _hair;
        [Saved] MeshFeature _beard;
        [Saved] BodyNormalFeature _normals;
        [Saved] BodyTattooFeature _bodyTattoo;
        [Saved] FaceTattooFeature _faceTattoo;

        [Saved] HashSet<BodyFeature> _additionalFeatures;

        [Saved] HairConfig _hairConfig;
        [Saved] HairConfig _beardConfig;
        FaceFeature _face;

        List<ICoverMesh> _coverableMeshes = new();
        List<HairController> _hairs = new();

        bool _blockHairsAdd;
        bool _areHairsTransparent;
        bool _wasMovedOrDiscarded;
        bool _customBodyMaterialsApplied;

        List<MeshCoverSettings> _covers = new();
        CoverType _aggregatedCover;
        
        readonly List<KandraMarkerOriginalMaterial> _originalKandraMaterials = new();
        ARAssetReference _customBodyMaterialReference;
        ARAssetReference _customFaceMaterialReference;

        FeaturesState _desiredState;
        bool _stateChangeInProgress;
        bool _registeredToProvider;

        public bool MutableSetterAsyncLock { get; set; }
        public bool IsShown { get; private set; }
        public IEnumerable<BodyFeature> AllFeatures {
            get {
                if (_skinColor != null) yield return _skinColor;
                if (_bodySkin != null) yield return _bodySkin;
                if (_faceSkin != null) yield return _faceSkin;
                if (_eyebrows != null) yield return _eyebrows;
                if (_eyes != null) yield return _eyes;
                if (_teeth != null) yield return _teeth;
                if (_face != null) yield return _face;
                if (_shapesFeature != null) yield return _shapesFeature;
                if (_hair != null) yield return _hair;
                if (_beard != null) yield return _beard;
                if (_normals != null) yield return _normals;
                if (_bodyTattoo != null) yield return _bodyTattoo;
                if (_faceTattoo != null) yield return _faceTattoo;
                foreach (var feature in AdditionalFeatures) {
                    yield return feature;
                }
            }
        }

        public IEnumerable<BodyFeature> AdditionalFeatures => _additionalFeatures ?? Enumerable.Empty<BodyFeature>();

        public IBaseClothes<IItemOwner> BaseClothes => ParentModel.Clothes;
        public GameObject GameObject => ParentModel.BodyView.gameObject;
        public GameObject SafeGameObject => ParentModel is { HasBeenDiscarded: false, BodyView: { gameObject: not null } bodyView } 
                                                    ? bodyView.gameObject 
                                                    : null;
        
        // === Standard features
        public Gender Gender {
            get => _gender;
            set => _gender = value;
        }

        public SkinColorFeature SkinColor {
            get => _skinColor;
            set => ChangeMutableFeature(ref _skinColor, value);
        }
        
        public BodySkinTexturesFeature BodySkin {
            [UnityEngine.Scripting.Preserve] get => _bodySkin;
            set => ChangeMutableFeature(ref _bodySkin, value);
        }

        public FaceSkinTexturesFeature FaceSkin {
            [UnityEngine.Scripting.Preserve] get => _faceSkin;
            set => ChangeMutableFeature(ref _faceSkin, value);
        }
        
        public EyebrowFeature Eyebrows {
            [UnityEngine.Scripting.Preserve] get => _eyebrows;
            set => ChangeMutableFeature(ref _eyebrows, value);
        }
        public EyeColorFeature Eyes {
            [UnityEngine.Scripting.Preserve] get => _eyes;
            set => ChangeMutableFeature(ref _eyes, value);
        }
        
        public TeethFeature Teeth {
            [UnityEngine.Scripting.Preserve] get => _teeth;
            set => ChangeMutableFeature(ref _teeth, value);
        }

        public BlendShapesFeature ShapesFeature {
            [UnityEngine.Scripting.Preserve] get => _shapesFeature;
            set => ChangeMutableFeature(ref _shapesFeature, value);
        }

        public MeshFeature Hair {
            [UnityEngine.Scripting.Preserve] get => _hair;
            set => ChangeMutableFeature(ref _hair, value);
        }

        public MeshFeature Beard {
            [UnityEngine.Scripting.Preserve] get => _beard;
            set => ChangeMutableFeature(ref _beard, value);
        }

        public BodyNormalFeature Normals {
            [UnityEngine.Scripting.Preserve] get => _normals;
            set => ChangeMutableFeature(ref _normals, value);
        }
        
        public BodyTattooFeature BodyTattoo {
            [UnityEngine.Scripting.Preserve] get => _bodyTattoo;
            set => ChangeMutableFeature(ref _bodyTattoo, value);
        }

        public FaceTattooFeature FaceTattoo {
            [UnityEngine.Scripting.Preserve] get => _faceTattoo;
            set => ChangeMutableFeature(ref _faceTattoo, value);
        }
        
        // === Lifetime
        protected override void OnInitialize() {
            _face ??= new FaceFeature();
            foreach (var feature in AllFeatures) {
                feature.Init(this);
            }

            ChangeStateChangeProgress(_stateChangeInProgress);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Hide();
            ClearReferences();
        }
        
        public void UnityUpdate() {
            if (IsShown && _desiredState == FeaturesState.Hidden) {
                HideTaskInternal().Forget();
            } else if (!IsShown && _desiredState == FeaturesState.Showed) {
                ShowTaskInternal().Forget();
            }
        }

        // === Persistence

        public override void Serialize(SaveWriter writer) {
            base.Serialize(writer);
            
            if (BlockRandomization) {
                writer.WriteName(SavedFields.BlockRandomization);
                writer.Write(BlockRandomization);
                writer.WriteSeparator();
            }
            if (_gender != Gender.None) {
                writer.WriteName(SavedFields._gender);
                writer.Write(_gender);
                writer.WriteSeparator();
            }
            if (_bodyState != GeneralBodyState.None) {
                writer.WriteName(SavedFields._bodyState);
                writer.Write(_bodyState);
                writer.WriteSeparator();
            }

            WriteBodyFeature(writer, SavedFields._skinColor, _skinColor, (w, f) => w.Write(f));
            WriteBodyFeature(writer, SavedFields._bodySkin, _bodySkin, (w, f) => w.Write(f));
            WriteBodyFeature(writer, SavedFields._faceSkin, _faceSkin, (w, f) => w.Write(f));
            WriteBodyFeature(writer, SavedFields._eyebrows, _eyebrows, (w, f) => w.Write(f));
            WriteBodyFeature(writer, SavedFields._eyes, _eyes, (w, f) => w.Write(f));
            WriteBodyFeature(writer, SavedFields._teeth, _teeth, (w, f) => w.Write(f));
            WriteBodyFeature(writer, SavedFields._shapesFeature, _shapesFeature, (w, f) => w.Write(f), shapes => shapes.IsEmpty);
            WriteBodyFeature(writer, SavedFields._hair, _hair, (w, f) => w.Write(f));
            WriteBodyFeature(writer, SavedFields._beard, _beard, (w, f) => w.Write(f));
            WriteBodyFeature(writer, SavedFields._normals, _normals, (w, f) => w.Write(f));
            WriteBodyFeature(writer, SavedFields._bodyTattoo, _bodyTattoo, (w, f) => w.Write(f));
            WriteBodyFeature(writer, SavedFields._faceTattoo, _faceTattoo, (w, f) => w.Write(f));

            if (_additionalFeatures != null) {
                writer.WriteName(SavedFields._additionalFeatures);
                writer.WriteHashSet(_additionalFeatures, (w, f) => w.Write(f));
                writer.WriteSeparator();
            }

            if (_hairConfig != null) {
                writer.WriteName(SavedFields._hairConfig);
                writer.WriteTemplate(_hairConfig);
                writer.WriteSeparator();
            }
            if (_beardConfig != null) {
                writer.WriteName(SavedFields._beardConfig);
                writer.WriteTemplate(_beardConfig);
                writer.WriteSeparator();
            }

            static void WriteBodyFeature<T>(SaveWriter writer, ushort name, T feature, Action<SaveWriter, T> write, Func<T, bool> skip = null) {
                if (feature == null) {
                    return;
                }
                if (skip != null && skip(feature)) {
                    return;
                }
                writer.WriteName(name);
                write(writer, feature);
                writer.WriteSeparator();
            }
        }

        // === API
        public void HeroPerspectiveChanged() {
            if (ParentModel is Hero) {
                _bodyState = GeneralBodyState.None;
            }
        }

        public void AddAdditionalFeature(BodyFeature feature) {
            AddAdditionalFeatureTask(feature).Forget();
        }
        
        public async UniTask AddAdditionalFeatureTask(BodyFeature feature) {
            if (CheckNotValid()) {
                return;
            }
            _additionalFeatures ??= new HashSet<BodyFeature>();
            if (_additionalFeatures.Add(feature)) {
                feature.Init(this);
                if (IsShown) {
                    await feature.Spawn();
                }
            }
        }

        public void RemoveAdditionalFeature(BodyFeature feature) {
            if (CheckNotValid()) {
                return;
            }
            if (_additionalFeatures != null && _additionalFeatures.Remove(feature) && IsShown) {
                feature.Release();
            }
        }

        public void Show() {
            _desiredState = FeaturesState.Showed;
            if (!_stateChangeInProgress) {
                ShowTaskInternal().Forget();
            }
        }
        
        public async UniTask ShowTask() {
            Show();
            await AsyncUtil.CheckAndWaitUntil(() => {
                bool visible = IsShown && !_stateChangeInProgress;
                return visible || _desiredState == FeaturesState.Hidden;
            });
        }

        async UniTask ShowTaskInternal() {
            if (IsShown || CheckNotValid()) {
                return;
            }

            ChangeStateChangeProgress(true);
            IsShown = true;

            try {
                if (_bodyState != GeneralBodyState.None && !_customBodyMaterialsApplied) {
                    var customMaterials = await LoadCustomBodyStateMaterials(_bodyState);
                    ApplyCustomBodyMaterials(customMaterials);
                } else if (_bodyState == GeneralBodyState.None && _customBodyMaterialsApplied) {
                    RestoreOriginalMaterials();
                }

                await UniTask.WhenAll(AllFeatures.Select(feature => feature.Spawn()));
            } finally {
                ChangeStateChangeProgress(false);
            }
        }

        public void Hide() {
            _desiredState = FeaturesState.Hidden;
            if (!_stateChangeInProgress) {
                HideTaskInternal().Forget();
            }
        }

        async UniTaskVoid HideTaskInternal() {
            if (!IsShown || CheckNotValid()) {
                return;
            }

            ChangeStateChangeProgress(true);
            IsShown = false;
            try {
                await UniTask.WhenAll(AllFeatures.Select(feature => feature.Release()));
                await UniTask.DelayFrame(1);
            } finally {
                ChangeStateChangeProgress(false);
            }
        }

        public void InitCovers(IBaseClothes clothes) {
            clothes.ListenTo(Locations.Mobs.BaseClothes.Events.ClothEquipped, AddCover, this);
            clothes.ListenTo(Locations.Mobs.BaseClothes.Events.ClothBeingUnequipped, RemoveCover, this);
            clothes.LoadedClothes.WhereNotNull().ForEach(AddCover);
        }

        [UnityEngine.Scripting.Preserve]
        public void AddCover(MeshCoverSettings cover) {
            if (CheckNotValid()) {
                return;
            }
            if (_covers.AddUnique(cover)) {
                RefreshCover();
            }
        }
        
        public void AddCoverableMesh(ICoverMesh feature) {
            if (CheckNotValid()) {
                return;
            }
            _coverableMeshes.Add(feature);
            feature.RefreshCover(_aggregatedCover);
        }

        public void RemoveCoverableMesh(ICoverMesh feature) {
            if (CheckNotValid()) {
                return;
            }
            feature.RefreshCover(CoverType.None);
            _coverableMeshes?.Remove(feature);
        }

        public void RefreshCover() {
            if (CheckNotValid()) {
                return;
            }
            var aggregatedCover = _covers.Aggregate(CoverType.None, (current, next) => current | next.Type);
            if (_aggregatedCover != aggregatedCover) {
                _aggregatedCover = aggregatedCover;
                foreach (var coverable in _coverableMeshes) {
                    coverable.RefreshCover(_aggregatedCover);
                }
            }
        }

        public void SetHairsAddBlock(bool blockHairs) {
            _blockHairsAdd = blockHairs;
        }

        public void AddHairFeature(HairController hair) {
            if (CheckNotValid()) {
                return;
            }
            if (_blockHairsAdd) {
                hair.EnsureDeinitialized();
                return;
            }
            _hairs.Add(hair);
            hair.EnsureInitialized();
            // TODO: manage hair blendshapes LODs (second parameter)
            hair.Refresh(_areHairsTransparent, _areHairsTransparent);
            var config = hair.IsBeard ? _beardConfig : _hairConfig;
            if (config) {
                hair.SetHairColor(config.data);
            }
        }

        public void ApplyHairConfig(Material material, bool isBeard) {
            if (CheckNotValid() && material != null) {
                return;
            }
            if (_hairConfig) {
                _hairConfig.data.ApplyTo(material, isBeard);
            }
        }

        public void RemoveAllHairFeatures() {
            if (CheckNotValid()) {
                return;
            }
            foreach (var hair in _hairs) {
                if (hair != null) {
                    hair.EnsureDeinitialized();
                }
            }
            _hairs.Clear();
        }

        public void RemoveHairFeature(HairController hair) {
            if (CheckNotValid()) {
                return;
            }
            if (hair != null) {
                hair.EnsureDeinitialized();
                _hairs.Remove(hair);
            } else {
                _hairs.RemoveAll(static hair => hair == null);
            }
        }

        public void ChangeHairColor(HairConfig config) {
            if (CheckNotValid()) {
                return;
            }
            _hairConfig = config;
            foreach (HairController hair in _hairs.Where(hair => !hair.IsBeard)) {
                hair.SetHairColor(_hairConfig.data);
            }
        }
        
        public void ChangeBeardColor(HairConfig config) {
            if (CheckNotValid()) {
                return;
            }
            _beardConfig = config;
            foreach (HairController hair in _hairs.Where(hair => hair.IsBeard)) {
                hair.SetHairColor(_beardConfig.data);
            }
        }

        public void MoveFrom(BodyFeatures features) {
            if (features.CheckNotValid()) {
                return;
            }
            BlockRandomization = features.BlockRandomization;
            IsShown = features.IsShown;
            _gender = features._gender;
            _bodyState = features._bodyState;
            _skinColor = features._skinColor;
            _bodySkin = features._bodySkin;
            _faceSkin = features._faceSkin;
            _eyebrows = features._eyebrows;
            _eyes = features._eyes;
            _teeth = features._teeth;
            _face = features._face;
            _shapesFeature = features._shapesFeature;
            _beard = features._beard;
            _hair = features._hair;
            _hairConfig = features._hairConfig;
            _beardConfig = features._beardConfig;
            _additionalFeatures = features._additionalFeatures;
            _coverableMeshes = features._coverableMeshes;
            _hairs = features._hairs;
            _areHairsTransparent = features._areHairsTransparent;
            _normals = features._normals;
            _bodyTattoo = features._bodyTattoo;
            _faceTattoo = features._faceTattoo;
            _covers = features._covers;
            _aggregatedCover = features._aggregatedCover;

            features.ClearReferences();
            
            foreach (var feature in AllFeatures) {
                feature.Init(this);
            }
        }

        public void CopyFrom(BodyFeatures features) {
            if (features.CheckNotValid()) {
                return;
            }
            
            _gender = features._gender;
            _bodyState = features._bodyState;
            _hairConfig = features._hairConfig;
            _beardConfig = features._beardConfig;
            SkinColor = features._skinColor?.Copy();
            BodySkin = features._bodySkin?.Copy();
            FaceSkin = features._faceSkin?.Copy();
            Eyes = features._eyes?.Copy();
            Eyebrows = features._eyebrows?.Copy();
            Teeth = features._teeth?.Copy();
            ShapesFeature = features._shapesFeature?.Copy();
            Beard = features._beard?.Copy();
            Hair = features._hair?.Copy();
            Normals = features._normals?.Copy();
            BodyTattoo = features._bodyTattoo?.Copy();
            FaceTattoo = features._faceTattoo?.Copy();

            if (features._additionalFeatures != null) {
                _additionalFeatures = new HashSet<BodyFeature>(features._additionalFeatures.Count);
                _additionalFeatures.AddRange(features._additionalFeatures.Select(feature => feature.GenericCopy()));
                foreach (var feature in _additionalFeatures) {
                    feature.Init(this);
                }
            } else {
                _additionalFeatures = null;
            }
        }

        public void RefreshDistanceBand(int band) {
            if (CheckNotValid()) {
                return;
            }
            bool inBand = LocationCullingGroup.InHairTransparentSurfaceBand(band);
            RefreshHairs(inBand);
        }

        public void Reload() {
            if (!IsShown || CheckNotValid()) {
                return;
            }
            Hide();
            Show();
        }
        
        public async UniTaskVoid ChangeBodyState(GeneralBodyState newBodyState) {
            if (CheckNotValid()) {
                return;
            }

            if (_bodyState == newBodyState) {
                return;
            }
            _bodyState = newBodyState;
            
            BodyStateMaterials newBodyStateMaterials = BodyStateMaterials.Default();
            if (newBodyState != GeneralBodyState.None) {
                newBodyStateMaterials = await LoadCustomBodyStateMaterials(newBodyState);
            }

            bool wasShown = IsShown;
            // Hide before changing, so materials should be restored to originals and not instanced.
            if (wasShown) {
                Hide();
            }

            // Replace original materials
            if (newBodyState != GeneralBodyState.None) {
                ApplyCustomBodyMaterials(newBodyStateMaterials);
            } else {
                RestoreOriginalMaterials();
            }
            
            // Reapply changes
            if (wasShown) {
                Show();
            }
        }
        
        void ApplyCustomBodyMaterials(BodyStateMaterials newBodyStateMaterials) {
            var skinMarker = GameObject.GetComponentInChildren<RenderersMarkers>(true);
            if (skinMarker == null) {
                Log.Minor?.Error($"SkinRendererMarker not found in {GameObject.name}", GameObject);
            }

            for (int i = 0; i < skinMarker.KandraMarkers.Length; i++) {
                var marker = skinMarker.KandraMarkers[i];
                var renderer = marker.Renderer;
                
                if (marker.MaterialType.HasCommonBitsFast(RendererMarkerMaterialType.Body)) {
                    renderer.EnsureInitialized();
                    _originalKandraMaterials.Add(new KandraMarkerOriginalMaterial(marker, renderer.rendererData.materials[marker.Index]));
                    renderer.UseOriginalMaterial(marker.Index, newBodyStateMaterials.bodyMaterial);
                } else if (marker.MaterialType.HasCommonBitsFast(RendererMarkerMaterialType.Face)) {
                    renderer.EnsureInitialized();
                    _originalKandraMaterials.Add(new KandraMarkerOriginalMaterial(marker, renderer.rendererData.materials[marker.Index]));
                    renderer.UseOriginalMaterial(marker.Index, newBodyStateMaterials.faceMaterial);
                }
            }

            _customBodyMaterialsApplied = true;
        }

        void RestoreOriginalMaterials() {
            if (_originalKandraMaterials.Count <= 0) {
                return;
            }

            foreach (KandraMarkerOriginalMaterial markerOriginalMaterial in _originalKandraMaterials) {
                var marker = markerOriginalMaterial.marker;
                marker.Renderer.UseOriginalMaterial(marker.Index, markerOriginalMaterial.material);
            }
            _originalKandraMaterials.Clear();

            _customBodyMaterialsApplied = false;
        }

        public async UniTask<T> ChangeMutableFeatureAsync<T>(T mutableFeature, T feature) where T : BodyFeature {
            if (Equals(mutableFeature, feature)) return mutableFeature;

            T previousToRelease = null;
            if (IsShown) {
                previousToRelease = mutableFeature;
            }

            if (feature != null) {
                mutableFeature = feature;
                feature.Init(this);
                if (IsShown) {
                    await feature.Spawn();
                }
            } else {
                mutableFeature = null;
            }
            previousToRelease?.Release(true);

            return mutableFeature;
        }

        void ChangeMutableFeature<T>(ref T mutableFeature, T feature) where T : BodyFeature {
            if (Equals(mutableFeature, feature)) return;

            if (MutableSetterAsyncLock) {
                mutableFeature = feature;
                return;
            }
            
            if (IsShown) {
                mutableFeature?.Release();
            }

            mutableFeature = feature;
            if (feature != null) {
                feature.Init(this);
                if (IsShown) {
                    feature.Spawn();
                }
            }
        }

        void AddCover(GameObject go) {
            if (CheckNotValid()) {
                return;
            }
            bool added = false;
            foreach (var cover in go.GetComponentsInChildren<MeshCoverSettings>().Where(s => s is {IsCover: true})) {
                if (_covers.AddUnique(cover)) {
                    added = true;
                }
            }
            if (added) {
                RefreshCover();
            }
        }

        void RemoveCover(GameObject go) {
            if (CheckNotValid()) {
                return;
            }
            bool removed = false;
            foreach (var cover in go.GetComponentsInChildren<MeshCoverSettings>().Where(s => s is {IsCover: true})) {
                if (_covers.Remove(cover)) {
                    removed = true;
                }
            }
            if (removed) {
                RefreshCover();
            }
        }

        void RefreshHairs(bool transparent) {
            if (_hairs == null) {
                Log.Debug?.Error($"Hair refresh for moved or/and discarded body features: {ID}; HasBeenDiscarded:{HasBeenDiscarded}");
                return;
            }
            if (_areHairsTransparent == transparent) {
                return;
            }
            _areHairsTransparent = transparent;
            foreach (var hair in _hairs) {
                // TODO: manage hair blendshapes LODs (second parameter)
                hair.Refresh(_areHairsTransparent, _areHairsTransparent);
            }
        }

        void ClearReferences() {
            UnityUpdateProvider.TryGet()?.UnRegisterBodyFeatures(this);

            BlockRandomization = true;
            IsShown = false;
            _skinColor = null;
            _bodySkin = null;
            _faceSkin = null;
            _eyebrows = null;
            _eyes = null;
            _teeth = null;
            _face = null;
            _shapesFeature = null;
            _hair = null;
            _beard = null;
            _hairConfig = default;
            _beardConfig = default;
            _additionalFeatures = null;
            _coverableMeshes = null;
            _hairs = null;
            _normals = null;
            _bodyTattoo = null;
            _faceTattoo = null;
            _covers = null;
            _aggregatedCover = CoverType.None;
            
            _customBodyMaterialReference?.ReleaseAsset();
            _customBodyMaterialReference = null;
            _customFaceMaterialReference?.ReleaseAsset();
            _customFaceMaterialReference = null;

            _originalKandraMaterials.Clear();
            
            _wasMovedOrDiscarded = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool CheckNotValid() {
            if (_wasMovedOrDiscarded) {
                var state = HasBeenDiscarded ? "discarded" : "moved";
                Log.Important?.Error($"Trying to use {state} BodyFeatures {ID}");
                return true;
            }
            return false;
        }
        
        // === Helpers
        void ChangeStateChangeProgress(bool stateChangeInProgress) {
            _stateChangeInProgress = stateChangeInProgress;
            if (_stateChangeInProgress && _registeredToProvider) {
                UnityUpdateProvider.GetOrCreate().UnRegisterBodyFeatures(this);
                _registeredToProvider = false;
            } else if (!_stateChangeInProgress && !_registeredToProvider) {
                UnityUpdateProvider.GetOrCreate().RegisterBodyFeatures(this);
                _registeredToProvider = true;
            }
        }
        
        public bool TryGetGameObject(out GameObject gameObject) {
            gameObject = ParentModel.BodyView?.gameObject;
            return gameObject != null;
        }

        async UniTask<BodyStateMaterials> LoadCustomBodyStateMaterials(GeneralBodyState newBodyState) {
            var oldBodyMaterialReference = _customBodyMaterialReference;
            var oldFaceMaterialReference = _customFaceMaterialReference;
            
            CommonReferences cr = CommonReferences.Get;
            switch (newBodyState) {
                case GeneralBodyState.RedDeath:
                    _customBodyMaterialReference = cr.GetRedDeathBodyMaterial(Gender).Get();
                    _customFaceMaterialReference = cr.GetRedDeathFaceMaterial(Gender).Get();
                    break;
                case GeneralBodyState.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(newBodyState), newBodyState, null);
            }
            
            var bodyMaterialTask = _customBodyMaterialReference.LoadAsset<Material>().ToUniTask();
            var faceMaterialTask = _customFaceMaterialReference.LoadAsset<Material>().ToUniTask();
            
            (Material bodyMaterial, Material faceMaterial) = await UniTask.WhenAll(bodyMaterialTask, faceMaterialTask);
            
            oldBodyMaterialReference?.ReleaseAsset();
            oldFaceMaterialReference?.ReleaseAsset();
            
            return new BodyStateMaterials(bodyMaterial, faceMaterial);
        }
        
        readonly struct BodyStateMaterials {
            public readonly Material bodyMaterial;
            public readonly Material faceMaterial;
            
            public BodyStateMaterials(Material bodyMaterial, Material faceMaterial) {
                this.bodyMaterial = bodyMaterial;
                this.faceMaterial = faceMaterial;
            }

            public static BodyStateMaterials Default() {
                return new BodyStateMaterials(null, null);
            }
        }

        readonly struct KandraMarkerOriginalMaterial {
            public readonly RenderersMarkers.KandraMarker marker;
            public readonly Material material;
            
            public KandraMarkerOriginalMaterial(RenderersMarkers.KandraMarker marker, Material material) {
                this.marker = marker;
                this.material = material;
            }
        }
        
        enum FeaturesState : byte {
            Undefined = 0,
            Hidden = 1,
            Showed = 2,
        }
    }

    public enum GeneralBodyState {
        None = 0,
        RedDeath = 1
    }
}