using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Requests;
using Google.Apis.Sheets.v4.Data;
using UnityEditor.Localization.Plugins.Google.Columns;
using UnityEditor.Localization.Reporting;
using UnityEngine;
using UnityEngine.Localization.Metadata;
using static Google.Apis.Sheets.v4.SpreadsheetsResource;
using static UnityEngine.Localization.Tables.SharedTableData;
using Data = Google.Apis.Sheets.v4.Data;
using Object = UnityEngine.Object;

namespace UnityEditor.Localization.Plugins.Google
{
    /// <summary>
    /// Provides an interface for syncing localization data to a Google Sheet.
    /// </summary>
    public class GoogleSheets
    {
        /// <summary>
        /// The sheets provider is responsible for providing the SheetsService and configuring the type of access.
        /// <seealso cref="SheetsServiceProvider"/>.
        /// </summary>
        public IGoogleSheetsService SheetsService { get; private set; }

        /// <summary>
        /// The Id of the Google Sheet. This can be found by examining the url:
        /// https://docs.google.com/spreadsheets/d/<b>SpreadsheetId</b>/edit#gid=<b>SheetId</b>
        /// Further information can be found <see href="https://developers.google.com/sheets/api/guides/concepts#spreadsheet_id">here.</see>
        /// </summary>
        public string SpreadSheetId { get; set;  }

        /// <summary>
        /// Is an API key being used or is it an OAuth?
        /// </summary>
        internal protected virtual bool UsingApiKey => (SheetsService as SheetsServiceProvider)?.Authentication != AuthenticationType.OAuth;

        /// <summary>
        /// Creates a new instance of a GoogleSheets connection.
        /// </summary>
        /// <param name="provider">The Google Sheets service provider. See <see cref="SheetsServiceProvider"/> for a default implementation.</param>
        public GoogleSheets(IGoogleSheetsService provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            SheetsService = provider;
        }

        /// <summary>
        /// Opens the spreadsheet in a browser.
        /// </summary>
        /// <param name="spreadSheetId"></param>
        public static void OpenSheetInBrowser(string spreadSheetId) => Application.OpenURL($"https://docs.google.com/spreadsheets/d/{spreadSheetId}/");

        /// <summary>
        /// Opens the spreadsheet with the sheet selected in a browser.
        /// </summary>
        /// <param name="spreadSheetId"></param>
        /// <param name="sheetId"></param>
        public static void OpenSheetInBrowser(string spreadSheetId, int sheetId) => Application.OpenURL($"https://docs.google.com/spreadsheets/d/{spreadSheetId}/#gid={sheetId}");

