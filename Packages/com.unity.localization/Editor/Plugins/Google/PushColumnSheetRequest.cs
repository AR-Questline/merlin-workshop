using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using UnityEditor.Localization.Plugins.Google.Columns;

namespace UnityEditor.Localization.Plugins.Google
{
    /// <summary>
    /// We do a per column request so that we can preserve any reserved columns.
    /// We use a UpdateCellsRequest instead of doing a batch values update as this allows us to set the note at the same time.
    /// </summary>
    class PushColumnSheetRequest
    {
        public IList<Request> Requests { get; set; }
        public UpdateCellsRequest UpdateCellsRequest { get; set; }
        public UpdateCellsRequest UpdateHeaderRequest { get; set; }
        public List<RowData> Rows { get; private set; } = new List<RowData>();
        public SheetColumn Column { get; private set; }
        public int ColumnIndex { get; private set; }

        public PushColumnSheetRequest(int sheetId, SheetColumn column)
        {
            Column = column;
            ColumnIndex = column.ColumnIndex;

            UpdateHeaderRequest = new UpdateCellsRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartColumnIndex = ColumnIndex,
                    EndColumnIndex = ColumnIndex + 1,
                    StartRowIndex = 0,
                    EndRowIndex = 1 // header only
                },
                Fields = "userEnteredValue,note"
            };

            UpdateCellsRequest = new UpdateCellsRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartColumnIndex = ColumnIndex,
                    EndColumnIndex = ColumnIndex + 1,
                    StartRowIndex = 1, // skip header
                },
                Rows = Rows
            };

            switch (Column.PushFields)
            {
                case PushFields.Value:
                    UpdateCellsRequest.Fields = "userEnteredValue";
                    break;
                case PushFields.Note:
                    UpdateCellsRequest.Fields = "note";
                    break;
                case PushFields.ValueAndNote:
                    UpdateCellsRequest.Fields = "userEnteredValue,note";
                    break;
            }

            Requests = new Request[]
            {
                new Request { UpdateCells = UpdateHeaderRequest },
                new Request { UpdateCells = UpdateCellsRequest }
            };
        }

        public void AddHeader(string value, string note)
        {
            UpdateHeaderRequest.Rows = new RowData[]
            {
                new RowData
                {
                    Values = CreateCell(value, note)
                }
            };
        }

        public void AddRow(string value, string note)
        {
            Rows.Add(new RowData
            {
                Values = CreateCell(value, note)
            });
        }

        static CellData[] CreateCell(string value, string note)
        {
            return new CellData[]
            {
                new CellData
                {
                    UserEnteredValue = new ExtendedValue { StringValue = value },
                    Note = note
                }
            };
        }
    }
}
