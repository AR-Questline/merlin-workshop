namespace Awaken.TG.Main.UI.ButtonSystem {
    public interface IVisualElementPromptPresenter {
        IVisualElementPromptHost ViewPromptHost { get; }

        void RegisterPromptHost(IVisualElementPromptHost host);
        void UnregisterPromptHost();
        void AssignPromptRoot();
    }
}