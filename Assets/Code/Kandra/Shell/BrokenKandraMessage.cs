using System;
using Awaken.Utility.LowLevel;

namespace Awaken.Kandra {
    public static class BrokenKandraMessage {
        public static void EDITOR_RuntimeReset() {
            throw new NotImplementedException();
        }

        public static string AppendMessageInfo(string message, uint wantedElements, MemoryBookkeeper memory) {
            throw new NotImplementedException();
        }

        public static void OutOfMemory(string inputMessage, KandraRenderer renderer) {
            throw new NotImplementedException();
        }

        public static void DataMismatch(KandraMesh kandraMesh, uint expectedSize, uint serializedData) {
            throw new NotImplementedException();
        }
    }
}