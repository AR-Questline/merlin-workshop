namespace Awaken.Kandra {
    public enum KandraBoundsAmplifier : byte {
        None = 0,
        Face = 1,
        SuperHuge = 2,
    }

    public static class KandraBoundsAmplifierExtensions {
        public static float Multiplier(this KandraBoundsAmplifier amplifier) {
            return amplifier switch {
                KandraBoundsAmplifier.None => 1f,
                KandraBoundsAmplifier.Face => 2f,
                KandraBoundsAmplifier.SuperHuge => 15f,
                _ => 1f,
            };
        }
    }
}