namespace Awaken.CommonInterfaces.Animations {
    /// <summary>
    /// A marker interface allowing object to be provided to <see cref="AnimatorBridge"/>.
    /// Implemented properties are fetched only during registration and are expected to be constant.
    /// </summary>
    public interface IAnimatorBridgeStateProvider {
        bool AlwaysAnimate => false;
        bool ForceAnimationCulling => false;
    }
}