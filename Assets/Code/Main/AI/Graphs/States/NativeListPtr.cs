namespace Awaken.TG.Main.AI.Graphs.States {
    public struct NativeListPtr {
        public int Start { [UnityEngine.Scripting.Preserve] get; }
        public int Length { get; private set; }

        public NativeListPtr(int start) {
            Start = start;
            Length = 0;
        }

        [UnityEngine.Scripting.Preserve]
        public void Prolong(int size) {
            Length += size;
        }
    }
}