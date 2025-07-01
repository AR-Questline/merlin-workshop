using System;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    public partial class CCSlider : CharacterCreatorPart, ICCPromptSource {
        readonly CCSliderData _data;

        public override string Title => _data.title;
        public int MinValue => _data.minValue;
        public int MaxValue => _data.maxValue;
        public int SavedValue {
            get => _data.getSavedValue();
            private set => _data.setSavedValue(value);
        }

        public bool HasDescription => _data.getDescription != null;
        
        public CharacterCreator CharacterCreator => ParentModel.ParentModel;

        public CCSlider(Func<CharacterCreator, CCSliderData> provider, CharacterCreator creator) {
            _data = provider(creator);
        }

        public void Increase() {
            if (SavedValue < MaxValue) {
                SavedValue++;
                CharacterCreator.Trigger(CharacterCreator.Events.AppearanceChanged, CharacterCreator);
            } else if (_data.cycle) {
                SavedValue = MinValue;
                CharacterCreator.Trigger(CharacterCreator.Events.AppearanceChanged, CharacterCreator);
            }
        }

        public void Decrease() {
            if (SavedValue > MinValue) {
                SavedValue--;
                CharacterCreator.Trigger(CharacterCreator.Events.AppearanceChanged, CharacterCreator);
            } else if (_data.cycle) {
                SavedValue = MaxValue;
                CharacterCreator.Trigger(CharacterCreator.Events.AppearanceChanged, CharacterCreator);
            }
        }

        public bool CanIncrease() {
            return _data.cycle || SavedValue < MaxValue;
        }

        public bool CanDecrease() {
            return _data.cycle || SavedValue > MinValue;
        }
        
        public string NameOfValue(int index) => _data.getNameOf(index);

        public string DescriptionOfValue(int index) => _data.getDescription?.Invoke(index);
    }
}