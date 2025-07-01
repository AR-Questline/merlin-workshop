# Search filters

The Localization package supports [Unity Search](https://docs.unity3d.com/2021.1/Documentation/Manual/search-overview.html) when picking Localized String and Asset references.
In Unity Editor 2022.1, the Localization package also supports the Visual Query Builder and includes several custom visual query blocks to assist in searching.

The Search picker is enabled by default and can also be toggled on/off through the Localization Preferences window (menu: **Edit > Preferences > Localization**).

There are several optional filters you can use to aid with searching.

When searching for a localized string reference, the search is performed against the following fields:

- Table collection name.
- Entry Key.
- The localized value from each table entry.

When searching for a localized asset reference the search is performed against the following fields:

- Table collection name.
- Entry Key.
- The asset Guid and file name from each table entry.

## Filters

You can use the following filters when searching for localized string and asset references.

| **Filter Name** | **Filter Tag** | **Description** | **Example** |
| --------------- | -------------- | ----------------| ------------|
| **Collection Name** | col | Queries the table collection name for each entry. | `col:game`<br>This finds all entries in a table collection with a fuzzy match to **Game**, such as "**Game** UI" or "My **Game** Text".<br><br>`col="Game UI"`<br>This finds all entries that are in the table "Game UI".*<br><br>*Note that when a term includes a space it should be placed inside of comments. |
| **Collection Group** | grp | Queries the table collection group for each entry. | `grp:UI`<br>This finds all entries in table collections which have a group with a fuzzy match to **UI** such as "Game **UI**" or "Level1/**UI**/Text". |
| **Key Name** | key | The key name filter is used to query against the name value of each entry. | `key:start`<br>Finds all entries that have a fuzzy match to **start**. Such as "**Start** Game". |
| **Key Id** | id | The Key filter is used to query against the id value of each key. | `id:123`<br>Finds all entries that have a fuzzy match to 123. Such as **123**0034 or 0032**123**45.<br><br>`id=123`<br>Find the entry with the id 123.<br><br>`id>123`<br>Find all entries that have an id value greater than 123.<br><br>`id>=123`<br>Find all entries that have an id value greater than or equal to 123.<br><br>`id>123 AND id<200`<br>Find all entries that have an id between 123 and 200. |
| **Translated Value** | tr<br>tr(locale) | Queries against the value in each String/Asset table or a specific table when using the optional locale filter argument. When searching asset tables this queries against both the asset Guid and file name. | **String Tables:**<br><br>`tr:"start game"`<br>Find all entries that have a fuzzy match to "start game".<br><br>`tr(en):"start game"`<br>Find all entries that have a fuzzy match for English to "start game"<br><br>`tr=""`<br>Find all entries that have an empty translated field.<br><br>`tr(ja)=""`<br>Find all entries that have a Japanese field with no value.<br><br>`col=menu NOT tr=""`<br>Find all entries in the table collection menu that do not have any empty translated values.<br><br>**Asset Tables:**<br><br>`tr(fr):".png"`<br>Find all png file assets.<br><br>`tr="My Texture.png"`<br>Find all entries that reference the exact file name. |
| **Metadata Type** | mt | Filters by metadata type. The metadata type that is attached to an entry or its shard table data is considered. The considered type can be an exact match or a close match using a fuzzy search. | `mt:comment`<br>Find all entries that have a metadata type with a fuzzy match to comment.<br><br>`mt=Comment`<br>Find all entries that have a metadata type with an exact match to the name Comment.<br><br>`mt:smart`<br>Find all entries that are marked as smart strings.<br><br>`mt=ItemGender`<br>Find all entries that have the ItemGender metadata. |
| **Metadata Value** | mv<br>mv(property) | Filters by metadata values. When using the filter tag without an argument the ToString method extracts the value from the metadata. You can also provide a property or field name as an argument. Reflection is then used to extract the value. | `mv:"untranslated"`<br>Find all entries that have a fuzzy ToString match to untranslated.<br><br>`mv(translationStatus)=Untranslated`<br>Find all metadata that has a field or property called **translationStatus** that has a value of Untranslated. |

## Table view columns

By default the Localized Asset and String picker shows  the following columns in table view:

| **Column**      | **Description**                     |
| --------------- | ----------------------------------- |
| **Label**       | Displays the Entry Key.             |
| **Description** | Displays the Table Collection Name. |

The Localization package also provides the following columns which you can choose to display in the table view:

- **Collection Name**
- **Collection Group**
- **Entry Key**
- **Entry Key Id**
- **Localized Text / Localized Asset per Locale**

To configure the table view to display additional columns, right-click the column header and choose **Add Column...**

## Scripted searching

You can use the Search Tool through script with the String and Asset Table filters. 
This can be used to write automation tools to quickly search for entries, for example to find all entries that have not yet been translated.

The following example shows various ways to search assets and string tables:

[!code-cs[search-samples](../DocCodeSamples.Tests/SearchSamples.cs)]
