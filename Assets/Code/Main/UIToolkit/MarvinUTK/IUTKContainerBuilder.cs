using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.MarvinUTK {
    public interface IUTKContainerBuilder {
        List<IUTKControlsFactory<VisualElement>> Factories { get; }
        VisualElement Build();
        IUTKContainerBuilder Add(IUTKControlsFactory<VisualElement> factory);
    }
}