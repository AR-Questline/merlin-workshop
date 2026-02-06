using System;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using UnityEngine;

namespace Awaken.Kandra {
    public class KandraTrisCullee : MonoBehaviour {
        public SerializableGuid id;
        public KandraRenderer kandraRenderer;
        private StructList<KandraTrisCuller> _cullers;

        public void Cull(KandraTrisCuller culler) {
            throw new NotImplementedException();
        }

        public void Uncull(KandraTrisCuller culler) {
            throw new NotImplementedException();
        }

        public struct EditorAccess {
            public static ref readonly StructList<KandraTrisCuller> Cullers(KandraTrisCullee cullee) =>
                ref cullee._cullers;

            public static UnsafeBitmask GetVisibleTriangles(KandraTrisCullee cullee, Allocator allocator) {
                throw new NotImplementedException();
            }

            public static void UpdateCulledMesh(KandraTrisCullee cullee) {
                throw new NotImplementedException();
            }
        }
    }
}