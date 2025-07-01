namespace Awaken.TG.Main.Heroes.CharacterSheet.Map {
    public class VCMapSubTabButton : MapSubTabsUI.VCHeaderTabButton {
        MapSubTabType _type;
        
        public override MapSubTabType Type => _type;
        public override string ButtonName => _type.Name;

        public void Setup(MapSubTabType type) {
            _type = type;
        }
    }
}