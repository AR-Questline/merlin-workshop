using System;
using Awaken.Utility.Enums;
using FMODUnity;

namespace Awaken.TG.Main.Utility.Audio {
    public class ARAudioType<T> : ARAudioType {
        readonly Func<T, EventReference> _getter;

        protected ARAudioType(string id, Func<T, EventReference> getter, string inspectorCategory) : base(id, inspectorCategory) {
            _getter = getter;
        }

        public EventReference RetrieveFrom(T audioOwner) => _getter(audioOwner);
    }
    
    public class ARAudioType : RichEnum {
        protected ARAudioType(string enumName, string inspectorCategory = "") : base(enumName, inspectorCategory) { }
    }
}