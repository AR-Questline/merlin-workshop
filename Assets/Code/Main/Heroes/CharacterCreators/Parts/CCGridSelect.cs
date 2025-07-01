using System;
using System.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    public partial class CCGridSelect : CharacterCreatorPart {
        CCGridSelectData _data;

        public ref CCGridSelectData Data => ref _data;
        public override string Title => _data.Title;
        public int SavedValue {
            get => _data.GetSavedValue();
            private set => _data.SetSavedValue(value);
        }

        public CharacterCreator CharacterCreator => ParentModel.ParentModel;

        public uint OptionsCount => Elements<CCGridSelectOption>().Count(o => o.IsSet);
        public int ColumnCount { get; set; }
        public int RowsCount => Mathf.CeilToInt(OptionsCount / (float) ColumnCount);
        public int FirstIndexInLastRow => (RowsCount - 1) * ColumnCount;
        public CCGridSelectOption[] AvailableOptions => Elements<CCGridSelectOption>().Where(o => o.IsSet).ToArray();

        public CCGridSelect(Func<CharacterCreator, CCGridSelectData> provider, CharacterCreator creator) {
            _data = provider(creator);
        }

        public void AfterViewSpawned() {
            if (_data.Type == GridSelectType.Icon) {
                SpawnOptions<CCGridSelectIconOption>();
            } else if (_data.Type == GridSelectType.Color) {
                SpawnOptions<CCGridSelectColorOption>();
            }
        }

        public void Select(int index) {
            SavedValue = index;
        }

        void SpawnOptions<TOption>() where TOption : CCGridSelectOption, new() {
            for (int i = 0; i < Data.Count; i++) {
                AddElement(new TOption { Index = i });
            }
        }

        public bool IsInFirstRow(int index) {
            return index < ColumnCount;
        }
        
        public bool IsInLastRow(int index) {
            return index >= FirstIndexInLastRow;
        }
    }
}