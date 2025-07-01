namespace Awaken.TG.Main.UIToolkit.PresenterData.Notifications {
    public interface IPresenterNotificationDataWithHeight : IPresenterNotificationData {
        public float DefaultHeight { get; }
        public float HeightDuration { get; }
    }
}