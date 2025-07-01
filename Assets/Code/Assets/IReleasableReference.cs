namespace Awaken.TG.Assets {
    /// <summary>
    /// Mark class as Releasable in order to couple it with View auto asset release system
    /// </summary>
    public interface IReleasableReference {
        void Release();
    }
}