        /// <summary>
        /// Creates a new Google Spreadsheet.
        /// </summary>
        /// <param name="spreadSheetTitle">The title of the Spreadsheet.</param>
        /// <param name="sheetTitle">The title of the sheet(tab) that is part of the Spreadsheet.</param>
        /// <param name="newSheetProperties"></param>
        /// <param name="reporter">Optional reporter to display the progress and status of the task.</param>
        /// <returns>Returns the new Spreadsheet and sheet id.</returns>
        public (string spreadSheetId, int sheetId) CreateSpreadsheet(string spreadSheetTitle, string sheetTitle, NewSheetProperties newSheetProperties, ITaskReporter reporter = null)
        {
            if (newSheetProperties == null)
                throw new ArgumentNullException(nameof(newSheetProperties));

            try
            {
                if (reporter != null && reporter.Started != true)
                    reporter.Start("Create Spreadsheet", "Preparing Request");

                var createRequest = SheetsService.Service.Spreadsheets.Create(new Spreadsheet
                {
                    Properties = new SpreadsheetProperties
                    {
                        Title = spreadSheetTitle
                    },
                    Sheets = new Sheet[]
                    {
                        new Sheet
                        {
                            Properties = new SheetProperties
                            {
                                Title = sheetTitle,
                            }
                        }
                    }
                });

                reporter?.ReportProgress("Sending create request", 0.2f);
                var createResponse = ExecuteRequest<Spreadsheet, CreateRequest>(createRequest);
                SpreadSheetId = createResponse.SpreadsheetId;
                var sheetId = createResponse.Sheets[0].Properties.SheetId.Value;

                reporter?.ReportProgress("Setting up new sheet", 0.5f);
                SetupSheet(SpreadSheetId, sheetId, newSheetProperties);

                reporter?.Completed(string.Empty);
                return (SpreadSheetId, sheetId);
            }
            catch (Exception e)
            {
                reporter?.Fail(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates a new sheet within the Spreadsheet with the id <see cref="SpreadSheetId"/>.
        /// </summary>
        /// <param name="title">The title for the new sheet</param>
        /// <param name="newSheetProperties">The settings to apply to the new sheet.</param>
        /// <returns>The new sheet id.</returns>
        public int AddSheet(string title, NewSheetProperties newSheetProperties)
        {
            if (string.IsNullOrEmpty(SpreadSheetId))
                throw new Exception($"{nameof(SpreadSheetId)} is required. Please assign a valid Spreadsheet Id to the property.");

            if (newSheetProperties == null)
                throw new ArgumentNullException(nameof(newSheetProperties));

            var createRequest = new Request()
            {
                AddSheet = new AddSheetRequest
                {
                    Properties = new SheetProperties { Title = title }
                }
            };

            var batchUpdateReqTask = SendBatchUpdateRequest(SpreadSheetId, createRequest);
            var sheetId = batchUpdateReqTask.Replies[0].AddSheet.Properties.SheetId.Value;
            SetupSheet(SpreadSheetId, sheetId, newSheetProperties);
            return sheetId;
        }

        /// <summary>
        /// Returns a list of all the sheets in the Spreadsheet with the id <see cref="SpreadSheetId"/>.
        /// </summary>
        /// <returns>The sheets names and id's.</returns>
        public List<(string name, int id)> GetSheets()
        {
            if (string.IsNullOrEmpty(SpreadSheetId))
                throw new Exception($"The {nameof(SpreadSheetId)} is required. Please assign a valid Spreadsheet Id to the property.");

            var sheets = new List<(string name, int id)>();
            var spreadsheetInfoRequest = SheetsService.Service.Spreadsheets.Get(SpreadSheetId);
            var sheetInfoReq = ExecuteRequest<Spreadsheet, GetRequest>(spreadsheetInfoRequest);

            foreach (var sheet in sheetInfoReq.Sheets)
            {
                sheets.Add((sheet.Properties.Title, sheet.Properties.SheetId.Value));
            }

            return sheets;
        }

        /// <summary>
        /// Returns all the column titles(values from the first row) for the selected sheet inside of the Spreadsheet with id <see cref="SpreadSheetId"/>.
        /// This method requires the <see cref="SheetsService"/> to use OAuth authorization as it uses a data filter which reuires elevated authorization.
        /// </summary>
        /// <param name="sheetId">The sheet id.</param>
        /// <returns>All the </returns>
        public IList<string> GetColumnTitles(int sheetId)
        {
            if (string.IsNullOrEmpty(SpreadSheetId))
                throw new Exception($"{nameof(SpreadSheetId)} is required.");

            var batchGetValuesByDataFilterRequest = new BatchGetValuesByDataFilterRequest
            {
                DataFilters = new DataFilter[1]
                {
                    new DataFilter
                    {
                        GridRange = new GridRange
                        {
                            SheetId = sheetId,
                            StartRowIndex = 0,
                            EndRowIndex = 1
                        }
                    }
                }
            };

            var request = SheetsService.Service.Spreadsheets.Values.BatchGetByDataFilter(batchGetValuesByDataFilterRequest, SpreadSheetId);
            var result = ExecuteRequest<BatchGetValuesByDataFilterResponse, ValuesResource.BatchGetByDataFilterRequest>(request);

            var titles = new List<string>();
            if (result?.ValueRanges?.Count > 0 && result.ValueRanges[0].ValueRange.Values != null)
            {
                foreach (var row in result.ValueRanges[0].ValueRange.Values)
                {
                    foreach (var col in row)
                    {
                        titles.Add(col.ToString());
                    }
                }
            }
            return titles;
        }

        /// <summary>
        /// Asynchronous version of <see cref="GetRowCount"/>
        /// <inheritdoc cref="GetRowCount"/>
        /// </summary>
        /// <param name="sheetId">The sheet to get the row count from</param>
        /// <returns>The row count for the sheet.</returns>
        public async Task<int> GetRowCountAsync(int sheetId)
        {
            var rowCountRequest = GenerateGetRowCountRequest(sheetId);
            var task = ExecuteRequestAsync<Spreadsheet, GetByDataFilterRequest>(rowCountRequest);
            await task.ConfigureAwait(true);

            if (task.Result.Sheets == null || task.Result.Sheets.Count == 0)
                throw new Exception($"No sheet data available for {sheetId} in Spreadsheet {SpreadSheetId}.");
            return task.Result.Sheets[0].Properties.GridProperties.RowCount.Value;
        }

        /// <summary>
        /// Returns the total number of rows in the sheet inside of the Spreadsheet with id <see cref="SpreadSheetId"/>.
        /// This method requires the <see cref="SheetsService"/> to use OAuth authorization as it uses a data filter which reuires elevated authorization.
        /// </summary>
        /// <param name="sheetId">The sheet to get the row count from.</param>
        /// <returns>The row count for the sheet.</returns>
        public int GetRowCount(int sheetId)
        {
            var rowCountRequest = GenerateGetRowCountRequest(sheetId);
            var response = ExecuteRequest<Spreadsheet, GetByDataFilterRequest>(rowCountRequest);

            if (response.Sheets == null || response.Sheets.Count == 0)
                throw new Exception($"No sheet data available for {sheetId} in Spreadsheet {SpreadSheetId}.");
            return response.Sheets[0].Properties.GridProperties.RowCount.Value;
        }

        GetByDataFilterRequest GenerateGetRowCountRequest(int sheetId)
        {
            if (string.IsNullOrEmpty(SpreadSheetId))
                throw new Exception($"{nameof(SpreadSheetId)} is required.");

            return SheetsService.Service.Spreadsheets.GetByDataFilter(new GetSpreadsheetByDataFilterRequest
            {
                DataFilters = new DataFilter[]
                {
                    new DataFilter
                    {
                        GridRange = new GridRange
                        {
                            SheetId = sheetId,
                        },
                    },
                },
            }, SpreadSheetId);
        }

        /// <summary>
        /// Asynchronous version of <see cref="PushStringTableCollection"/>
        /// <inheritdoc cref="PushStringTableCollection"/>
        /// </summary>
        /// <param name="sheetId">The sheet(Spreadsheet tab) to insert the data into.</param>
        /// <param name="collection">The collection to extract the data from.</param>
        /// <param name="columnMapping">The column mappings control what data will be extracted for each column of the sheet. The list must contain 1 <see cref="KeyColumn"/>.</param>
        /// <param name="reporter">Optional reporter to display the progress and status of the task.</param>
        /// <returns></returns>
        public async Task PushStringTableCollectionAsync(int sheetId, StringTableCollection collection, IList<SheetColumn> columnMapping, ITaskReporter reporter = null)
        {
            VerifyPushPullArguments(sheetId, collection, columnMapping, typeof(KeyColumn));

            // Nothing to push
            if (collection.StringTables.Count == 0)
                return;

            try
            {
                if (reporter != null && reporter.Started != true)
                    reporter.Start($"Push `{collection.TableCollectionName}` to Google Sheets", "Checking if sheet needs resizing");

                var requests = new List<Request>();
                var rowCountTask = GetRowCountAsync(sheetId);
                await rowCountTask.ConfigureAwait(true);
                var rowCount = rowCountTask.Result;

                // Do we need to resize the sheet?
                var requiredRows = collection.SharedData.Entries.Count + 1; // + 1 for the header row
                if (collection.SharedData.Entries.Count > rowCount)
                {
                    reporter?.ReportProgress("Generating sheet resize request", 0.15f);
                    requests.Add(ResizeRow(sheetId, requiredRows));
                }

                GeneratePushRequests(sheetId, collection, columnMapping, requests, reporter);

                reporter?.ReportProgress("Sending Request", 0.5f);
                var sendTask = SendBatchUpdateRequestAsync(SpreadSheetId, requests);
                await sendTask.ConfigureAwait(true);
                reporter?.Completed($"Pushed {requiredRows} rows and {requiredRows * columnMapping.Count} cells successfully.");
            }
            catch (Exception e)
            {
                reporter?.Fail(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Extracts data from <paramref name="collection"/> using <paramref name="columnMapping"/> and sends it to the sheet
        /// inside of the Spreadsheet with id <see cref="SpreadSheetId"/>.
        /// This method requires the <see cref="SheetsService"/> to use OAuth authorization as an API Key does not have the ability to write to a sheet.
        /// </summary>
        /// <param name="sheetId">The sheet(Spreadsheet tab) to insert the data into.</param>
        /// <param name="collection">The collection to extract the data from.</param>
        /// <param name="columnMapping">The column mappings control what data will be extracted for each column of the sheet. The list must contain 1 <see cref="KeyColumn"/>.</param>
        /// <param name="reporter">Optional reporter to display the progress and status of the task.</param>
        /// <example>
        /// A <see cref="StringTableCollection"/> can exist over several Google Sheets, for example one per Locale.
        /// This example shows to push one of those Locales.
        /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="push-collection-english"/>
        /// </example>
        /// <example>
        /// This example shows how to push all the locales in your project by using <see cref="ColumnMapping"/> to generate the column mapping data for you.
        /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="push-project-locales"/>
        /// </example>
        /// <example>
        /// This example shows how to use the data that was configured in the <see cref="GoogleSheetsExtension"/> to perform a push.
        /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="push-with-extension"/>
        /// </example>
        /// <example>
        /// This example shows how to push every <see cref="StringTableCollection"/> that contains a <see cref="GoogleSheetsExtension"/>.
        /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="push-all-extensions"/>
        /// </example>
        public void PushStringTableCollection(int sheetId, StringTableCollection collection, IList<SheetColumn> columnMapping, ITaskReporter reporter = null)
        {
            VerifyPushPullArguments(sheetId, collection, columnMapping, typeof(KeyColumn));

            // Nothing to push
            if (collection.StringTables.Count == 0)
                return;

            try
            {
                if (reporter != null && reporter.Started != true)
                    reporter.Start($"Push `{collection.TableCollectionName}` to Google Sheets", "Checking if sheet needs resizing");

                var requests = new List<Request>();
                var rowCount = GetRowCount(sheetId);

                // Do we need to resize the sheet?
                var requiredRows = collection.SharedData.Entries.Count + 1; // + 1 for the header row
                if (collection.SharedData.Entries.Count > rowCount)
                {
                    reporter?.ReportProgress("Generating sheet resize request", 0.15f);
                    requests.Add(ResizeRow(sheetId, requiredRows));
                }

                GeneratePushRequests(sheetId, collection, columnMapping, requests, reporter);

                reporter?.ReportProgress("Sending Request", 0.5f);
                var resp = SendBatchUpdateRequest(SpreadSheetId, requests);
                reporter?.Completed($"Pushed {requiredRows} rows successfully.");
            }
            catch (Exception e)
            {
                reporter?.Fail(e.ToString());
                throw;
            }
        }

        void GeneratePushRequests(int sheetId, StringTableCollection collection, IList<SheetColumn> columnMapping, List<Request> requestsToSend, ITaskReporter reporter)
        {
            // Prepare the column requests.
            // We use a request per column as its possible that some columns in the sheet will be preserved and we don't want to write over them.
            reporter?.ReportProgress("Generating column headers", 0);
            var columnSheetRequests = new List<PushColumnSheetRequest>(columnMapping.Count);
            foreach (var col in columnMapping)
            {
                var colRequest = new PushColumnSheetRequest(sheetId, col);

                columnSheetRequests.Add(colRequest);
                colRequest.Column.PushBegin(collection);
                colRequest.Column.PushHeader(collection, out var header, out var note);
                colRequest.AddHeader(header, note);
            }

            reporter?.ReportProgress("Generating push data", 0.1f);
            foreach (var row in collection.GetRowEnumeratorUnsorted())
            {
                foreach (var colReq in columnSheetRequests)
                {
                    if (row.KeyEntry.Metadata.HasMetadata<ExcludeEntryFromExport>())
                        continue;

                    colReq.Column.PushCellData(row.KeyEntry, row.TableEntries, out var value, out var note);
                    colReq.AddRow(value, note);
                }
            }

            foreach (var col in columnSheetRequests)
            {
                col.Column.PushEnd();
                requestsToSend.AddRange(col.Requests);
            }
        }

        /// <summary>
        /// Pulls data from the Spreadsheet with id <see cref="SpreadSheetId"/> and uses <paramref name="columnMapping"/>
        /// to populate the <paramref name="collection"/>.
        /// </summary>
        /// <param name="sheetId">The sheet(Spreadsheet tab) to pull the data from.</param>
        /// <param name="collection">The collection to insert the data into.</param>
        /// <param name="columnMapping">The column mappings control what data to extract for each column of the sheet. The list must contain one <see cref="IPullKeyColumn"/>.</param>
        /// <param name="removeMissingEntries">After a pull has completed, any keys that exist in the <paramref name="collection"/> but did not exist in the sheet are considered missing,
        /// this may be because they have been deleted from the sheet. A value of true will remove these missing entries; false will preserve them.</param>
        /// <param name="reporter">Optional reporter to display the progress and status of the task.</param>
        /// <param name="createUndo">Should an Undo be recorded so any changes can be reverted?</param>
        /// <example>
        /// A <see cref="StringTableCollection"/> can exist over several Google Sheets, for example one per Locale.
        /// This example shows how to pull one of those Locales into a <see cref="StringTableCollection"/>.
        /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="pull-collection-english"/>
        /// </example>
        /// <example>
        /// This example shows how to pull all the locales in your project by using the <see cref="ColumnMapping"/> to generate the column mapping data for you.
        /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="pull-project-locales"/>
        /// </example>
        /// <example>
        /// This example shows how to use the data that was configured in a Google Sheets extension to perform a pull.
        /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="pull-with-extension"/>
        /// </example>
        /// <example>
        /// This example shows how to pull every <see cref="StringTableCollection"/> that contains a <see cref="GoogleSheetsExtension"/>.
        /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="pull-all-extensions"/>
        /// </example>
        public void PullIntoStringTableCollection(int sheetId, StringTableCollection collection, IList<SheetColumn> columnMapping, bool removeMissingEntries = false, ITaskReporter reporter = null, bool createUndo = false)
        {
            VerifyPushPullArguments(sheetId, collection, columnMapping, typeof(IPullKeyColumn));

            try
            {
                var modifiedAssets = collection.StringTables.Select(t => t as Object).ToList();
                modifiedAssets.Add(collection.SharedData);

                if (createUndo)
                {
                    Undo.RegisterCompleteObjectUndo(modifiedAssets.ToArray(), $"Pull `{collection.TableCollectionName}` from Google sheets");
                }

                if (reporter != null && reporter.Started != true)
                    reporter.Start($"Pull `{collection.TableCollectionName}` from Google sheets", "Preparing columns");

                // The response columns will be in the same order we request them, we need the key
                // before we can process any values so ensure the first column is the key column.
                var sortedColumns = columnMapping.OrderByDescending(c => c is IPullKeyColumn).ToList();

                // We can only use public API. No data filters.
                // We use a data filter when possible as it allows us to remove a lot of unnecessary information,
                // such as unneeded sheets and columns, which reduces the size of the response. A Data filter can only be used with OAuth authentication.
                reporter?.ReportProgress("Generating request", 0.1f);
                ClientServiceRequest<Spreadsheet> pullReq = UsingApiKey ? GeneratePullRequest() : GenerateFilteredPullRequest(sheetId, columnMapping);

                reporter?.ReportProgress("Sending request", 0.2f);
                var response = ExecuteRequest<Spreadsheet, ClientServiceRequest<Spreadsheet>>(pullReq);

                reporter?.ReportProgress("Validating response", 0.5f);

                // When using an API key we get all the sheets so we need to extract the one we are pulling from.
                var sheet = UsingApiKey ? response.Sheets?.FirstOrDefault(s => s?.Properties?.SheetId == sheetId) : response.Sheets[0];
                if (sheet == null)
                    throw new Exception($"No sheet data available for {sheetId} in Spreadsheet {SpreadSheetId}.");

                // The data will be structured differently if we used a filter or not so we need to extract the parts we need.
                var pulledColumns = new List<(IList<RowData> rowData, int valueIndex)>();

                if (UsingApiKey)
                {
                    // When getting the whole sheet all the columns are stored in a single Data. We need to extract the correct value index for each column.
                    foreach (var sortedCol in sortedColumns)
                    {
                        pulledColumns.Add((sheet.Data[0].RowData, sortedCol.ColumnIndex));
                    }
                }
                else
                {
                    if (sheet.Data.Count != columnMapping.Count)
                        throw new Exception($"Column mismatch. Expected a response with {columnMapping.Count} columns but only got {sheet.Data.Count}");

                    // When using a filter each Data represents a single column.
                    foreach (var d in sheet.Data)
                    {
                        pulledColumns.Add((d.RowData, 0));
                    }
                }

                MergePull(pulledColumns, collection, columnMapping, UsingApiKey, removeMissingEntries, reporter);

                // There is a bug that causes Undo to not set assets dirty (case 1240528) so we always set the asset dirty.
                modifiedAssets.ForEach(EditorUtility.SetDirty);

                LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(this, collection);

                // Flush changes to disk.
                collection.SaveChangesToDisk();
            }
            catch (Exception e)
            {
                reporter?.Fail(e.Message);
                throw;
            }
        }

        void VerifyPushPullArguments(int sheetId, StringTableCollection collection, IList<SheetColumn> columnMapping, Type requiredKeyType)
        {
            if (string.IsNullOrEmpty(SpreadSheetId))
                throw new Exception($"{nameof(SpreadSheetId)} is required.");

            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (columnMapping == null)
                throw new ArgumentNullException(nameof(columnMapping));

            if (columnMapping.Count == 0)
                throw new ArgumentException("Must include at least 1 column.", nameof(columnMapping));

            if (columnMapping.Count(c => requiredKeyType.IsAssignableFrom(c.GetType())) != 1)
                throw new ArgumentException($"Must include 1 {requiredKeyType.Name}.", nameof(columnMapping));

            ThrowIfDuplicateColumnIds(columnMapping);
        }

        ClientServiceRequest<Spreadsheet> GeneratePullRequest()
        {
            var request = SheetsService.Service.Spreadsheets.Get(SpreadSheetId);
            request.IncludeGridData = true;
            request.Fields = "sheets.properties.sheetId,sheets.properties.gridProperties.rowCount,sheets.data.rowData.values.formattedValue,sheets.data.rowData.values.note";
            return request;
        }

        ClientServiceRequest<Spreadsheet> GenerateFilteredPullRequest(int sheetId, IList<SheetColumn> columnMapping)
        {
            var getRequest = new GetSpreadsheetByDataFilterRequest { DataFilters = new List<DataFilter>() };

            foreach (var col in columnMapping)
            {
                getRequest.DataFilters.Add(new DataFilter
                {
                    GridRange = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = 1, // Ignore header
                        StartColumnIndex = col.ColumnIndex,
                        EndColumnIndex = col.ColumnIndex + 1
                    }
                });
            }

            var request = SheetsService.Service.Spreadsheets.GetByDataFilter(getRequest, SpreadSheetId);
            request.Fields = "sheets.properties.gridProperties.rowCount,sheets.data.rowData.values.formattedValue,sheets.data.rowData.values.note";
            return request;
        }

        void MergePull(List<(IList<RowData> rowData, int valueIndex)> columns, StringTableCollection collection, IList<SheetColumn> columnMapping, bool skipFirstRow, bool removeMissingEntries, ITaskReporter reporter)
        {
            reporter?.ReportProgress("Preparing to merge", 0.55f);

            // Keep track of any issues for a single report instead of filling the console.
            var messages = new StringBuilder();

            var keyColumn = columnMapping[0] as IPullKeyColumn;
            Debug.Assert(keyColumn != null, "Expected the first column to be a Key column");

            var rowCount = columns[0].rowData != null ? columns[0].rowData.Count : 0;

            // Send the start message
            foreach (var col in columnMapping)
            {
                col.PullBegin(collection);
            }

            reporter?.ReportProgress("Merging response into collection", 0.6f);
            var keysProcessed = new HashSet<long>();

            // We want to keep track of the order the entries are pulled in so we can match it
            var sortedEntries = new List<SharedTableEntry>(rowCount);
            var addedIds = new Dictionary<long, int>(); // So we dont add duplicates. (id,row)
            var addedKeys = new Dictionary<string, int>(); // So we dont add duplicate names. (name, row)

            long totalCellsProcessed = 0;

            var keyValueIndex = columns[0].valueIndex;
            for (int row = skipFirstRow ? 1 : 0; row < rowCount; row++)
            {
                var keyRowData = columns[0].rowData[row];
                var keyData = keyRowData?.Values ? [keyValueIndex];
                var keyValue = keyData?.FormattedValue;
                var keyNote = keyData?.Note;

                // Skip rows with no key data
                if (string.IsNullOrEmpty(keyValue) && string.IsNullOrEmpty(keyNote))
                    continue;

                var rowKeyEntry = keyColumn.PullKey(keyValue, keyNote);

                // Ignore duplicate ids (LOC-464)
                if (addedIds.TryGetValue(rowKeyEntry.Id, out int duplicateRowId))
                {
                    messages.AppendLine($"An entry with the Id {rowKeyEntry.Id} has already been processed at row {duplicateRowId}, The entry {keyValue} at row {row} will be ignored.");
                    continue;
                }

                // Rename duplicate names with unique ids.
                if (addedKeys.TryGetValue(rowKeyEntry.Key, out int duplicateRowKey))
                {
                    string newName = $"{rowKeyEntry.Key}_{rowKeyEntry.Id}";
                    messages.AppendLine($"An entry with the name `{rowKeyEntry.Key}` has already been processed at row {duplicateRowKey}, The entry {keyValue} at row {row} has been renamed to {newName}.");
                    rowKeyEntry.Key = newName;
                }

                addedIds.Add(rowKeyEntry.Id, row);
                addedKeys.Add(rowKeyEntry.Key, row);
                sortedEntries.Add(rowKeyEntry);

                if (rowKeyEntry == null)
                {
                    messages.AppendLine($"No key data was found for row {row} with Value '{keyValue}' and Note '{keyNote}'.");
                    continue;
                }

                // Record the id so we can check what key ids were missing later.
                keysProcessed.Add(rowKeyEntry.Id);
                totalCellsProcessed++;

                for (int col = 1; col < columnMapping.Count; ++col)
                {
                    string value = null;
                    string note = null;

                    var colRowData = columns[col].rowData;
                    var valueIndex = columns[col].valueIndex;

                    // Do we have data in this column for this row?
                    if (colRowData != null && colRowData.Count > row && colRowData[row]?.Values?.Count > valueIndex)
                    {
                        var cellData = colRowData[row].Values[valueIndex];
                        if (cellData != null)
                        {
                            value = cellData.FormattedValue;
                            note = cellData.Note;
                            totalCellsProcessed++;
                        }
                    }

                    // We always call PullCellData as its possible that data may have existed
                    // in a previous Pull and has now been removed. We call Pull so that the column
                    // is aware it is now null and can remove any metadata it may have added in the past. (LOC-134)
                    columnMapping[col].PullCellData(rowKeyEntry, value, note);
                }
            }

            // Send the end message
            foreach (var col in columnMapping)
            {
                col.PullEnd();
            }

            reporter?.ReportProgress("Removing missing entries and matching sheet row order", 0.9f);
            collection.MergeUpdatedEntries(keysProcessed, sortedEntries, messages, removeMissingEntries);
            reporter?.Completed($"Completed merge of {rowCount} rows and {totalCellsProcessed} cells from {columnMapping.Count} columns successfully.\n{messages.ToString()}");
        }

        void ThrowIfDuplicateColumnIds(IList<SheetColumn> columnMapping)
        {
            var ids = new HashSet<string>();
            foreach (var col in columnMapping)
            {
                if (ids.Contains(col.Column))
                    throw new Exception($"Duplicate column found. The Column {col.Column} is already in use");
                ids.Add(col.Column);
            }
        }

        void SetupSheet(string spreadSheetId, int sheetId, NewSheetProperties newSheetProperties)
        {
            var requests = new List<Request>();

            requests.Add(SetTitleStyle(sheetId, newSheetProperties));

            if (newSheetProperties.FreezeTitleRowAndKeyColumn)
                requests.Add(FreezeTitleRowAndKeyColumn(sheetId));

            if (newSheetProperties.HighlightDuplicateKeys)
                requests.Add(HighlightDuplicateKeys(sheetId, newSheetProperties));

            if (requests.Count > 0)
                SendBatchUpdateRequest(spreadSheetId, requests);
        }

        Request FreezeTitleRowAndKeyColumn(int sheetId)
        {
            return new Request()
            {
                UpdateSheetProperties = new UpdateSheetPropertiesRequest
                {
                    Fields = "GridProperties.FrozenRowCount,GridProperties.FrozenColumnCount,",
                    Properties = new SheetProperties
                    {
                        SheetId = sheetId,
                        GridProperties = new GridProperties
                        {
                            FrozenRowCount = 1,
                            FrozenColumnCount = 1
                        }
                    }
                }
            };
        }

        Request HighlightDuplicateKeys(int sheetId, NewSheetProperties newSheetProperties)
        {
            return new Request
            {
                // Highlight duplicates in the A(Key) field
                AddConditionalFormatRule = new AddConditionalFormatRuleRequest
                {
                    Rule = new ConditionalFormatRule
                    {
                        BooleanRule = new BooleanRule
                        {
                            Condition = new BooleanCondition
                            {
                                Type = "CUSTOM_FORMULA",
                                Values = new[] { new ConditionValue { UserEnteredValue = "=countif(A:A;A1)>1" } }
                            },
                            Format = new CellFormat { BackgroundColor = UnityColorToDataColor(newSheetProperties.DuplicateKeyColor) }
                        },
                        Ranges = new[]
                        {
                            new GridRange
                            {
                                SheetId = sheetId,
                                EndColumnIndex = 1
                            }
                        }
                    }
                },
            };
        }

        Request SetTitleStyle(int sheetId, NewSheetProperties newSheetProperties)
        {
            return new Request
            {
                // Header style
                RepeatCell = new RepeatCellRequest
                {
                    Fields = "*",
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = 0,
                        EndRowIndex = 1,
                    },
                    Cell = new CellData
                    {
                        UserEnteredFormat = new CellFormat
                        {
                            BackgroundColor = UnityColorToDataColor(newSheetProperties.HeaderBackgroundColor),
                            TextFormat = new TextFormat
                            {
                                Bold = true,
                                ForegroundColor = UnityColorToDataColor(newSheetProperties.HeaderForegroundColor)
                            }
                        }
                    }
                }
            };
        }

        Request ResizeRow(int sheetId, int newSize)
        {
            return new Request
            {
                UpdateSheetProperties = new UpdateSheetPropertiesRequest
                {
                    Properties = new SheetProperties
                    {
                        SheetId = sheetId,
                        GridProperties = new GridProperties
                        {
                            RowCount = newSize
                        },
                    },
                    Fields = "gridProperties.rowCount"
                }
            };
        }

        static Data.Color UnityColorToDataColor(UnityEngine.Color color) => new Data.Color() { Red = color.r, Green = color.g, Blue = color.b, Alpha = color.a };

        internal protected virtual Task<BatchUpdateSpreadsheetResponse> SendBatchUpdateRequestAsync(string spreadsheetId, IList<Request> requests)
        {
            var service = SheetsService.Service;
            var requestBody =  new BatchUpdateSpreadsheetRequest { Requests = requests };
            var batchUpdateReq = service.Spreadsheets.BatchUpdate(requestBody, spreadsheetId);
            return batchUpdateReq.ExecuteAsync();
        }

        internal protected virtual BatchUpdateSpreadsheetResponse SendBatchUpdateRequest(string spreadsheetId, IList<Request> requests)
        {
            var service = SheetsService.Service;
            var requestBody =  new BatchUpdateSpreadsheetRequest { Requests = requests };
            var batchUpdateReq = service.Spreadsheets.BatchUpdate(requestBody, spreadsheetId);
            return batchUpdateReq.Execute();
        }

        internal protected virtual BatchUpdateSpreadsheetResponse SendBatchUpdateRequest(string spreadsheetId, params Request[] requests)
        {
            var service = SheetsService.Service;
            var requestBody =  new BatchUpdateSpreadsheetRequest { Requests = requests };
            var batchUpdateReq = service.Spreadsheets.BatchUpdate(requestBody, spreadsheetId);
            return batchUpdateReq.Execute();
        }

        internal protected virtual Task<TResponse> ExecuteRequestAsync<TResponse, TClientServiceRequest>(TClientServiceRequest req) where TClientServiceRequest : ClientServiceRequest<TResponse> => req.ExecuteAsync();
        internal protected virtual TResponse ExecuteRequest<TResponse, TClientServiceRequest>(TClientServiceRequest req) where TClientServiceRequest : ClientServiceRequest<TResponse> => req.Execute();
    }
}
