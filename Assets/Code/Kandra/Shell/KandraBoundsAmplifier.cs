using System;

namespace Awaken.Kandra {
    public enum KandraBoundsAmplifier : byte {
        None = 0,
        Face = 1,
        SuperHuge = 2,
    }

    public static class KandraBoundsAmplifierExtensions {
        public static float Multiplier(this KandraBoundsAmplifier amplifier) {
            throw new NotImplementedException();
        }
    }
}