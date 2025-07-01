# Syncing String Table Collections

For each String Table Collection you want to connect to a Google Sheet, you need to add a Google Sheet extension to the String Table Collection’s **Extensions list**. To do this, click the **Add(+)** button in the **Extensions** field. It is possible to add multiple extensions to a String Table Collection (for example, you might want to have a different sheet for each locale, and therefore a Google Sheet Extension for each sheet).

To sync a String Table to a Google Sheet, you need to connect it to a Sheets Service Provider asset. See [Sheets Service Provider](Google-Sheets-Sheets-Service-Provider.md) for information on creating and configuring one.

![String Table Collection - Google Sheets Extension.](images/GoogleExtension_Inspector.png)

| **Property**                   | **Description**                                                                                                                                                                                                         |
|--------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Sheets Service Provider**    | Assign the [Sheets Service Provider](Google-Sheets-Sheets-Service-Provider.md) Asset you want to use.                                                                                                                   |
| **Create New Spreadsheet**     | Select this button to create a new spreadsheet for String Table data. Use this if a Google Sheet for this String Table does not already exist.                                                                          |
| **Spreadsheet Id**             | Enter the Spreadsheet ID from your Google Spreadsheet. In the Spreadsheet’s Google URL, this is in the middle of the URL:<br><br>https://<span>docs.google.</span>com/spreadsheets/d/**spreadsheetId**/edit#gid=sheetId |
| **Sheet Id**                   | Enter the Sheet ID from your Google Spreadsheet. In the Sheet’s Google URL, this at the end of the URL:<br><br>https://<span>docs.google.</span>com/spreadsheets/d/spreadsheetId/edit#gid=**sheetId**                   |
| **Add Sheet**                  | Create a new sheet based on the properties defined in the associated Sheets Service Provider’s New Sheet Properties.                                                                                                    |
| **Select Sheet**               | Select a sheet from the Google Spreadsheet.                                                                                                                                                                             |
| **Mapped Columns**             | Assign specific types of data to specific columns. See **Mapped Columns**, below, for details.                                                                                                                          |
| **Remove Missing Pulled Keys** | Remove any keys that are not present in a sheet after a pull.                                                                                                                                                           |
| **Push Selected**              | Push selected mapped columns to the Google sheet.                                                                                                                                                                       |
| **Pull Selected**              | Pull selected mapped columns from the Google sheet.                                                                                                                                                                     |
| **Push**                       | Push all mapped columns to the Google sheet.                                                                                                                                                                            |
| **Pull**                       | Pull all mapped columns from the Google sheet.                                                                                                                                                                          |

## Mapped Columns

Use the **Mapped Columns** field to configure how your project pushes and pulls data from the spreadsheet. A mapped column can read and write to a single column's value and note property.

To add a new column mapping, select the **Add(+)** button.

![Mapped Columns Add Menu.](images/GoogleExtension_MappedColumns.png)

| **Option**                        | **Description** |
| ----------------------------------| --------------- |
| **Key Column**                   | A **Key Column** includes the Key name in the value field and the Key Id in the note field. This means that it is possible to rename a key and even assign a custom Id value.<br><br>![Key Column Inspector.](images/GoogleExtension_KeyColumn.png)
| **Locale Column**                | The **Locale Column** includes the values from a single String Table for the selected Locale. If **Include Comments** is enabled, any comment metadata is included in the Sheet as notes.<br><br>![Locale Column Inspector.](images/GoogleExtension_LocaleColumn.png)
| **Locale Comment Column**        | The **Locale Comment Column** includes comment metadata as values.<br><br>![Locale Comment Column Inspector.](images/GoogleExtension_LocaleCommentColumn.png)
| **Add Default Columns**          | This adds a **Key Column** and a **Locale Column** for each Locale in the project.
| **Extract Columns From Sheet**   | If the Spreadsheet already contains locale data, this option reads the column titles and creates column mappings. This option requires [OAuth authorization](Google-Sheets-Sheets-Service-Provider.md#oauth-authentication).<br><br>It recognizes the following names:<br><dl><dt>Key Column</dt><dd>- “Key”</dd><dd>- “Keys”</dd><dt>Locale Column</dt><dd>- Locale asset name</dd><dd>- Locale identifier ToString</dd><dd>- Locale identifier code</dd><dt>Locale Comment Column</dt><dd>- The same as the Locale Column appended by “Comments”.</dd></dl>

> [!NOTE]
> The Mapped Columns must include one Key Column or derived Key Column. Unity needs this key so that it can associate the sheet rows to the corresponding keys in the String Table Collection.

### Example Google Sheet

![An example of a Google sheet with populated localization data.](images/GoogleSheet_Annotations.png)

**A**: The **Key column** includes the unique Key for each entry. <br/>
**B**: The **Locale columns**. Each Locale has a separate column for its localized values. <br/>
**C**: The **Key Id** is stored in the **note field** of each Key cell. When adding a new entry this field is empty and is populated when the next sync occurs. <br/>

> [!TIP]
> You can assign an ID value to a new entry instead of using the auto-generated ID. To do this, right-click the field and select **insert note**. Custom IDs should use negative values to prevent future conflicts, see [Table Keys](TableEntryKeys.md) for further information.

**D**: Duplicate keys are highlighted to indicate a possible issue. You should either combine or rename duplicates to prevent possible conflicts. <br/>

## Custom columns

It is possible to add support for pushing and pulling custom data.

To create a custom column, you need to inherit from the abstract class [SheetColumn](xref:UnityEditor.Localization.Plugins.Google.Columns.SheetColumn). A common use for a custom column is to support additional metadata. In this case, you can use the abstract classes [KeyMetadataColumn](xref:UnityEditor.Localization.Plugins.Google.Columns.KeyMetadataColumn`1) and [LocaleMetadataColumn](xref:UnityEditor.Localization.Plugins.Google.Columns.LocaleMetadataColumn`1) to simplify the process.

The following example demonstrates how to populate custom metadata into a column.

[!code-cs[google-custom-columns](../DocCodeSamples.Tests/GoogleSheetsSamples.cs#locale-metadata-column)]

## Synchronizing

The **Push** option sends all data from the String Table Collection to the chosen sheet in the Google Sheet. **Push Selected** does the same, but it only sends the selected column and the **Key** column.

The **Pull** option downloads the mapped columns and updates the String Table Collection. Pull Selected does the same as **Pull**, but only for the selected mapped column and the **Key** column.

If **Remove Missing Pulled Keys** is enabled, any keys that are not in the Spreadsheet during a Pull are removed from the String Table Collection.

The script API [GoogleSheets](xref:UnityEditor.Localization.Plugins.Google.GoogleSheets) allows you to **Push** and **Pull** from custom code.
