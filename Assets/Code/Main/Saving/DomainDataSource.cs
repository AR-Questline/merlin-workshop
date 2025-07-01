namespace Awaken.TG.Main.Saving {
    public enum DomainDataSource : byte {
        [UnityEngine.Scripting.Preserve] Invalid = 0,
        FromGameState = 1,
        FromSaveFile = 2,
    }
}