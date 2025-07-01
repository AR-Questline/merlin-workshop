namespace Awaken.TG.MVC.Utils {
    /// <summary>
    /// Marker interface for models that can be cloned.
    /// </summary>
    public interface ICloneAbleModel : IModel {
        void CopyPropertiesTo(Model clone);
    }
}