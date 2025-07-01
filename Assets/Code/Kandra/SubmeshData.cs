using System;

namespace Awaken.Kandra {
    [Serializable]
    public struct SubmeshData {
        public uint indexStart;
        public uint indexCount;

        public override string ToString() {
            return $"SubmeshData: [{indexStart}, {indexStart+indexCount})<{indexCount}>";
        }
    }
}
