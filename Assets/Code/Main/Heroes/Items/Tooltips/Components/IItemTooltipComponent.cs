using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    public interface IItemTooltipComponent {
        View TargetView { get; set; }
        ref PartialVisibility Visibility { get; }
        bool UseReadMore { get; }
        bool UseReadMoreEnabled { get; set; }
        
        void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare);
        void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare, View view);
        void ToggleSectionActive(bool active);
        
        void SetupComponent(View view, bool useReadMoreEnabled = false) {
            TargetView = view;
            UseReadMoreEnabled = useReadMoreEnabled;
            Visibility = PartialVisibility.Visible;
            Visibility.OnVisibilityChanged += ToggleSectionActive;
        }
    }
}