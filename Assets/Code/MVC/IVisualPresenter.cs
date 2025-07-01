using UnityEngine.UIElements;

namespace Awaken.TG.MVC {
    /// <summary>
    /// Provides basic information for controlling UIToolkit elements.
    /// </summary>
    public interface IVisualPresenter {
        VisualElement Content { get; }
    }
}
