namespace Awaken.TG.Editor.Localizations {
    public static class DataToExportExtensions {
        public static DataToExportConverted ToDataToExportConverted(this DataToExport dataToExport) {
            return new DataToExportConverted(dataToExport);
        }
    }
}