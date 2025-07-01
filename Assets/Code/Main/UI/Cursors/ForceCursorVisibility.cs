using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.Cursors {
    public partial class ForceCursorVisibility : Element<IModel> {
        public sealed override bool IsNotSaved => true;

        public bool ShouldBeVisible { get; }

        public ForceCursorVisibility(bool shouldBeVisible) {
            ShouldBeVisible = shouldBeVisible;
        }
    }
}