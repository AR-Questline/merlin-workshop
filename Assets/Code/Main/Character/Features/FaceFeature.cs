using Awaken.Utility;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public partial class FaceFeature : BodyFeature, ICoverMesh {
        public override ushort TypeForSerialization => SavedTypes.FaceFeature;
        
        const CoverType FaceCovered = CoverType.FullFaceCovered;

        GameObject _instance;

        public override UniTask Spawn() {
            if (Features.HasBeenDiscarded) {
                return UniTask.CompletedTask;
            }
            // Find face mesh
            var markers = Features.GameObject.GetComponentInChildren<RenderersMarkers>(true);
            if (markers) {
                for (int i = 0; i < markers.KandraMarkers.Length; i++) {
                    if (markers.KandraMarkers[i].MaterialType.HasFlagFast(RendererMarkerMaterialType.Face)) {
                        _instance = markers.KandraMarkers[i].Renderer.gameObject;
                        break;
                    }
                }
            }
            Features.AddCoverableMesh(this);
            return UniTask.CompletedTask;
        }

        public override UniTask Release(bool _ = false) {
            Features.RemoveCoverableMesh(this);
            return UniTask.CompletedTask;
        }

        public FaceFeature Copy() {
            FaceFeature copy = new FaceFeature {
                _instance = _instance,
            };
            return copy;
        }
        public override BodyFeature GenericCopy() => Copy();

        public void RefreshCover(CoverType cover) {
            if (_instance != null) {
                _instance.SetActive((FaceCovered | cover) != cover);
            }
        }
    }
}