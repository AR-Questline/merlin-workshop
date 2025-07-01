namespace Awaken.TG.Main.UIToolkit.PresenterData.Notifications {
    public interface IPresenterNotificationData : IPresenterData {
        public float VisibilityDuration { get; }
        public float FadeDuration { get; }
    }
}