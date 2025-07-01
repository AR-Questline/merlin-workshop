using System;
using UnityEngine;

namespace Awaken.Utility.Collections {
    [Serializable]
    public class SymmetricMatrix<T> {
        [SerializeField] Row[] rows;

        public SymmetricMatrix(int size) {
            rows = new Row[size];
            for (int i = 0; i < size; i++) {
                rows[i] = new Row(i+1);
            }
        }

        public int Size => rows.Length;
        public T this[int i, int j] {
            get => i < j ? rows[j][i] : rows[i][j];
            set {
                if (i < j) {
                    rows[j][i] = value;
                } else {
                    rows[i][j] = value;
                }
            }
        }

        [Serializable]
        struct Row {
            [SerializeField] T[] values;

            public Row(int length) {
                values = new T[length];
            }

            public T this[int i] {
                get => values[i];
                set => values[i] = value;
            }
        }
    }

    [Serializable]
    public class SymmetricMatrixBool : SymmetricMatrix<bool> {
        public SymmetricMatrixBool(int size) : base(size) { }
    }
}