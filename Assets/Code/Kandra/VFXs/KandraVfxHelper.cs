using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Awaken.Kandra.VFXs {
    public class KandraVfxHelper {
        Dictionary<int, IndicesData> _indicesBuffers = new Dictionary<int, IndicesData>();

        public void Dispose() {
            foreach (var (hash, data) in _indicesBuffers) {
                data.buffer.Release();
            }
            _indicesBuffers.Clear();
        }

        public unsafe GraphicsBuffer GetIndexBuffer(KandraMesh mesh) {
            var hash = mesh.GetHashCode();
            if (!_indicesBuffers.TryGetValue(hash, out var data)) {
                var indices = KandraRendererManager.Instance.StreamingManager.LoadIndicesData(mesh);
                UnsafeArray<uint> localBuffer;
                if (indices.Length % 2 == 0) {
                    localBuffer = indices.AsUnsafeArray().As<uint>().AsUnsafeArray();
                } else {
                    localBuffer = new UnsafeArray<uint>(indices.Length/2, ARAlloc.Temp);
                    UnsafeUtility.MemCpy(localBuffer.Ptr, indices.Ptr, indices.Length * sizeof(ushort));
                }
                var buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)indices.Length, sizeof(uint));
                buffer.SetData(localBuffer.AsNativeArray());
                data = new IndicesData(buffer);

                localBuffer.Dispose();
            } else {
                data = data.IncrementRefCount();
            }
            _indicesBuffers[hash] = data;
            return data.buffer;
        }

        public void ReleaseIndexBuffer(KandraMesh mesh) {
            var hash = mesh.GetHashCode();
            if (!_indicesBuffers.TryGetValue(hash, out var data)) {
                return;
            }
            data = data.DecrementRefCount();
            if (data.refCount == 0) {
                data.buffer.Release();
                _indicesBuffers.Remove(hash);
            } else {
                _indicesBuffers[hash] = data;
            }
        }

        public readonly struct IndicesData {
            public readonly GraphicsBuffer buffer;
            public readonly ushort refCount;

            public IndicesData(GraphicsBuffer buffer, ushort refCount = 1) {
                this.buffer = buffer;
                this.refCount = refCount;
            }

            public IndicesData IncrementRefCount() {
                return new IndicesData(buffer, (ushort)(refCount + 1));
            }

            public IndicesData DecrementRefCount() {
                return new IndicesData(buffer, (ushort)(refCount - 1));
            }
        }

        public readonly struct EditorAccess {
            readonly KandraVfxHelper _helper;

            public Dictionary<int, IndicesData> IndicesBuffers => _helper._indicesBuffers;

            public EditorAccess(KandraVfxHelper helper) {
                _helper = helper;
            }

            public static EditorAccess Get() {
                return new EditorAccess(KandraRendererManager.Instance.KandraVfxHelper);
            }
        }
    }
}
