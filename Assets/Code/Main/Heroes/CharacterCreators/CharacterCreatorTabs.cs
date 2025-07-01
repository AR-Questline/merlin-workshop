using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterCreators {
    public partial class CharacterCreatorTabs : Tabs<CharacterCreator, VCharacterCreatorTabs, CharacterCreatorTabType, ICharacterCreatorTab> {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.Previous;
        protected override KeyBindings Next => KeyBindings.UI.Generic.Next;

        protected override void ChangeTab(CharacterCreatorTabType type) {
            ParentModel.ResetPrompts(true);
            base.ChangeTab(type);
        }
    }

    public class CharacterCreatorTabType : CharacterCreatorTabs.DelegatedTabTypeEnum {
        [UnityEngine.Scripting.Preserve]
        public static readonly CharacterCreatorTabType 
            Body = new(nameof(Body), _ => new CharacterCreatorBodyTab()),
            Face = new(nameof(Face), _ => new CharacterCreatorFaceTab()),
            Hair = new(nameof(Hair), _ => new CharacterCreatorHairTab()),
            Tattoo = new(nameof(Tattoo), _ => new CharacterCreatorTattooTab()),
            Name = new(nameof(Name), _ => new CharacterCreatorNameTab());
        CharacterCreatorTabType(string enumName, SpawnDelegate spawn, string titleId = "") : base(enumName, titleId, spawn, Always) { }
    }

    public interface ICharacterCreatorTab : CharacterCreatorTabs.ITab { }
    public abstract partial class CharacterCreatorTab<TTabView> : CharacterCreatorTabs.Tab<TTabView>, ICharacterCreatorTab where TTabView : View { }
    
    public partial class CharacterCreatorBodyTab : CharacterCreatorTab<VCharacterCreatorBody> { }
    public partial class CharacterCreatorFaceTab : CharacterCreatorTab<VCharacterCreatorFace> { }
    public partial class CharacterCreatorHairTab : CharacterCreatorTab<VCharacterCreatorHair> { }
    public partial class CharacterCreatorTattooTab : CharacterCreatorTab<VCharacterCreatorTattoo> { }
    public partial class CharacterCreatorNameTab : CharacterCreatorTab<VCharacterCreatorName> { }
}