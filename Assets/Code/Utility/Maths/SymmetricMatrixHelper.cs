namespace Awaken.Utility.Maths {
    public static class SymmetricMatrixHelper {
        /// <param name="lastElementIndex">Index of the last element from which matrix was built</param>
        /// <returns>Index of value for symmetric matrix</returns>
        static int MatrixIndex(int index1, int index2, int lastElementIndex) {
            var pair = new SymmetricMatrixIndexingPair(index1, index2);
            var sequenceValue = pair.indexMin * (pair.indexMin - 1) / 2;
            var rawStartIndex = pair.indexMin*lastElementIndex - sequenceValue;
            var columnOffset = pair.indexMax - pair.indexMin - 1;
            return rawStartIndex + columnOffset;
        }

        static int IndexingSequenceValue(int row) {
            return row * (row - 1) / 2;
        }
    }
}
