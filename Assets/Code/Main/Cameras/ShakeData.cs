namespace Awaken.TG.Main.Cameras {
    [UnityEngine.Scripting.Preserve]
    public struct ShakeData {
        [UnityEngine.Scripting.Preserve] public readonly float force;
        [UnityEngine.Scripting.Preserve] public readonly float drop;

        public ShakeData(float f, float d) {
            force = f;
            drop = d;
        }
    }
}
