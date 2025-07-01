using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Utility.VSDatums.TypeInstances {
    public class VSDatumTypeInstanceRichEnum<T> : VSDatumTypeInstance<T> where T : RichEnum {
        public static readonly VSDatumTypeInstanceRichEnum<T> Instance = new();
        public override T GetDatumValue(in VSDatumValue value) => value.RichEnum as T;
        public override VSDatumValue ToDatumValue(T value) => new() { RichEnum = value };
    }
}
