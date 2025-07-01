namespace Awaken.TG.Main.Cameras.Controllers {
    /// <summary>
    /// Interface for camera components. Managed by VGameCamera.
    /// </summary>
    public interface ICameraController {
        /// <summary>
        /// Init is called after all Camera Components have been attached. Here they can get references to each other.
        /// </summary>
        void Init();
        void Refresh(bool active);
        void OnChanged(bool active);
    }
}