using System;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Main.Utility.VSDatums.TypeInstances {
    public class VSDatumTypeInstanceEnum<TEnum> : VSDatumTypeInstance<TEnum> where TEnum : Enum {
        public static readonly VSDatumTypeInstanceEnum<TEnum> Instance = new();
        public override TEnum GetDatumValue(in VSDatumValue value) => value.Int.ToEnum<TEnum>();
        public override VSDatumValue ToDatumValue(TEnum value) => new() { Int = value.ToInt() };
    }
}