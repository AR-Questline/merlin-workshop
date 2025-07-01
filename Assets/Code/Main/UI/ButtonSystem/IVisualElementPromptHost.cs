using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.ButtonSystem {
    public interface IVisualElementPromptHost : IPromptHost, IView {
        Transform IPromptHost.PromptsHost => transform;
        VisualElement VisualPromptHost { get; set; }

        void Add(VisualElement prompt) {
            VisualPromptHost.Add(prompt);
        }
    }
}