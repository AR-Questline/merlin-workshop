namespace Awaken.TG.Graphics.Cutscenes {
    public interface ICutsceneAttachment {
        public void OnCutsceneInit(VCutsceneBase vCutscene);
        public void OnCutsceneStart(VCutsceneBase vCutscene);
        public void OnCutsceneEnd(VCutsceneBase vCutscene);
        public void OnCutscenePaused();
        public void OnCutsceneUnpaused();
    }
}