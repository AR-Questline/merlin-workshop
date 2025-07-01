using Awaken.Utility.LowLevel;

namespace Awaken.TG.Editor.DataViews.Structure {
    public unsafe struct DataViewCell {
        public IDataViewSource source;
        public UniversalPtr headerMetadata;
        public UniversalPtr typeMetadata;

        public DataViewCell(IDataViewSource source, in UniversalPtr headerMetadata, in UniversalPtr typeMetadata) {
            this.source = source;
            this.headerMetadata = headerMetadata;
            this.typeMetadata = typeMetadata;
        }
    }
}