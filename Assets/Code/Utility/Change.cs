namespace Awaken.Utility {
    public readonly struct Change<T> {
        public readonly T from;
        public readonly T to;

        public Change(T from, T to) {
            this.from = from;
            this.to = to;
        }

        public void Deconstruct(out T from, out T to) {
            from = this.from;
            to = this.to;
        }
    }
}