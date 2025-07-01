namespace Awaken.TG.Main.Rendering {
    public class LightRenderLayers {
        const int
            Default = 0,
            UI = 1,
            EnvironmentUI = 2;

        [UnityEngine.Scripting.Preserve]
        public const int
            DefaultMask = 1 << Default,
            UIMask = 1 << UI,
            EnvironmentUIMask = 1 << EnvironmentUI;
    }
}