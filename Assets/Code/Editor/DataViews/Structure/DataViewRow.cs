using System;
using Awaken.TG.Editor.DataViews.Headers;
using Awaken.Utility.LowLevel;

namespace Awaken.TG.Editor.DataViews.Structure {
    public struct DataViewRow {
        public readonly IDataViewSource source;
        public readonly string simpleName;
        public readonly UniversalPtr[] headerMetadata;
        public readonly UniversalPtr[] typeMetadata;
        public bool selected;
        
        DataViewRow(IDataViewSource source, int index, in Precomputed precomputed, int metadatas) {
            this.source = source;
            simpleName = precomputed.names[index][precomputed.commonPrefixLength..];
            headerMetadata = new UniversalPtr[metadatas];
            typeMetadata = new UniversalPtr[metadatas];
            selected = false;
        }

        public DataViewCell this[int i] => new(source, headerMetadata[i], typeMetadata[i]);

        public static DataViewRow[] Create(IDataViewSource[] objects, DataViewHeader[] headers) {
            if (objects.Length == 0) {
                return Array.Empty<DataViewRow>();
            }
            var precomputed = Precompute(objects);
            var result = new DataViewRow[objects.Length];
            for (int i = 0; i < objects.Length; i++) {
                result[i] = new DataViewRow(objects[i], i, in precomputed, headers.Length);
                for (int j = 0; j < headers.Length; j++) {
                    result[i].headerMetadata[j] = headers[j].CreateMetadata(objects[i]);
                    result[i].typeMetadata[j] = headers[j].Type.CreateMetadata();
                }
            }
            return result;
        }
        
        static Precomputed Precompute(IDataViewSource[] objects) {
            var names = new string[objects.Length];
            for (int i = 0; i < objects.Length; i++) {
                names[i] = objects[i].Name;
            }
            int commonPrefix = CalculateCommonPrefix(names);
            return new Precomputed {
                names = names,
                commonPrefixLength = commonPrefix
            };
            
            static int CalculateCommonPrefix(string[] names) {
                int prefix = 0;
                while (true) {
                    if (prefix >= names[0].Length) {
                        return 0; // if any name is contained in all others display whole names
                    }
                    char c = names[0][prefix];
                    for (int i = 1; i < names.Length; i++) {
                        if (prefix >= names[i].Length) {
                            return 0; // if any name is contained in all others display whole names
                        }
                        if (c != names[i][prefix]) {
                            return prefix;
                        }
                    }
                    prefix++;
                }
            }
        }
        
        struct Precomputed {
            public string[] names;
            public int commonPrefixLength;
        }
    }
}