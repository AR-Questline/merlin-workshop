namespace FMODUnity {
    public readonly struct FMODParameter {
        public readonly string name;
        public readonly float value;

        public FMODParameter(string name, float value) {
            this.name = name;
            this.value = value;
        }

        public FMODParameter(string name, bool value) {
            this.name = name;
            this.value = value ? 1 : 0;
        }
    }
}