namespace Awaken.TG.Editor.Localizations {
    /// <summary>
    /// Used for exporting data to Google Sheets.
    /// </summary>
    public struct DataToExportConverted {
        
        public string ID { get; }
        public string Source { get; }
        public string Category { get; }
        public string Gesture { get; }
        public string Actor { get; }
        public string PreviousLine { get; }
        public string Translation { get; }
        public string Language { get; }
        
        public DataToExportConverted(DataToExport dataToExport) {
            // LocalizationExporterUtilities.TermDataConverter termDataConverter = new();
            // LocalizationExporterUtilities.TermDataGroupedConverter termDataGroupedConverter = new();
            
            // ID = termDataGroupedConverter.ConvertToString(dataToExport.groupedTermsData, null, null);
            ID = "";
            Source = dataToExport.Source;
            Category = dataToExport.category;
            Gesture = dataToExport.gesture;
            Actor = dataToExport.actor;
            PreviousLine = dataToExport.previousLine;
            Translation = dataToExport.translation;
            // Language = termDataConverter.ConvertToString(dataToExport.CurrentLocale, null, null);
            Language = "";
        }
    }
}