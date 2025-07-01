namespace Awaken.TG.Assets {
    public interface IReleasableOwner {
        void RegisterReleasableHandle(IReleasableReference releasableReference);
        void ReleaseReleasable();
    }
}