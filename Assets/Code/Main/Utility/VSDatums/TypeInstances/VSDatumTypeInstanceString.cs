namespace Awaken.TG.Main.Utility.VSDatums.TypeInstances {
    public class VSDatumTypeInstanceString : VSDatumTypeInstance<string> {
        public static readonly VSDatumTypeInstanceString Instance = new();
        public override string GetDatumValue(in VSDatumValue value) => value.String;
        public override VSDatumValue ToDatumValue(string value) => new() { String = value };
    }
}