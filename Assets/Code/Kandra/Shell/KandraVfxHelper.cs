using System;
using System.Collections.Generic;
using UnityEngine;

namespace Awaken.Kandra.VFXs {
    public class KandraVfxHelper {
        private Dictionary<int, IndicesData> _indicesBuffers;

        public void Dispose() {
            throw new NotImplementedException();
        }

        public GraphicsBuffer GetIndexBuffer(KandraMesh mesh) {
            throw new NotImplementedException();
        }

        public void ReleaseIndexBuffer(KandraMesh mesh) {
            throw new NotImplementedException();
        }

        public readonly struct IndicesData {
            public readonly GraphicsBuffer buffer;
            public readonly ushort refCount;

            public IndicesData(GraphicsBuffer buffer, ushort refCount = 1) {
                throw new NotImplementedException();
            }

            public IndicesData IncrementRefCount() {
                throw new NotImplementedException();
            }

            public IndicesData DecrementRefCount() {
                throw new NotImplementedException();
            }
        }

        public readonly struct EditorAccess {
            readonly KandraVfxHelper _helper;

            public Dictionary<int, IndicesData> IndicesBuffers => _helper._indicesBuffers;

            public EditorAccess(KandraVfxHelper helper) {
                throw new NotImplementedException();
            }

            public static EditorAccess Get() {
                throw new NotImplementedException();
            }
        }
    }
}