namespace Awaken.TG.Main.Utility.VSDatums.TypeInstances {
    public class VSDatumTypeInstanceInt : VSDatumTypeInstance<int> {
        public static readonly VSDatumTypeInstanceInt Instance = new();
        public override int GetDatumValue(in VSDatumValue value) => value.Int;
        public override VSDatumValue ToDatumValue(int value) => new() { Int = value };
    }
}