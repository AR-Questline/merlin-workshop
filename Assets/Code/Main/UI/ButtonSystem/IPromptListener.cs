namespace Awaken.TG.Main.UI.ButtonSystem {
    public interface IPromptListener {
        void OnHoldKeyDown(Prompt source) { }
        void OnHoldKeyHeld(Prompt source, float percent) { }
        void OnHoldKeyUp(Prompt source, bool completed = false) { }
        void OnTap(Prompt source) { }
        void OnHoldPromptInterrupted(Prompt source) { }

        void SetName(string name);
        void SetActive(bool active);
        void SetVisible(bool visible);
    }
}