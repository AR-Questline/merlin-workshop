using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public sealed partial class MeshFeature : BodyFeature, ICoverMesh {
        public override ushort TypeForSerialization => SavedTypes.MeshFeature;

        [Saved] ARAssetReference _asset;

        GameObject _instance;
        HairController _hairController;

        bool _isCoverable;
        CoverType _coverType;
        CancellationTokenSource _destroyCancellationTokenSource;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] MeshFeature() { }
        public MeshFeature(ARAssetReference asset) {
            _asset = asset;
        }

        public override async UniTask Spawn() {
            var baseClothes = Features.BaseClothes;
            if (baseClothes == null) return;

            _destroyCancellationTokenSource?.Cancel();
            _destroyCancellationTokenSource = null;
            
            (GameObject go, bool _) = await Features.BaseClothes.EquipTask(_asset);
            _instance = go;
            if (_instance == null) {
                return;
            }

            var coverSettings = _instance.GetComponentsInChildren<MeshCoverSettings>();
            if (coverSettings.IsNotEmpty()) {
                _coverType = CoverType.None;
                _isCoverable = true;
                foreach (var coverSetting in coverSettings) {
                    _isCoverable &= !coverSetting.IsCover;
                    _coverType |= coverSetting.Type;
                }
            }
            if (_isCoverable) {
                Features.AddCoverableMesh(this);
            }

            _hairController = _instance.GetComponentInChildren<HairController>();
            if (_hairController != null) {
                Features.AddHairFeature(_hairController);
            }
            baseClothes.Trigger(BaseClothes.Events.ClothEquipped, _instance);
        }

        public override async UniTask Release(bool prettySwap = false) {
            if (!prettySwap) {
                await AsyncRelease();
                return;
            }
            
            if (_isCoverable) {
                Features.RemoveCoverableMesh(this);
            }
            Features.RemoveHairFeature(_hairController);
            Features.BaseClothes?.Unequip(_asset);
            _isCoverable = false;
            _coverType = CoverType.None;
            _instance = null;
            _hairController = null;
        }

        public async UniTask AsyncRelease() {
            if (_destroyCancellationTokenSource is { IsCancellationRequested: true }) {
                return;
            }
            _destroyCancellationTokenSource = new();
            var cancellationToken = _destroyCancellationTokenSource.Token;
            if (await AsyncUtil.DelayFrame(Features, cancellationToken: cancellationToken)) {
                if (_isCoverable) {
                    Features.RemoveCoverableMesh(this);
                }
                Features.RemoveHairFeature(_hairController);
                Features.BaseClothes?.Unequip(_asset);
            }
            if (!cancellationToken.IsCancellationRequested) {
                _isCoverable = false;
                _coverType = CoverType.None;
                _instance = null;
                _hairController = null;
            }
        }

        public void RefreshCover(CoverType cover) {
            if (_isCoverable && _instance) {
                _instance.SetActive((_coverType | cover) != cover);
            }
        }

        public MeshFeature Copy() {
            return new MeshFeature(new ARAssetReference(_asset.Address, _asset.SubObjectName));
        }
        public override BodyFeature GenericCopy() => Copy();

        bool Equals(MeshFeature other) {
            return Equals(_asset, other._asset);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MeshFeature)obj);
        }

        public override int GetHashCode() {
            return (_asset != null ? _asset.GetHashCode() : 0);
        }
    }
}