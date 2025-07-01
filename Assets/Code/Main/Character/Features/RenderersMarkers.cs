using System;
using Awaken.Kandra;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public class RenderersMarkers : MonoBehaviour {
        [field: SerializeField] public KandraMarker[] KandraMarkers { get; set; }

        [Serializable]
        public struct KandraMarker : IEquatable<KandraMarker> {
            [field: SerializeField] public RendererMarkerMaterialType MaterialType { get; private set; }
            [field: SerializeField] public KandraRenderer Renderer { get; private set; }
            [field: SerializeField] public int Index { get; private set; }

            public KandraMarker(RendererMarkerMaterialType materialType, KandraRenderer renderer, int index) {
                MaterialType = materialType;
                Renderer = renderer;
                Index = index;
            }

            public void Deconstruct(out KandraRenderer renderer, out int index, out RendererMarkerMaterialType materialType) {
                renderer = Renderer;
                index = Index;
                materialType = MaterialType;
            }

            public bool Equals(KandraMarker other) {
                return MaterialType == other.MaterialType && Renderer == other.Renderer && Index == other.Index;
            }
            public override bool Equals(object obj) {
                return obj is KandraMarker other && Equals(other);
            }
            public override int GetHashCode() {
                unchecked {
                    int hashCode = (int)MaterialType;
                    hashCode = (hashCode * 397) ^ (Renderer != null ? Renderer.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ Index;
                    return hashCode;
                }
            }
            public static bool operator ==(KandraMarker left, KandraMarker right) {
                return left.Equals(right);
            }
            public static bool operator !=(KandraMarker left, KandraMarker right) {
                return !left.Equals(right);
            }
        }
    }
}