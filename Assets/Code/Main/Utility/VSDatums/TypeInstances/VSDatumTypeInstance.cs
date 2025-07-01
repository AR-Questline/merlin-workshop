using System;

namespace Awaken.TG.Main.Utility.VSDatums.TypeInstances {
    public abstract class VSDatumTypeInstance {
        public abstract Type GetDatumType();
        public abstract object GetBoxedDatumValue(in VSDatumValue value);
    }
    
    public abstract class VSDatumTypeInstance<T> : VSDatumTypeInstance {
        public sealed override Type GetDatumType() => typeof(T);
        public sealed override object GetBoxedDatumValue(in VSDatumValue value) => GetDatumValue(value);

        public abstract T GetDatumValue(in VSDatumValue value);
        public abstract VSDatumValue ToDatumValue(T value);
    }
}