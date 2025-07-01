using Awaken.TG.Main.Utility.RichLabels.SO;

namespace Awaken.TG.Main.Utility.RichLabels {
    public interface IRichLabelUser {
        public RichLabelSet RichLabelSet { get; }
        public RichLabelConfigType RichLabelConfigType { get; }
        public bool DisplayDropdown => true;
        public bool AutofillEnabled { get; }
        public void Editor_Autofill();
    }
}