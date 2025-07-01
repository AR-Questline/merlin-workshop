using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public interface IViewCompareTooltipSystem : IView {
        void ComparerAppear(bool instant);
        void ComparerDisappear(bool instant);
    }
}