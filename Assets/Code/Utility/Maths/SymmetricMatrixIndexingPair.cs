namespace Awaken.Utility.Maths {
    public readonly struct SymmetricMatrixIndexingPair {
        public readonly int indexMin;
        public readonly int indexMax;

        public SymmetricMatrixIndexingPair(int index1, int index2) {
            if (index1 < index2) {
                indexMin = index1;
                indexMax = index2;
            } else {
                indexMin = index2;
                indexMax = index1;
            }
        }
    }
}
