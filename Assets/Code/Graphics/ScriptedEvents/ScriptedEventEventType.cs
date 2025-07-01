namespace Awaken.TG.Graphics.ScriptedEvents {
    public enum ScriptedEventEventType : byte {
        [UnityEngine.Scripting.Preserve] None,
        IncreaseProlongedAssetRefCount,
        DecreaseProlongedAssetRefCount,
        IncreaseMainAssetRefCount,
        DecreaseMainAssetRefCount,
    }
}