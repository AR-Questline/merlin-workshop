using System;
using Awaken.Utility;
using Awaken.Utility.LowLevel.Collections;
using UnityEngine;

namespace Awaken.Kandra {
    [ExecuteInEditMode]
    public class KandraTrisCuller : MonoBehaviour {
        public CulledMesh[] culledMeshes = Array.Empty<CulledMesh>();
        private FrugalList<KandraTrisCullee> _cullees;

        public void Cull(KandraTrisCullee cullee) {
            throw new NotImplementedException();
        }

        public void Uncull(KandraTrisCullee cullee) {
            throw new NotImplementedException();
        }

        public void DisableCulledTriangles(Guid culleeGuid, ref UnsafeBitmask visibleTris) {
            throw new NotImplementedException();
        }

        public struct CulledMesh {
            public SerializableGuid culleeId;
            public CulledRange[] culledRanges;
        }

        public struct CulledRange {
            public uint start;
            public ushort length;
        }

        public struct EditorAccess {
            public static ref readonly FrugalList<KandraTrisCullee> Cullees(KandraTrisCuller culler) =>
                ref culler._cullees;
        }
    }
}