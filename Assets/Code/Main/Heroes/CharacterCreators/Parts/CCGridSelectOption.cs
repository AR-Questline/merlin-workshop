using Awaken.TG.Assets;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    public abstract partial class CCGridSelectOption : Element<CCGridSelect>, ICCPromptSource {
        public int Index { get; init; }
        public virtual bool IsSet => true;

        public int SavedValue => ParentModel.SavedValue;
        public CharacterCreator CharacterCreator => ParentModel.CharacterCreator;

        public void Select() {
            FMODManager.PlayOneShot(Services.Get<CommonReferences>().AudioConfig.ButtonClickedSound);
            ParentModel.Select(Index);
        }

        public void FocusAbove() {
            if (ParentModel.IsInFirstRow(Index)) {
                ParentModel.FocusAbove(Index, ParentModel.ColumnCount);
            } else {
                FocusWithOffset(-ParentModel.ColumnCount);
            }
        }

        public void FocusBelow() {
            if (ParentModel.IsInLastRow(Index)) {
                ParentModel.FocusBelow(Index, ParentModel.ColumnCount);
            } else {
                FocusWithOffset(ParentModel.ColumnCount);
            }
        }

        public void FocusLeft() {
            FocusWithOffset(-1);
        }

        public void FocusRight() {
            FocusWithOffset(1);
        }

        void FocusWithOffset(int offset) {
            var options = ParentModel.AvailableOptions;
            var index = options.IndexOf(this) + offset;
            index = Mathf.Clamp(index, 0, options.Length - 1);
            World.Only<Focus>().Select(options[index].MainView);
        }
    }

    [SpawnsView(typeof(VCCGridSelectIconOption))]
    public partial class CCGridSelectIconOption : CCGridSelectOption {
        public SpriteReference Icon { get; private set; }
        public override bool IsSet => Icon is {IsSet: true};

        protected override void OnInitialize() {
            ShareableSpriteReference dataGetSpriteOf = ParentModel.Data.GetSpriteOf(Index);
            if (dataGetSpriteOf.IsSet) {
                Icon = dataGetSpriteOf.Get();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Icon?.Release();
            Icon = null;
        }
    }

    [SpawnsView(typeof(VCCGridSelectColorOption))]
    public partial class CCGridSelectColorOption : CCGridSelectOption {
        public Color Color => ParentModel.Data.GetColorOf(Index);
    }
}