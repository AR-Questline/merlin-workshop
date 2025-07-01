using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.MarvinUTK {
    public interface IUTKControlsFactory<out T> where T : VisualElement {
        T Create();
    }
}