using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    public abstract partial class CharacterCreatorPart : Element<ICharacterCreatorTab> {
        public sealed override bool IsNotSaved => true;
        
        public abstract string Title { get; }
        
        public void FocusAbove(int index, int columns) {
            FocusAbove((index % columns) / (float)(columns - 1));
        }
        public void FocusAbove(float horizontalPercent = 0) {
            var parts = ParentModel.Elements<CharacterCreatorPart>().GetEnumerator();
            if (!parts.MoveNext()) {
                return;
            }
            var previous = parts.Current;
            while (parts.MoveNext()) {
                var next = parts.Current;
                if (next == this) {
                    previous.View<IVCCFocusablePart>().ReceiveFocusFromBottom(horizontalPercent);
                    break;
                }
                previous = next;
            }
        }

        public void FocusBelow(int index, int columns) {
            FocusBelow((index % columns) / (float)(columns - 1));
        }
        public void FocusBelow(float horizontalPercent = 0) {
            var parts = ParentModel.Elements<CharacterCreatorPart>().GetEnumerator();
            if (!parts.MoveNext()) {
                return;
            }
            var previous = parts.Current;
            while (parts.MoveNext()) {
                var next = parts.Current;
                if (previous == this) {
                    next.View<IVCCFocusablePart>().ReceiveFocusFromTop(horizontalPercent);
                    break;
                }
                previous = next;
            }
        }
    }

    public interface IVCCFocusablePart : IView {
        void ReceiveFocusFromTop(float horizontalPercent);
        void ReceiveFocusFromBottom(float horizontalPercent);
    }
}