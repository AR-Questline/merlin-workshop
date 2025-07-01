namespace Awaken.TG.Main.Utility.VSDatums.TypeInstances {
    public class VSDatumTypeInstanceBool : VSDatumTypeInstance<bool> {
        public static readonly VSDatumTypeInstanceBool Instance = new();
        public override bool GetDatumValue(in VSDatumValue value) => value.Bool;
        public override VSDatumValue ToDatumValue(bool value) => new() { Bool = value };
    }
}