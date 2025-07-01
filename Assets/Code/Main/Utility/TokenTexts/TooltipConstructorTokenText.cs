using Awaken.TG.Main.Character;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using Awaken.Utility;

namespace Awaken.TG.Main.Utility.TokenTexts {
    public partial class TooltipConstructorTokenText : TokenText {
        public override ushort TypeForSerialization => SavedTypes.TooltipConstructorTokenText;

        public TooltipConstructor SubTooltip { get; [UnityEngine.Scripting.Preserve] set; }
        
        public TooltipConstructor GetTooltip(ICharacter owner, object payload) {
            TooltipConstructor constructor = new TooltipConstructor();
            foreach (var token in _tokens) {
                if (token.Type == TokenType.TooltipTitle) {
                    constructor.WithTitle(token.GetValue(owner, payload));
                } else if (token.Type == TokenType.TooltipMainText) {
                    constructor.WithMainText(token.GetValue(owner, payload));
                } else if (token.Type == TokenType.TooltipText || token.Type == TokenType.TooltipTextOutOfFight) {
                    constructor.WithText(token.GetValue(owner, payload));
                }
            }

            constructor.SubTooltip = SubTooltip;
            return constructor;
        }
    }
}