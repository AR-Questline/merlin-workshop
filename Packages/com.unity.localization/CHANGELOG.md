# Changelog

All notable changes to this package will be documented in this file.

## [1.5.3] - 2024-09-30

### Added

- Added additional menu options to Tables Window to expose the CSV Extension(s) available for a collection. This enables you to import and export data using custom settings configured through the CSV Extensions on the collection. (LOC-1130)
- Added 'Open(Merge)` and `Import(Merge)' options to the CSV Extension property drawer. (LOC-1130)

### Changed

- Upgraded to Addressables 1.22.2. (LOC-1125)

### Fixed

- Added "None" option to localized string and asset picker. ([LOC-1141](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1141))
- Added support for tracking UGUI Layout Groups via property variants so the layout updates when a locale is changed. This will require re-adding the GameObjectLocalizer as we do not currently have a way to update existing trackers to the new type. ([LOC-1128](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1128))
- Fixed `LocalizationSettings.ProjectLocale` being null when the available locales are not initialized. ([LOC-1139](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1139))
- Fixed `MetadataType.All` not including `AllSharedTableEntries` and `LocalizationSettings`. ([LOC-1131](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1131))
- Fixed `StackOverflowException` when two locales use each other as fallbacks and Preload Behavior is set to Preload Selected Locale And Fallbacks. ([LOC-1119](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1119))
- Fixed an issue where we would parse strings that were not marked as smart. ([LOC-1156](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1156))
- Fixed being able to create cyclic locale fallbacks through the editor. (LOC-1117)
- Fixed TrackedTransform so it correctly applies the locale position and rotation to a tracked GameObject. ([LOC-1148](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1148))

## [1.5.2] - 2024-05-14

### Fixed

- Exporting to an existing CSV file will now replace it instead of over-writing the contents. (LOC-1105). ([LOC-1105](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1105))
- Fixed an issue when comparing locales in the player. We now do a check against the locale contents as well as the reference. ([LOC-1098](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1096))
- Fixed an issue where a table handle could become invalid and still be used. We now reload when this occurs. ([LOC-1098](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1096))
- Removed initialization during preloaded assets phase. This was causing freezing issues when trying to load bundles before any scenes had been loaded. ([ADDR-3664](https://issuetracker.unity3d.com/product/unity/issues/guid/ADDR-3664))
- Removed the ability to add a folder to an asset table. This is unsupported. ([LOC-1107](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1107))
- Resolved issues with renaming entries when pulling data from a Google Sheet, particularly in cases where the ID between entries had been swapped. ([LOC-909](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-909))

## [1.5.1] - 2024-03-19

### Changed

- Release version of 1.5.0. Contains all preview changes.

## [1.5.0-pre.7] - 2024-03-13

### Added

- Added `AbortFormattingException`, this can be used to stop processing of the current format item in a Smart String when thrown from a custom `ISource`. ([LOC-1087](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1087))

### Fixed

- Exported XLIFF 1.2 documents will now include the required attribute datatype on the file elements. (LOC-1091)
- Fixed errors in smart strings that contain nested localized strings with additional selectors/formatters when the string table had not yet loaded. ([LOC-1087](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1087))
- Fixed the GameObjectLocalizer not updating if a persistent variable is changed. ([LOC-964](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-964))
- Improved error handling when trying to parse an invalid localized property variant value. ([LOC-1081](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1081))
- Improved performance when removing an item from the Localization Tables Window. ([LOC-1042](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1042))

## [1.5.0-pre.6] - 2024-01-31

### Changed

- CSV files will now be exported in UTF8 without the unesessesary byte order mark (BOM).
- Improved string and asset picker in 2022.2+. The search provider will now start in visual query builder mode and the Inspector panel is now disabled, it was not used.
- Removed the Locale Source option from the **Locale Generator** window.
- Renamed **Locale Generator** to **Add Locale** in Localization Settings.

### Fixed

- A locale change event is no longer sent in playmode when a change is detected to the localization tables. This is now only done in edit mode when previewing a locale via the localization scene controls. ([LOC-1025](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1025))
- Fixed incorrect calculation of string table row height in the Table window. ([LOC-1058](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1058))
- Improved handling of dependency operations that may be released after execution. The localization initialization operation will no longer fail during WaitForCompletion when the addressable initialization operation is automatically released. ([LOC-1039](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1039))
- Improved null handling of `TrackedProperties` values when something went wrong during serialization.
- We now fail the initialization operation if one of the sub-operations fails. ([LOC-1038](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1038))

## [1.5.0-pre.5] - 2023-12-04

### Fixed

- Fixed an issue with UXML attribute converters not being included in 2023.3.

## [1.5.0-pre.4] - 2023-12-01

### Changed

- Removed use of new contextual menu API call `ClearHeaderItems`. This will be removed in 2023.3.

## [1.5.0-pre.3] - 2023-11-30

### Fixed

- Fixed an issue with version defines that was failing to detect 2023.3.

## [1.5.0-pre.2] - 2023-11-27

### Fixed

- Added back in removed API for `ProjectLocalePopupField` and `LocalizationTablesWindow` to avoid making breaking changes to API.

## [1.5.0-pre.1] - 2023-11-21

### Added

- Added `AsynchronousBehavior` to string and asset database. These can be used to set a global `WaitForCompletion` flag instead of having to set it on every `LocalizedAsset` and `LocalizedString`.
- Added `LocalizationImportMenuAttribute`, `LocalizationExportMenuAttribute` and `LocalizationEntryMenuAttribute`. These can be used to add custom menu items to the Localization Tables window.
- Added support for UI Toolkit data bindings. Localized strings and assets can now be added as data bindings to UI Toolkit elements. See docs for more details.

### Changed

- Improved performance of localization object pools and releasing.
- The Localization Tables window has had several UX improvements made. (LOC-214)

### Deprecated

- `ProjectLocalePopupField` and `XliffVersionPopup` `UxmlFactory` and `UxmlTraits` are now obsolete when using Unity 2023.2 and above.

### Fixed

- Added `IDisposable` support to property drawers. This will cleanup resources and reduce the subscribers to editor events helping performance when entering playmode. Note, this also requires the Editor changes that add `IDisposable` support to property drawers.
- Fixed `GetAllTables` `WaitForCompletion` never finishing when run outside of playmode. ([LOC-1011](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1011))
- Fixed `LocalizationSettings` not initializing in the player until the first request was made. ([LOC-975](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-975))
- Fixed `NullReferenceException` when calling GetAllTables outside of playmode and no editor locale was selected. ([LOC-1011](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1011))
- Fixed exception being thrown from `WaitForCurrentOperationAsyncOperationBase ` when the dependency became invalid after `WaitForCompletion` was called on it. We now fail the operation instead. ([LOC-992](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-992))
- Fixed the TableReference field losing its value when `OnAfterDeserialize` was called multiple times.
Fixed the layout of the localized reference field dropdown and buttons. ([LOC-985](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-985))
- Initialization operation will now wait for any asynchronous bundle unloading to complete before beginning initialization. This fixes the error "Table does not have a SharedTableData". ([LOC-1021](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-1021))

## [1.4.5] - 2023-09-20

### Fixed

- Added handling of null `StringReference`/`AssetReference` reference to `LocalizedAssetBehaviour` and `LocalizedStringEvent`. ([LOC-945](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-942))
- Fixed "Objects are trying to be loaded during a domain backup" error occurring on empty `TableReferences` during serialization. ([LOC-969](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-969))
- Fixed `StringTableEntry.IsSmart` from not detecting if the entry was marked smart when its local `SmartFormatTag` metadata was missing but it was still registered in the `SmartFormatTag`. ([LOC-966](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-966))
- Fixed an issue that would cause sprites/textures to flicker when the locale was changed. This was due to us unloading the previous locale's assets before the new ones were available. The `LocalizedAsset` now holds onto the asset handle until its new one has finished loading. ([LOC-976](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-976))
- Fixed compilation errors when using Addressables 2.0.0.
- Fixed Create Table window controls being hidden when a large number of locales were in the project,. (LOC-913)

## [1.4.4] - 2023-06-15

### Changed

- Added support for TMP now being part of the UGUI package in 2023.2.
- Updated obsolete Unity Search API `QueryEngine.Parse` to now use `QueryEngine.ParseQuery` on Unity 2022.2+.
- Updated to Addressables `1.21.8`. (LOC-704)
- Updated to use the Editor XCODE public API when using 2023.1 and above.

### Fixed

- Added multi-line edit support to `name` field in `LocalizedString` and `LocalizedAsset` property drawers. (LOC-875)
- Fixed an error in the device simulator when the project contained no locales. (LOC-888)
- Fixed device simulator Localization menu not showing in 2021.1 and above.
- Fixed preload locale operation not being released when it was completed instantly.
- Fixed tables returned by `GetAllTables` sometimes being released and destroyed. ([LOC-882](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-882))
- Fixed the "Expected to have a current operation to wait on" error when initializing localization before Addressables initialization during a WaitForCompletion call. ([LOC-929](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-929))
- Fixed the **Google Sheet Service Provider** - **Application Name** losing its value after asset creation. ([LOC-864](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-864))
- Improved WaitForCompletion code so that it can handle failed dependencies and won't get stuck in an infinite loop. ([LOC-929](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-929))
- Reenabled code that ensures localization processes assets after Addressables. This was disabled in 1.3.1. (LOC-704)
- Removing an entry from a table will now update the shared metadata. ([LOC-870](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-870))

## [1.4.3] - 2023-01-13

### Fixed

- An empty search string will now show all entries in the `LocalizedAsset` and `LocalizedString` popup instead of nothing. ([LOC-785](https://issuetracker.unity3d.com/issues/localization-string-value-search-returns-0-string-tables-when-no-characters-are-given-to-search-by))
- Default IOS localized fields are no longer set to empty strings. We now use the project locale values as the defaults. (LOC-804)
- Fixed `AmbiguousMatchException` when building to IOS. ([LOC-855](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-855))
- Fixed Tag & Shared metadata being duplicated on an entry when they were added multiple times. For example, setting `StringTableEntry.IsSmart` to `true` when it was already `true` would cause 2 `SmartFormatTag` to be added to the metadata. ([LOC-779](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-779))
- Fixed the Table Editor search not checking the first 2 string table columns. ([LOC-820](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-820))
- The Active Editor Locale will now persist between Editor sessions. ([LOC-787](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-787))
- The Table Editor will no longer draw alternating background bars in the remaining space. (LOC-842)

## [1.4.2] - 2022-09-27

### Added

- Added support for tracking a [MeshFilter](https://docs.unity3d.com/ScriptReference/MeshFilter.html) when using **Localized Property Variants**. (LOC-719)
- Added **Preload Behavior** to **Localization Settings**. This allows further control over which assets to preload, such as loading fallback assets as well as the selected locale. (LOC-762)
- Added `ITableProvider` and `ITablePostProcessor`. These let you provide tables from custom localizations and to apply changes to a table when it first loads. (LOC-673)
- Added `LoadAssetAsync<TObject>()` to `LocalizedAssetBase`. This helps to filter sub-assets when trying to load them and they share a common type among other sub-assets with the same name. (LOC-793)
- Added `LocalizationEditorEvents.ExtensionAddedToCollection` & `LocalizationEditorEvents.ExtensionRemovedFromCollection`, which you can use to react to collection extension additions & removals. Added `CollectionExtension.Destroy()` which you can use to perform cleanup when a collection extension is removed. (LOC-685)
- Added `LocalizedMesh` class to support localizing mesh assets.
- Added a step to the **Asset Table Addressables Analyzer** that checks if sub-assets are referenced by name and offers to upgrade them. (LOC-793)
- Added support for [IConvertible](https://docs.microsoft.com/en-us/dotnet/api/system.iconvertible) to Plural and Conditional Smart String formatters. This uses the `ToDecimal` method when a numeric value is required for evaluating the object. (LOC-778)
- Added support for `SystemLanguage.Hindi` in Unity 2022.2 and above. (LOC-672)
- Added support for drawing the enum values in the `GameObjectLocalizer` editor when tracking an enum property. (LOC-684)
- Added support for referencing sub-assets. Previously we would only address the main asset and extract the sub-asset by matching the type. This was unpredictable when the main asset was the same type or contained multiple sub-assets with the same type. We now address the sub-asset by name so that we can ensure we get it correctly regardless of the type or number of other sub-assets. (LOC-793)
- Added the 'NoTranslationFound' event to allow users to register to the callback whenever an entry doesn't have a translation. (LOC-688)

### Changed

- An exception is now thrown when attempting to add a built-in resource to an Asset Table when using `AssetTableCollection.AddAssetToTable`. Addressables doesn't currently support built-in resources. Create a local copy if you need to use a built-in resource.
- Updated Google Sheets API to 1.57.0.2720 and Newtonsoft.Json API to 13.0.1. (LOC-250)
- Updated to Addressables 1.20.5.
- When possible, the **CSV Extension** now stores the file path as relative to the project when using the **Save...** and **Open...** commands.

### Deprecated

- Deprecated API `LocalizedString.CurrentLoadingOperation` and `LocalizedAsset.CurrentLoadingOperation` now produce errors when used.

### Fixed

- Fixed **Google Sheets** `CreateMappingsFromColumnNames` not using `Locale.LocaleName` when considering possible Locale names.
- Fixed `InvalidOperationException: Stack empty` when using **GoogleSheets** extension on Linux. ([LOC-698](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-698))
- Fixed `ListFormatter` missing the spacer when extracting literal characters from a Smart String.
- Fixed `NullReferenceException` during Edit mode when calling `GetLocalizedString` on a smart but empty StringTableEntry. (LOC-754)
- Fixed errors when exiting playmode with **Enter playmode** support enabled and **use built addressables playmode**. ([LOC-722](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-722))
- Fixed file path not being saved when using **CSV Extension** **Save...** and **Open...** commands. ([LOC-751](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-751))
- Fixed freeze when an invalid Smart String was called with `WaitForCompletion` and throw exception was enabled in the settings. (LOC-768)
- Fixed LocalizedString preview not working when the instance was nested inside of a serialized class. ([LOC-812](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-812))
- Fixed newly created collections having `preload` enabled by default.
- Improved Addressables operations management. Fixed several issues that would cause an addressable operation to be leaked and the asset not released. ([LOC-794](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-794))
- Improved the **Missing Tables** list in the **Table Collection Editor** so it doesn't return false positives, which causes the "Create" button to become disabled. ([LOC-800](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-800))

## [1.3.2] - 2022-06-01

### Changed

- Changed CSV Extension "Open..." button so it performs a full import instead of a merge. ([LOC-540](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-540))
- Disabled fix for "New localization assets created automatically and/or old assets are modified when loading project without Library folder ([LOC-400](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-400))" on Unity 2019.4 to 2021.3. This is due to a bug in Addressables (ADDR-2521). This fix will be re-enabled in the future. (LOC-704)
- Updated dependency `com.unity.nuget.newtonsoft-json` to 2.0.2.

### Fixed

- Fixed `NullReferenceException` when creating a new table that takes name from renamed table. ([LOC-550](https://issuetracker.unity3d.com/issues/nullreferenceexception-creating-a-table-that-takes-name-from-renamed-table))
- Fixed `GameObjectLocalizer` failing to apply local asset references in Unity versions before 2022.2. ([LOC-714](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-714))
- Fixed `LocalizedStringTable` and `LocalizedAssetTable` property drawer not setting `GUI.changed` when the reference was changed. ([LOC-725](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-725))
- Fixed Asset Tables being destroyed during preloading. ([LOC-727](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-727))
- Improved `FormatException` error message when `String.Format` throws during `GetLocalizedString`.
- String Table entries will no longer try to use `String.Format` when using `GetLocalizedString` with non-null but empty arguments. ([LOC-689](https://issuetracker.unity3d.com/issues/localization-formatexception-error-is-thrown-in-the-console-window-when-calling-getlocalizedstringasync-class))

## [1.3.1] - 2022-04-14

### Added

- Added **Localize Property** context menu to properties in the Inspector. This can be used as an alternative to the **Track Changes** option to manually add the property as a **Localized Property Variant**. The menu item **Remove Localized Property** removes properties. The menu item **Remove Localized Property Variant** removes a specific selected Locale's override. (LOC-358)
- Added `RemoveTrackedProperty` to `TrackedObject`.
- Added support to handle a locale that has multiple fallbacks defined. ([LOC-607](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-607))

### Changed

- Localized string and asset variants that are empty (no table or entry set) will no longer produce errors and will be ignored.
- Replaced the `Locale Code` field in the Pseudo localization editor with a `Source Locale` field that only lets you select a locale in the project. (LOC-611)
- Updated to Addressables `1.19.19`.

### Deprecated

- Replaced `Locale.GetFallback` with `Locale.GetFallbacks` in order to support multiple fallbacks per locale. ([LOC-607](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-607))

### Fixed

- Fixed `LocalesProvider.AddLocale` ignoring the locale being added when a potential fallback was already in the provider.
- Fixed `LocalizedDatabase.GetTableAsync` creating a new operation each time when requesting a table that had not yet loaded. This would consume more memory than was necessary.
- Fixed `LocalizedString` and `LocalizedAsset` not showing prefab overrides in the Inspector. (LOC-592)
- Fixed `NullReferenceException` being thrown when applying property variants and the project locale was null. ([LOC-601](https://issuetracker.unity3d.com/issues/errors-are-thrown-in-play-mode-when-project-locale-identifier-is-not-set-in-localization-settings))
- Fixed bug where calling `RefreshString`, when the loading operation had not completed, would cause `RefreshString` to execute twice.
- Fixed build failures on Android Platform when System Language is used as Locale Source for Chinese (Simplified) & Chinese (Traditional). (LOC-681)
- Fixed console errors and warnings when making changes in the Inspector and the project was not configured for localization.
- Fixed duplicating of Addressable assets when importing a project with no library folder. (LOC-400)
- Fixed errors when trying to create a table in the Packages directory. ([LOC-614](https://issuetracker.unity3d.com/issues/localization-assets-cannot-be-created-inside-embedded-packages))
- Fixed fallback not working when the fallback had a missing table.
- Fixed operations being released immediately in edit mode, this could cause data to be lost too soon. ([LOC-667](https://issuetracker.unity3d.com/issues/getlocalizedstring-returns-null-when-the-editor-is-not-in-play-mode))
- Fixed operations sometimes being released too soon, which could cause them to be released before the yield on the operation had executed.
- Fixed Psuedo locale editor using the incorrect field for the locale name.
- Fixed Unity freezing when calling `WaitForCompletion` on a string operation that threw an exception during formatting. (LOC-630)
- Improved automatic releasing of operations so that we no longer use `CompletedTypeless`, which generates GC.
- Improved memory management to reduce the amount of garbage being generated by internal operations and adding/removing events. (LOC-571)
- Reduced allocations when applying Localized Property variants and the string/asset operation was still loading. (LOC-480)

## [1.2.1] - 2022-02-14

### Added

- Added **Import/CSV(Merge)** to only update the entries in the CSV. ([LOC-540](https://issuetracker.unity3d.com/issues/localization-table-editor-does-not-remove-extra-rows-when-opening-csv-file-with-lower-rows-count))
- Added **Page Controls** to the **Table editor**. This limits the number of items visible at one time and helps to improve the performance when dealing with large tables. ([LOC-474](https://issuetracker.unity3d.com/issues/localizations-string-table-update-time-is-low-when-working-with-many-entries))
- Added `GetRowEnumeratorUnsorted` to `LocalizationTableCollection.cs`, this allows for stepping through the rows in the same order as the `SharedTableData`.
- Added SetTable and SetEntry methods to LocalizedStringEvents. These can be used with UnityEvents to update localized strings. (LOC-377)

### Changed

- **Import/CSV** will now replace the contents and remove missing entries. ([LOC-540](https://issuetracker.unity3d.com/issues/localization-table-editor-does-not-remove-extra-rows-when-opening-csv-file-with-lower-rows-count))
- CSV export now exports the rows in the same order as the `SharedTableData`. ([LOC-588](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-558))
- Improved some menu items UX by renaming to include ellipses ("...") when the action requires additional input.
- Updated to Addressables `1.19.17`.

### Fixed

- Fix null reference exceptions been thrown when Custom Locale does not use Custom Formatter. (LOC-526)
- Fixed `GameObjectLocalizer` not updating when reenabled. ([LOC-568](https://issuetracker.unity3d.com/issues/localization-scene-controls-do-not-change-font-asset-in-disabled-gameobjects))
- Fixed `LocalesProvider` throwing exception when adding a pseudo-locale with the same name as another. ([LOC-548](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-548))
- Fixed `LocalizedStringDatabase.GetLocalizedStringAsync` wrapping IList arguments with `params object[]`. ([LOC-575](https://issuetracker.unity3d.com/issues/localizedstringdatabase-dot-getlocalizedstring-wraps-corrupts-arguments))
- Fixed AvailableLocales locales list returns an empty list if InitializationOperation is not done. (LOC-506)
- Fixed crash with YGLayoutNodeInternal when opening the docked Project Settings window after deleting the Localization Settings file. ([1371884](https://issuetracker.unity3d.com/issues/editor-crashes-when-opening-the-attached-project-settings-window-after-deleting-the-localization-settings-file))
- Fixed InvalidOperationException when setting Asset Table Entry to None when it is already empty. ([LOC-520](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-519))
- Fixed issue where GetLocale returns a null reference instead of a fallback Locale when providing CultureInfo. (LOC-462)
- Fixed Legacy `GroupResolver` generating a `FormattingException` when resolving an asset's addressable group name. The Smart String argument  {Identifier-Code} should be {Identifier.Code}. This will need to be fixed manually to existing legacy group resolvers, alternatively create a new legacy group resolver asset. ([LOC-544](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-544))
- Fixed preloading error "A table with the same key already exists. " caused when loading a table before preloading had completed. ([LOC-581](https://issuetracker.unity3d.com/issues/error-a-table-with-the-same-key-already-exists-something-went-wrong-during-preloading))
- Fixed shared comments not being updated correctly when imported from CSV. ([LOC-577](https://issuetracker.unity3d.com/issues/shared-comments-are-lost-when-exporting-slash-importing-a-csv-file))
- Fixed the Locale Game View Menu from blocking the top part of the GameVIew. ([LOC-552](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-552))
- Fixed the player freeze or player crash caused by WaitForCompletion on PreloadDatabaseOperation. when doing an Il2cpp Standalone build. (LOC-527)

## [1.1.1] - 2022-01-13

### Fixed

- Rerelease of 1.1.0 to fix an issue with package promotions.

## [1.1.0] - 2022-01-10

### Added

- Added a 'ClearAllEntries' API to `StringTableCollection` and `AssetTableCollection` for clearing all the Entries and assets when needed. (LOC-420)
- Added a new Localized Asset and Localized String picker which uses **Unity Search**. This has improved performance and supports multiple localization filters and custom table view columns. This is also available in the Unity Search window but does not currently do anything when an entry is selected. The legacy picker can be reenabled through the preferences window. (LOC-267)
- Added additional Persistent Variable types for: byte, sbyte, short, ushort, long, ulong, uint and double. (LOC-431)
- Added support for localizing the User Tracking Description (NSUserTrackingUsageDescription) on iOS. (LOC-403)

### Changed

- Added a warning when building to iOS/Android and the App Info has not been configured. App Info must be present in order to correctly configure the apps language settings.
- Updated to Addressables `1.19.15`.

### Deprecated

- `LocalizationEditorSettings.FindSimilarKey` is now deprecated and will be removed in the future. Unity Search now supports searching. See [TableEntrySearchData](xref:UnityEditor.Localization.Search.TableEntrySearchData) for further details.

### Fixed

- Added ScrollView support to Table metadata editor. (LOC-500)
- Entries with duplicate names will now be ignored when pulling from Google Sheets. A message will be added to the report to notify you when this happens. ([LOC-464](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-464))
- Fixed `NullReferenceException` when showing property drawer for PlatForm Override Type Entry and no entry was selected. (LOC-502)
- Fixed bug where adding Pseudo Locale to the project were not been shown in Active Locale popup in Localization Scene Controls window. (LOC-439)
- Fixed bug where LocaleIdentifier code comparisons were case-sensitive. (LOC-463)
- Fixed CSV `KeyIdColumns` not using the custom Key, Id, and Shared Comments field names when exporting to CSV. ([LOC-481](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-481))
- Fixed detection of Android system language. ([LOC-478](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-478))
- Fixed possible `OverflowException` occurring when using multiple `Preserve Tags` methods in a pseudo-locale. ([LOC-517](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-517))
- Fixed localized property variant assets not being applied in 2022.2 standalone player builds due to changes from 1342327.
- iOS Xcode project will now be configured to use the **Project Locale** as the development language. This should cause this language to become the default language used when the app does not support the system language. ([LOC-507](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-507))
- The `SystemLocaleSelector` will now detect the preferred language on iOS. ([LOC-452](https://issuetracker.unity3d.com/issues/ios-localization-is-ignored-when-changed-through-application-settings))
- When building to Android the App Name localization will now always be included and not only when an Icon was also configured.

## [1.0.5] - 2021-10-27

### Fixed

- Fixed bug where New Localization Tables are not preloaded when Preload All Tables is enabled. ([LOC-423](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-422))
- Fixed TreeView compilation issue on 2022.1.

## [1.0.4] - 2021-10-19

### Fixed

- Added new XLIFF import option "Import / XLIFF into Collection". (LOC-376)
- Fixed `FileNotFoundException` being thrown when using the CSV extension **Export** button with a new file. ([LOC-429](https://issuetracker.unity3d.com/issues/filenotfoundexception-is-thrown-when-exporting-localization-csv-file))
- Fixed `MissingReferenceException` occurring due to `ReleaseNextFrame` when exiting play mode.
- Fixed `NullReferenceException` when pulling a Google sheet with the **Remove Missing Pulled Keys** option enabled and the missing key did not contain an entry in the first table. ([LOC-434](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-434))
- Fixed issue where Localized Property Variants reset the world space UI's z position when Active Locale is changed. (LOC-399)
- Fixed issue where 'RaiseTableEntryModified' event not been invoked when there is a change in EntryKey. (LOC-397)
- Fixed nested `LocalizedStrings` not trigging a ValueChanged event when their local variables were changed. ([LOC-418](https://issuetracker.unity3d.com/issues/nested-localizedstring-does-not-refresh))
- Modified assets will now be written to disk after importing CSV, XLIFF or Google Sheets. ([LOC-341](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-341))
- Pressing Escape key will now close the popup window for selecting a table or table entry. ([LOC-416](https://issuetracker.unity3d.com/issues/localization-cant-close-localizedstring-dropdown-with-esc-key))

## [1.0.3] - 2021-09-30

### Fixed

- Removed duplicate sample code `StringTableCollectionSamples`. This was causing some internal test failures that was preventing us from adding the package to 2021.2.

## [1.0.2] - 2021-09-28

### Fixed

- Fixed `Element 'Style' has no registered factory method` error in 2019.4. (LOC-318)
- Fixed AppIcon not resizing according to the DPI input for the selected Android Icon Configurations when building for Android. ([LOC-371](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-368))
- Fixed changes to existing Property Variants being recorded for a Locale that was not currently tracked when Track changes was disabled. ([LOC-380](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-380))
- Fixed XLIFF exporter crash when String Table Entry is null or empty. (LOC-383)

## [1.0.1] - 2021-09-16

### Fixed

- Fixed missing API issues in Scripting API documentation.

## [1.0.0] - 2021-09-10

### Added

- `LocalizedString` can now be used as a local and global variable. When accessed in a *Smart String* this gives access to the translated value and can also be used for accessing metadata. See the **Persistent Variables** sample for an example of handling gender using this method. (LOC-257)
- Added **Local Variables** support to `LocalizedString`. This works in a similar manner to **GlobalVariables** however the variables can only be accessed via the `LocalizedString` they are attached to.
- Added 'Show Folder' button to CSV Extention to displlay the folder where the CSV file was last saved.
- Added support for defining templates through `TemplateFormatter` editor.

### Changed

- ` LocalizedString` **FallbackState** and **WaitForCompletion** fields are now always visible in the inspector.
- `IVariable` interface has been changed from using a property for the source value to a method in order to provide additional information. Please see upgrade guide for further details. (LOC-257)
- After exporting CSV and XLIFF the output directory will now be opened.
- Disabled **WaitForCompletion** support on [WebGL](https://docs.unity3d.com/Packages/com.unity.addressables@latest/index.html?subfolder=/manual/SynchronousAddressables.html#webgl).
- Setting the `LocalizedString.LocaleOverride` will now force an update.
- Types marked with `ObsoleteAttribute` will no longer be shown in dropdown menus.
- Updated icons with new styles. (LOC-248)
- Updated to Addressables `1.19.4`.

### Deprecated

- `GlobalVariables` namespace has been renamed to `PersistentVariables` and associated scripts have also been renamed. Script upgrade API will automatically upgrade relevant scripts.
- `LocalizedStringEvent` arguments have been deprecated. A warning will appear in the inspector to say so if they are being used and will offer to upgrade by transferring them into the String Reference Local Variables field.

### Fixed

- Added multi edit support for `LocalizedString`, `LocalizedAsset`, and `LocalizedStringEvent`. ([LOC-295](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-295))
- Changes are now saved when fixing missing entries in a table. (LOC-205)
- CSV files that are imported and exported through the Editor will now use `FileShare.ReadWrite` to avoid sharing violations with other software that may have the files open. ([LOC-348](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-348))
- Custom Locale Window will now show a Save File Panel instead of Save Folder Panel. (LOC 293)
- Fixed **Localization Settings** editor String Database/Smart Format, Sources and Formatters not being correctly nested under the **Smart Format** foldout.
- Fixed `NullReferenceException` occuring in `ScenePropertyTracker` when `currentValue` was null. (LOC-359)
- Fixed `Selected locale is null` error occurring when entering playmode and using `WaitForCompletion` or calling `GetSelectedLocale`. ([LOC-305](https://issuetracker.unity3d.com/issues/waitforcompletion-selected-locale-is-null))
- Fixed a compilation error regarding `GameObjectLocalizer` when using Unity 2020.2.
- Fixed Android build failure when the application name contains an escape character . ([LOC-292](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-292))
- Fixed build failures when building for Android with Android App info metadata for certain Locale codes. ([LOC-361](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-361))
- Fixed error regarding unknown Style in 2019.4 when opening Localization Settings editor. ([LOC-304](https://issuetracker.unity3d.com/issues/2019-dot-4-element-style-has-no-registered-factory-method))
- Fixed error where StringTable gets overwritten when removing and re-adding it back to the String Table Collection. (LOC-168)
- Fixed errors in Table Editor when a selected entry was removed. (LOC-296). ([LOC-296](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-296))
- Fixed errors while changing SelectedLocale after adding and removing a method to LocalizedStringTable.TableChanged event. (LOC-320)

## [1.0.0-pre.10] - 2021-07-20

### Added

- Added **Localized Property Variants** feature. This lets any component property be tracked for locale specific changes and provides an alternative workflow to the LocalizedStringEvent/LocalizedAssetEvent component approach. See the docs for further details. Note this feature requires **Unity 2020.3** and above.
- Added `AssetTableEntry.SetAssetOverride`. This enables updating an **Asset Tables** localized assets in the player so entries can be updated, such as from an external source. (LOC-217)
- Added `InitializeSynchronously` property to `LocalizationSettings`. This can be used to complete initialization immediately when it is started instead of asyncronously.
- Added `Key` property to `TableEntry`.
- Added `ProjectLocale` property to LocalizationSettings`. This is used by the **Localized Property Variants** to record the default values.
- Added a warning to the Inspector when a table collection or entry is missing for `LocalizedString`, `LocalizedAsset`, `LocalizedStringTable` and `LocalizedAssetTable`. (LOC-176)
- Added overloaded `AddAssetToTable` that takes a `LocaleIdentifier` to `AssetTableCollection`. This version also creates a new table if one does not exist.
- Added Unicode escape sequence support to Smart Format when using `ConvertCharacterStringLiterals`. This lets you write unicode in strings in the form `\uXXXX`.

### Changed

- **WaitForCompletion** and **Enable Fallback** fields are now always shown in the `LocalizedString` and `LocalizedAsset` property drawers.
- Added `LocalizedAssetBase` and `LoadAssetAsObjectAsync` to all `LocalizedAssets`. This enables loading Localized assets in a generic way.
- Changed `AssetTable.GetAssetAsync` to use a `TableEntryReference` instead of a long.
- Changed `SharedTableData.Addkey(string)` so that if the name already exists then a unique version is generated based on the provided name instead of returning null.
- Google Sheets Authorization errors are now reported in the console when authorizing through the **Google Sheets Service Provider** editor.
- Removed console warning in new project regarding missing `LocalizationSettings` when opening scene controls (LOC-194) and the project did not contain the `LocalizationSettings` asset. ([LOC-194](https://issuetracker.unity3d.com/issues/warning-when-opening-scene-controls-window-in-an-empty-project))
- Small performance improvement to `SystemLocaleSelector`, when looking for a fallback Locale we do not check the same Locale twice.
- Updated to Addressables `1.18.11`.

### Deprecated

- Deprecated some API with typos in `SheetsServiceProvider` (`SetOAuthCrendtials` and `AuthoizeOAuth`).

### Fixed

- **Google Sheets Service Provider** now supports authorizing multiple OAuth tokens. You will need to re-authorize when upgrading. (LOC-188)
- **Pseudo Locale Editor** Methods and Preview fields now change expanded state if the label is clicked.
- `LocaleIdentifier` property drawer now use the property display name instead of "Identifier".
- Added  `xml:space="preserve"` to exported **XLIFF** translation units. (LOC-193). ([LOC-193](https://issuetracker.unity3d.com/issues/new-lines-in-xliff-are-deleted))
- Fixed **Locale** not having the correct label applied when first added and the project contained no Addressable Asset Settings.
- Fixed `LocalizedDatabase` so it correctly releases all references it holds to a table.
- Fixed `NullReferenceException` being thrown when opening the Tables Window menu and the project contains no Localization Settings. ([LOC-200](https://issuetracker.unity3d.com/issues/nullreferenceexception-appears-when-opening-localization-tables-window-more-options-menu-and-there-are-no-localization-settings))
- Fixed `NullReferenceException` being thrown when pushing to a **Google Sheet** and the **String Table Collection** contained a Key with no localized values. ([LOC-199](https://issuetracker.unity3d.com/issues/localization-nullreferenceexception-when-pushing-empty-string-to-google-sheets))
- Fixed `NullReferenceException` when the Addressable settings contained a null group. (LOC-196)
- Fixed `SelectedLocale is null` error being thrown when calling `GetLocalizedString` whilst exiting Play Mode. ([LOC-209](https://issuetracker.unity3d.com/issues/selectedlocale-is-null-error-is-thrown-when-accessing-a-localization-dot-localizedstring-within-update-and-then-exiting-play-mode))
- Fixed a bug in `LocalizedStringPropertyDrawer` and `LocalizedAssetPropertyDrawer` that changed `LocalizedString` and `LocalizedAsset` values when reordering them in a list. (LOC-128)
- Fixed Addressables issue that would throw the error `Exception: Reentering the Update method is not allowed` when calling `WaitForCompletion` inside of the `OnLocaleChanged` callback. The event is no longer called from inside of an Addressables update.
- Fixed Analyze rules throwing an `InvalidCastException` when no issues were found and the Fix operation was performed. (LOC-201)
- Fixed background tasks becoming unresponsive after importing/exporting CSV files.
- Fixed compilation errors in DocCodeSamples.Tests when the UGUI package was not included in the project. (LOC-197)
- Fixed duplication of existing Locales when importing a Table Collection. (LOC-175)
- Fixed errors in **LocalizationSettings** editor when deleting Locales and Addressable asset settings from the project.
- Fixed errors when opening **Device Simulator window**.
- Fixed errors when reordering Table Entry Metadata when PlatformOverride is added as metadata. (LOC-!87)
- Fixed LocalizedString not using WaitForCompletion when using the StringChanged event. (LOC-218)
- Updated `LocalizationTableCollection.RemoveEntry` to remove entries from tables.

## [1.0.0-pre.9] - 2021-05-12

### Added

- Added `ExcludeEntry` **Metadata** . This can be used to prevent an entry from being exported to Google Sheets, XLIFF, and CSV.
- Added `WaitForCompletion` property to `LocalizedString` and `LocalizedAsset`. This can be set in the inspector or via script to force synchronous loading.
- Added synchronous methods to `LocalizedString` and `LocalizedAsset`.

### Changed

- `LocalizationSettings.SelectedLocale` is now syncronous and does not need Initialization to have been completed.
- Moved `TryEvaluateAllLiterals` into its own interface `IFormatterLiteralExtractor` and renamed to `WriteAllLiterals`.
- Renamed `LocalizeString.GetLocalizedString` to `LocalizeString.GetLocalizedStringAsync`.
- System Locale Selector will now only output the message The Locale X is not available, however the parent locale Y is available. in the player.
- Updated Smart Format library to `aa9e4288612f79b86ab0cea36066797118d11a69` excluding the Nullable types support.
  - Fixed: Take an erroneous format string like `"this is {uncomplete"` (missing closing brace). Before v2.7.0 the parser handled `{uncomplete` as a `TextLiteral`, not as an erroneous `Placeholder`.
  - Fixed: Fixed issue where If the `Parser` encountered a `ParsingError.TooManyClosingBraces`, this closing brace was simply "swallowed-up". This way, the result with `Parser.ErrorAction.MaintainTokens` differs from the original format string. From v2.7.0, the redundant closing brace is handled as a `TextLiteral`. 
  - Improved: For `ParsingError.TrailingOperatorsInSelector` and `ParsingError.InvalidCharactersInSelector` the causing character is now included in the `Exception.Message`.
  - Fix: Fully implemented all `Settings.ParseErrorAction`.
  - Enhanced SubString extension as described in [PR142](https://github.com/axuno/SmartFormat/pull/142).
- Updated to Addressables `1.18.2`.

### Fixed

- Added support for `WaitForCompletion` so Async operations can be completed immediately and support synchronous workflows. (LOC-185)
- Fixed `ArgumentException` when adding a new table collection in a new project. ([LOC-192](https://issuetracker.unity3d.com/issues/errors-thrown-first-time-entering-play-mode))
- Fixed `LocalizeStringEvent` overwriting the `StringReference` Arguments when enabled.
- Fixed `NullReferenceException` in LoadTableOperation when the Locale could not be found. ([LOC-192](https://issuetracker.unity3d.com/issues/errors-thrown-first-time-entering-play-mode))
- Fixed `TemplateFormatter` not being added when selected through the Add Formatter menu. (LOC-179)
- Fixed default table version of `GetLocalizedStringAsync` arguments being wrapped in an `object[]` when using `IList<object>`. (LOC-191)
- Fixed Localized assets not using fallbacks.
- Fixed table name validation - names can no longer contain leading or trailing whitespace, nor can they contain characters that Unity considers invalid. ([LOC-183](https://jira.unity3d.com/browse/LOC-183))
- Fixed warning regarding `SelectedLocale` being thrown when entering play mode and the **Scene Controls Window** was open.
- Locale Analyzer, String Table Analyzer and Asset Table Analyzer will now check the Addressables groups have valid schemas. (LOC-190)
- Table Window now updates itself when Tables, Collections or Shared Table Data is deleted or restored in the project. (LOC-182)

## [1.0.0-pre.8] - 2021-04-27

### Changed

- Changed to Pre-release.

## [0.11.1] - 2021-04-23

### Fixed

- Fixed errors being generated when using Play Mode with Domain and Scene reload disabled. ([LOC-165](https://issuetracker.unity3d.com/issues/errors-thrown-when-entering-playmode-and-using-enter-play-mode-options-experimental))
- Fixed `NotSupportedException: Collection is read-only` error sometimes being produced by preload operation.

## [0.11.0] - 2021-04-08

### Added

- Added **Addressable Group Rules** asset. This can be used to configure what Addressable groups assets should be placed into. See Addressables Integration docs for further details.
- Added `CollectionExtension.Initialize`, this will be called when the extension is first added to a table collection.
- Added `IEntryOverride`. This can be used to create overrides for table entries to direct to different localized values.
- Added `Locale.LocaleName` property. This can be used to customize the name of the Locale instead of using the name of the Locale Asset in the `name` property. ([LOC-144](https://issuetracker.unity3d.com/issues/localization-locale-assets-name-is-not-saved-when-renaming-it-through-the-inspector))
- Added `LocalizationEditorSettings.GetPsuedoLocales`. This returns all Pseudo Locales that are included in the project.
- Added `LocalizedGameObjectEvent` component. This can be used to localize Prefabs.
- Added `LocalizedMaterial` & `LocalizedGameObject`.
- Added `PlatformOverride` **Metadata**. This can be used to override the value for an entry for a chosen platform, the override can change the entry, table or both.
- Added `ReleaseAsset` method in `AssetTable`. This can be used to release an asset from memory for the provided AssetTableEntry.
- Added `ReleaseTables` to `LocalizedDatabase`. This can be used to force a table to be released from the database so that it may be unloaded providing there are no other references to it.
- Added `SetEntryAssetType` and `GetEntryAssetType` to `AssetTableCollection.`. This can be used to control the expected asset type for an entry.
- Added `SharedEntry` property to `StringTableEntry` and `AssetTableEntry`. This will return the `SharedTableData` entry for the current Locale specific entry.
- Added `SharedStringTableEntry` and `SharedAssetTableEntry` to `MetadataType`. These can be used to only show **Metadata** for a shared table entry only.
- Added 3 new Addressable Analyzer rules which can be used to check for Addressable issues and upgrade when changing the **Addressable Group Rules**.
- Added additional override for `StringTableEntry.GetLocalized` which allows for passing in an optional **Pseudo Locale**. By default the **Selected Locale** will be used. (LOC-162)
- Added constructors to `LocalizedString`, `LocalizedAsset`, `LocalizedStringTable` and `LocalizedAssetTable`. These show the minimum arguments required for the class to function correctly.
- Added extra override for method `GetLocalizedString` which now accepts the parameters as list of objects [IList<object>].
- Added language change menu to Device Simulator window.
- Added metadata support to **LocalizationSettings**.
- Added method `TableEntryReference.ToString(TableReference)`. This will use the `TableReference` to gather additional data such as the **Key** or **Key Id**.
- Added support for localizing **Android App name**. Only the **Display Name** is currently supported on the Android Platform. To configure the Android App add the **Android/AppInfo** metadata to the LocalizationSettings.
- Added support for localizing Android app Icon. We currently support  Adaptive Icon, Round Icon, and Legacy Icon on the Android Platform. To configure the Android app icons, add the Android/Android Adaptive Icon or Android/Android Round Icon or Android/Android Legacy Icon metadata to the LocalizationSettings.
- Added support for localizing iOS App components. Display Name, Microphone description, Camera description and Location description are currently supported. To configure the iOS App, add the **iOS/AppInfo** metadata to the **LocalizationSettings**.
- Added the functionality to **Preview Localization** without entering Play mode. Do this through the **Localization Scene Controls** window. See Edit Mode Support docs for further details.
- Added UI validation hints for table collection creation.
- When a `TableReference` is a Guid type, the `TableCollectionName` will now attempt to resolve the name from the `AssetDatabase` or `LocalizationSettings`. This is particularly helpful when debugging as `ToString` will now also contain the `TableCollectionName` as well as the `Guid`.

### Changed

- `MetadataAttribute.AllowMultiple` is now respected. Metadata that already exists will now be disabled in the add menu.
- Changed `LocalizedString.Arguments` to be an `IList<object>` instead of `object[]`.
- Changed table collection names so they can no longer be comprised of whitespace, cannot contain invalid filename characters, and cannot contain "[]".
- Changed table collections so you can no longer create table collections of the same type with duplicate names.
- Changed the **Google Sheets Service - New Sheet Properties** field so it is now only visible when using **OAuth** authentication.
- Moved `UnityEventAudioClip`, `UnityEventSprite`, `UnityEventString` and `UnityEventTexture` to `UnityEngine.Localization.Events` namespace.
- Removed `LocaleIdentifier.Undefined`. You should compare against `default` to see if a `LocaleIdentifier` is defined. ([LOC-150](https://issuetracker.unity3d.com/issues/inconsistent-behaviour-between-localeidentifier-constructors-and-deserialization-when-using-different-empty-instances))
- Renamed `StringUnityEvent` to `UnityEventString`.
- Updated **Addressables** to **1.17.15**.

### Fixed

- `StringTableEntry.GetLocalizedString` variants now use the table's locale formatter if no formatter is provided.
- Added new functionality, so loading a table via its **Table Collection Name Guid** will now complete immediately if it was previously loaded using the **Table Collection Name**. (LOC-172). (LOC-172)
- Fixed **Psuedo Localization** so that when it is enabled, it will now be applied when calling `StringTableEntry.GetLocalized`. (LOC-162)
- Fixed `AssetTypeMetadata` being added multiple times to **Shared Table Data** when adding new entries.
- Fixed `CollectionExtension.TargetCollection` not being set when adding an extension via script.
- Fixed `LocaleIdentifier` Equality comparisons when comparing undefined versions. ([LOC-150](https://issuetracker.unity3d.com/issues/inconsistent-behaviour-between-localeidentifier-constructors-and-deserialization-when-using-different-empty-instances))
- Fixed `LocaleIdentifier` property drawer not updating when changing the Locale object. ([LOC-148](https://issuetracker.unity3d.com/issues/localization-localizationsettings-slash-locale-id-field-is-not-updated-when-selecting-locale-with-object-picker))
- Fixed `MissingMethodException` exception being thrown when adding a **Smart String** Formatter that does not contain a default constructor. ([LOC-164](https://issuetracker.unity3d.com/issues/adding-smart-format-formatters-via-inspector-throws-exception))
- Fixed `NullReferenceException` being thrown when pulling from an empty Google Sheet. (LOC-161)
- Fixed `ValueTupleSource` not being marked as `Serializable` which could cause serialization issues. ([LOC-147](https://issuetracker.unity3d.com/issues/unknown-managed-type-referenced-unity-dot-localization-unityengine-dot-localization-dot-smartformat-dot-extensions-dot-valuetuplesource))
- Fixed a bug where invoking `RemoveKey` was not removing the Keys from other tables. Now, this is handled by abstract overrides in StringTableCollection and AssetTableCollection.
- Fixed corruption of PlayerSettings preloaded assets when building with the **InputSystem** package in the project. ([LOC-146](https://issuetracker.unity3d.com/issues/localization-slash-inputsystem-asset-is-not-removed-from-preloaded-assets-list))
- Fixed Editor becoming unresponsive when using the **Google Sheets Service Authorize** button. (LOC-166)
- Fixed error regarding nested header foldouts in LocalizedString inspector. ([LOC-163](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-163))
- Fixed exception being thrown when adding a **CSV Extension** to a table collection. ([LOC-158](https://issuetracker.unity3d.com/issues/unityexception-is-thrown-while-moving-or-creating-scripts-after-adding-csv-extension-to-serialized-string-table-collection))
- Fixed incorrect formula syntax in Google Sheets duplicate key rule. This would cause errors when creating a new sheet and the default Google Sheet Language was not English. ([case 1286943](https://issuetracker.unity3d.com/product/unity/issues/guid/1286943))
- Fixed issue so that pulling from a **Google Sheet** with an **API Key** will no longer create an entry from the sheets header row.
- Fixed Localization being applied twice when changing the reference on a `LocalizationStringEvent` or `LocalizedAssetBehaviour` on a disabled GameObject which was then activated. ([LOC-132](https://issuetracker.unity3d.com/issues/localizestringevent-applies-localization-twice-when-activating-object-if-stringreference-was-set-when-object-was-inactive))
- Fixed Localization Tables window not updating when a value was changed in the LocalizedString property drawer.
- Fixed Localization Tables window not updating when Converting Table Entry to Smart String in the LocalizedString property drawer.
- Fixed NullReferenceException when pulling from a **Google Sheet** that contains an empty row. (LOC-149)
- Fixed NullReferenceException when pulling from a Google Sheet that contains an empty row. ([LOC-149](https://issuetracker.unity3d.com/issues/googlesheets-nullrefexception-when-pulling-empty-cell))
- Fixed possible infinite loop when calling `Locale.GetFallback()`.
- Fixed the **Table Collection Editor** incorrectly showing the Tables field as a ReoderableList in 2020.2+. (LOC-167)
- Regenerated Object Pool guids to prevent conflicts when using the package in a project with other packages that use the ObjectPool classes.

## [0.10.0] - 2021-01-20

### Added 

- Added `GetDefaultTableAsync` to `LocalizedDatabase`. This can be used to get the DefaultTable directly. 
- Added `HasChangeHandler` to `LocalizedString` and `LocalizedAsset`. ([LOC-111](https://issuetracker.unity3d.com/issues/localization-unable-to-check-if-registerchangehandler-is-null-when-using-localisestringevent))
- Added `GenerateCharacterSet` to `StringTable` and `StringTableCollection`. This can be used to extract the unique characters used by 1 or more `String Tables`. This could then be used with a Font Atlas creator. It is also possible to export a **Character Set** file from the Export menu of the String Table window, the String Table Collection window, or the Localization Tables window.
- Added new Google Sheets example `SmartStringColumn` for synchronizing the Smart String property of an entry.
- Added **Global Variables** to **Smart Strings**. **Global Variables** can be defined in a `GlobalVariableGroup` asset which can then be added to a `GlobalVariableSource` in the **Smart Format Sources** list. Variables can then be referenced in a **Smart String** without the need to pass arguments. For example: `{group-name.variable-name}`. When a `GlobalVariables` value is changed via script, it will automatically trigger any `LocalizedString` that is using it to refresh. See Samples and docs for further details.
- Added `HasTagMetadata` to `TableEntry`.
- Added PreLoadTables to LocalizedDatabase. This can be used to force tables to be loaded, including their contents without having to set the preload flag in the editor.
- Added fallback support. When a localized value is missing then the Locale fallback metadata or CultureInfo will be used to find a suitable fallback Locale. This can be enabled project-wide through the Localization Settings String Database or Asset Database, it can also be configured on a per-item basis for LocalizedStrings.
- Added useFallback parameter to LocalizedStringDatabase.GetLocalizedStringAsync & LocalizedDatabase.GetTableEntryAsync. This can be used as a flag to enable FallBack behavior for Locales that have missing translations.
- Added Custom Locale UI Window. This can be used to add any custom Locales that are not supported in CultureInfo class or the SystemLanguage.
- Added **Preloading Screen** sample. This shows how you can wait for the `InitializationOperation` to complete.

### Changed
- Reduced the amount of garbage being generated by requests for localized strings and assets. The operations inside of `AsyncOperationHandle` are now returned in the subsequent frame after completion. If you wish to hold onto an `AsyncOperationHandle` you should call `Addressables.ResourceManager.Acquire` to prevent the operation being reused and `Addressables.Release` to allow for it to be reused.
- Merged all Third Party dependencies into a single library **Unity.Localization.ThirdParty.Editor.dll**. This will prevent conflicts when projects/packages use the same libraries. **Google Sheets**, **Newtonsoft Json** and **CsvHelper** can still be accessed by adding **Unity.Localization.ThirdParty.Editor** as an **Assembly Reference** to your own asmdef files.
- Removed dependency on package **com.unity.nuget.newtonsoft-json**. It is now part of the combined dependencies.
- Updated Third Party dependencies. Google Sheets to v.1.49.0.2111, CsvHelper to 15.0.9 and Newtonsoft.Json to 12.0.3.
- Clicking the **Localized Table Collection** name label will now toggle the selector foldout for `LocalizedString` and `LocalizedAsset`. ([LOC-125](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-125))
- Significant improvements to **Smart String** overall performance. Testing showed results of an average of 10x less garbage and runs 2-3x faster.
- Updated **Smart Strings** with changes from **SmartFormat** v2.5.3.0-preview1.
  - Internal string comparisons (i.e. for placeholder names) are no more culture-specific, but Ordinal or OrdinalIgnoreCase respectively.
  - ListFormatter will now process IList data sources only.
  - Removed **SmartObjects**
- Updated to **Addressables** 1.6.13. 
- It's now possible to import `StringTableCollections` and `AssetTableCollections` into new or existing projects. When importing the Collection will create/update the Addressables and create any missing Locales in the project.
- Changed sample **Language Selection Menu (UGUI)** to use the Native name for Locales.
- The `Localization Tables` window search field is no longer cleared when changing selected table.
- After pulling from a **Google Sheet**, the order of the keys in the table will now match the order of the **Google Sheet**.
- When pushing to a **Google Sheet**, Table entries will no longer be sorted by the Key Id but keep their current order, as shown in the table editor window.
- It is now possible to use a **Google Sheet** with Sheet Id 0.
- Creating a new `LocalizationSettings` asset in the **Project Settings** window no longer selects the asset.
- Setting `LocalizedString` `Arguments` will now trigger `RefreshString`.
- `LocalizedString.RefreshString` will no longer throw an exception if there are no `StringChanged` subscribers.
- `LocalizedStrings` that are **Smart** will always use **Smart Format** even when no arguments have been provided.
- Culture-sensitive formatting now uses the `Locale` information.
- The `Localization Tables` window search field is no longer cleared when changing selected table.
- The `Localization Tables` window search field is no longer cleared when changing selected table.
- No Translation Found message is now a Smart String and has access to several placeholders which can be used to proivide details on the missing entry. 

### Fixed
- Fixed exceptions being thrown when trying to load a table that does not exist. Loading missing String Tables will now use the missing translation message instead of throwing an exception when a table was missing and assets will return a failed operation handle. (LOC-120)
- Fixed DefaultTable property throwing an invalid key exception when it is empty. Now getting a value from empty DefaultTable will throw an exception. 
- The LocalizedAsset selector will now filter out incompatible types. ([LOC-123](https://issuetracker.unity3d.com/issues/localizedasset-property-drawer-does-not-filter-by-type))
- Fixed **LocalizedString** Editor not preserving new items added to the Preview Arguments field. ([LOC-122](https://issuetracker.unity3d.com/issues/new-items-are-not-added-to-the-preview-arguments-list-in-the-localizedstring-field-when-pressing-the-plus-icon))
- Fixed `AddTagMetadata` creating a new tag each time instead of using an existing tag. `RemoveTagMetadata` will now remove all empty tag entries, not just the first one it finds. ([LOC-124](https://issuetracker.unity3d.com/issues/smart-format-tag-in-the-metadata-stays-after-disabling-the-smart-option-when-smart-option-is-enabled-on-another-entry))
- **String Table Entries** which have a null or empty localized string will now be treated as missing translations and use the `No Translation Found Format`. ([LOC-126](https://issuetracker.unity3d.com/issues/localization-no-translation-found-format-variable-isnt-used-when-an-entry-locale-is-empty))
- Fixed exception when calling GetLocale and no Addressables settings exists in the project.
- Fixed **XLIFF 2.0** generating empty **notes** nodes. ([LOC-131](https://issuetracker.unity3d.com/issues/localization-export-xliff-generates-table-with-notes-node-when-notes-are-not-added))
- Fixed **Google Sheets** `PullEnd` not being called after pulling from a Google Sheet. ([LOC-133](https://issuetracker.unity3d.com/issues/googlesheets-does-not-call-pullend-on-custom-columns))
- **Google Sheets** `PullCellData` is now called for all cells even if the sheet cell is empty. (LOC-134)
- Pulling a **Google Sheet** using an **API Key** will now use the correct public API and no longer produce authorization errors. ([LOC-139](https://issuetracker.unity3d.com/issues/localization-using-api-key-to-retrieve-data-outputs-request-is-missing-required-authentication-credential-expected-oauth-2))
- Calling `LocalizationEditorSettings.GetLocales` will no longer create a new Addressable Assets Settings file if one does not exist. ([LOC-142](https://issuetracker.unity3d.com/issues/a-new-addresableassetsdata-folder-is-created-when-recompiling-a-script-while-the-localization-tables-window-is-open))
- Fixed a bug where the Locales list were not been updated, when the New Table tab is open and the Locale name was changed.
- Improved `LocalizationSetting` editor performance. The editor is now using UI Toolkit to render and should be much smoother. (LOC-90)
- Fixed **Smart String** editor becoming corrupted when a parsing exception was thrown in the **Debug** view.
- Fixed exception being thrown by `GetSourceExtension` and `GetFormatterExtension` when no value could be found.
- Fixed `LocalizedAsset` Property Drawer not resolving the asset type when displaying a list of `LocalizedAssets`.
- Fixed preloaded Sprite assets having the incorrect type(Texture). ([LOC-142](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-143))

## [0.9.0] - 2020-10-27

### Added
- Added support for exporting and importing `StringTables` and `StringTableCollections` as XML Localisation Interchange File Format (XLIFF). XLIFF version [1.2](https://docs.oasis-open.org/xliff/v1.2/os/xliff-core.html) and [2.0](https://docs.oasis-open.org/xliff/xliff-core/v2.0/os/xliff-core-v2.0-os.html) are both supported. See docs for further details.
- Added support for exporting and importing `StringTableCollections` as Comma Separated Values (CSV).
- Added `GetRowEnumerator` to `AssetTableCollection` and `StringTableCollection`. This can be used to step through each key and its localized values.
- Added `ReleaseAssets` to allow for forcing all `AssetTable` assets to be released. This is called automatically when the `LocalizationSettings.SelectedLocale` is changed.

### Changed
- Updated to Addressables version **1.16.6**.
- Editor Foldout fields now toggle when clicking on the label.
- When enabling preload on Asset Tables, all assets will now be preloaded by default instead of requiring the `PreloadAssetTable` Metadata. Adding a `PreloadAssetTable` can be used to disable this default behavior. ([LOC-114](https://issuetracker.unity3d.com/issues/localized-audio-clips-fail-to-load-on-first-retrieval-when-using-loadassetasync))
- `SmartFormat` editor will now show parsing and format errors in the preview field.
- Added title header to Google Sheets extension property drawer.

### Fixed
- Fixed smart `StringTableEntry` returning the wrong localized string after its value was changed.
- Fixed `LocalizedAsset` from loading when it contained an empty reference. ([LOC-109](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-109))
- Fixed `InvalidCastException` when calling `AssetTable.GetAssetAsync` and the table was preloaded. ([LOC-114](https://issuetracker.unity3d.com/issues/localized-audio-clips-fail-to-load-on-first-retrieval-when-using-loadassetasync))
- Fixed bug in `PreloadDatabaseOperation` that caused it to complete before preloading was finished. ([LOC-114](https://issuetracker.unity3d.com/issues/localized-audio-clips-fail-to-load-on-first-retrieval-when-using-loadassetasync))
- Fixed Tables Window throwing exceptions when editing a collection which had 1 or more tables without a matching locale in the project.
- Fixed `ProjectSettings.asset` being modified on disc during a build.
- Fixed `LocalizedString` property drawer throwing `NullReferenceException` when selecting a Localization Table Collection which did not have a table for every Locale in the project. ([LOC-118](https://issuetracker.unity3d.com/issues/nullreferenceexception-thrown-when-editing-a-localizedstring-linked-to-a-collection-that-does-not-have-a-table-for-every-project-locale))
- Calling `AddKey` will now check if the new Id is already in use and generate a new Id if it is. This means it is now safe to use custom Id values.
- `SharedTableData` that contains an empty Guid will now be repaired when loaded in the editor.
- Fixed `SmartFormat` editor becoming corrupt when showing preview and `ThrowException` action was enabled in ParseError or FormatError. [(LOC-119)(https://issuetracker.unity3d.com/issues/editor-ui-is-broken-when-editing-a-smart-string-and-throwexceptions-error-mode-is-enabled)]
- Increased max width for `Localization Tables` window Id field so large keys can be fully visible.

## [0.8.1] - 2020-08-19

### Fixed
- Fixed incorrect Further Reading URL in Pseudo-Localization documentation page.
- Fixed changes to tables not being saved due to them not being set dirty when edited through the Table Editor.

## [0.8.0] - 2020-08-17

### Added
- Added `IKeyGenerator` to `SharedTableData`. This is now used to generate the unique ids for each key. By default `DistributedUIDGenerator` will be used to generate the keys however it is possible to change it back to Sequential by assigning `SequentialIDGenerator` to `SharedTableData.KeyGenerator`. See docs for further details.
- Added `PreserveTags` Pseudo-localization method. This will prevent tags such as rich text or xml from being modified by any subsequent Pseudo-localization methods. ([LOC-86](https://issuetracker.unity3d.com/issues/pseudo-localization-methods-are-applied-to-the-whole-string-when-using-rich-text-tags))
- Added `LocalizeSpriteEvent` component. This can be used for Localizing `Sprite` assets.
- Added `Localize` context menu item for UGUI `Image` component. This will add and setup a `LocalizeSpriteEvent` component.
- Added `Group` property to `StringTableCollection`and `AssetTableCollection`, This can be used to customize the menu grouping when selecting a Table Collection.
- Locales can now be sorted in the Localization project settings view.
- Added `SortOrder` property to `Locale`. This can be used to override the default sorting order. This value is set automatically when changing the order in the editor.
- Added `CheckForMissingSharedTableDataEntries` to `StringTable` and `AssetTable`. This will find any entries in the table that are missing matching Shared Table Entries. These missing entries can be repaired with the `MissingEntryAction` parameter.
- Added `RemapKey` to `SharedTableData`. This will let the Id of a key be changed.
- Added `LocalizedStringTable` and `LocalizedAssetTable`. These work in a similar way to `LocalizedString` and `LocalizedAsset` but will return a Table instead of a single table entry.
- Added new Sample: Simple Language Selection Menu (IMGUI). Includes 2 different examples of a Locale selection menu.

### Changed
- Changed Table Entry Key Id data type from `uint` to `long`. All Methods that were using a `uint` now require a `long`.
- `IPseudoLocalizationMethod` method `Transform` now passes a `Message` instead of a string.
- The Locale column order in the `Localization Tables` window is now determined by the Locale's order.
- Upgraded to latest `Newtonsoft Json` package `2.0.0`.
- Renamed `LocalizedTable` to `LocalizationTable`.
- Renamed `LocalizedTableT` to `DetailedLocalizationTable`
- Renamed `LocalizedTableCollection` to `LocalizationTableCollection`
- Improved error message when a Locale can not be selected during initialization.
- Replaced `LocalizedString` `RegisterChangeHandler` and `ClearChangeHandler` with the event `StringChanged`.
- Replaced `LocalizedAsset` `RegisterChangeHandler` and `ClearChangeHandler` with the event `AssetChanged`.

### Fixed

- Fixed StringTableCollection assets not being set dirty when pulling from a Google Sheet in the editor. ([1240528](https://issuetracker.unity3d.com/issues/undo-does-not-set-assets-dirty-when-using-iserializationcallbackreceiver))
- Fixed StringTableCollection assets not being set dirty when pulling from a Google Sheet in the editor.
- Fixed `LocalizedString.GetLocalizedString` not using the argument provided in `LocalizedString.Arguments`.
- Fixed serialization errors `Should not occur! Internal logic error: please report bug.` when `Comment Metadata` was attached to the table or shared table data. (LOC-104)
- Fixed `LocalizedDatabase.GetTableAsync` trying to load an already loaded `LocalizedTable` when using a GUID TableReference. This was causing preloaded tables to still require some loading.
- Fixed "LocalizationSettings has a different serialization layout" error. Moved the `Locale GameView Menu` property from LocalizationSettings to EditorPrefs and moved the menu option into `Preference/Localization`. (LOC-99)
- Fixed `GoogleSheets` push not handling Table entries that were missing matching Shared Table Entries, this would cause future rows to be blank. ([LOC-102](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-102))
- Fixed `NullReferenceException` in `Localization Settings` when Editor window focus was lost and Smart Format properties field was visible. ([LOC-101](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-101))
- Fixed `Available Locales` not updating when performing an Undo/Redo after adding or removing a Locale.
- `LocalizedString` and `LocalizedAsset` property drawers now remember the expanded state of each Locale field when an Undo/Redo occurs. ([LOC-105](https://issuetracker.unity3d.com/issues/undoing-a-change-to-a-language-in-the-localizedstring-inspector-resets-the-folded-state))
- Added Undo support when adding and removing a table from a `StringTableCollection` or `AssetTableCollection`. When Undo is not used the asset will be set dirty so changes are not lost. ([LOC-107](https://issuetracker.unity3d.com/product/unity/issues/guid/LOC-107))

## [0.7.1] - 2020-06-05

### Added
- Added `StringTableCollection` and `AssetTableCollection`. These are editor only assets for controlling a collection of string or asset tables. Tables that are not part of a collection will now be ignored. To upgrade old table assets select one and press the `Create Collection` button in the inspector.
- Added Google Sheets support. A `StringTableCollection` can by pushed and pulled by using the `Google Sheets Extension` in the editor or through script with the `GoogleSheets` class. Table entry values, comments and metadata can be synced to a Google Sheet. This is currently an Editor only feature.
- Added `DisplayName` attribute. This is an optional attribute for `Metadata` and `CollectionExtensions` so that a custom name that can be displayed in the editor instead of the class name.
- Added `LocalizationSettings.SelectedLocaleAsync`. This is a safer alternative to `LocalizationSettings.SelectedLocale` as it will ensure that the `AvailableLocales` is initialized.

### Changed
- Renamed `LocalizeStringBehaviour` to `LocalizeStringEvent`.
- Renamed `LocalizeAudioClipBehaviour` to `LocalizeAudioClipEvent`.
- Renamed `LocalizeTextureBehaviour` to `LocalizeTextureEvent`.
- Renamed `TableName` to `TableCollectionName`.
- The editor class `AssetTableCollection` has been repurposed. Please see the new `StringTableCollection` and `AssetTableCollection` assets.
- `LocalizationEditorSettings` has changed:
  - Replaced `ModificationEvent` with `EditorEvents` class
  - `AddAssetToTable` and `RemoveAssetFromTable` have been moved to `AssetTableCollection`
  - Added `GetStringTableCollection`, `GetStringTableCollections`, `GetAssetTableCollection` and `GetAssetTableCollections`.
  - Removed `RemoveTable`, `GetAssetTables`, `GetAssetTablesCollection`. These features are replaced by  `StringTableCollection` and `AssetTableCollection`.
  - Simplified `ILocalesProvider` interface Only 1 `GetLocale` function is now required.
  - Updated code Samples to include Creating Locales, Loading Strings, Google Sheets and a Language selection menu example.
  
### Fixed
- Fixed `Asset Tables Window` throwing `NullReferenceException` when editing Metadata for an entry that does not exist in the selected table.
- Fixed incorrect installation instructions in documentation.
- Fixed `SerializedTableEntryReference` not handling key names correctly, which could cause issues with LocalizedString and LocalizedAsset. (case 1230444)
- Fixed `TableReference` returning invalid value for TableName when type is Guid.
- Fixed `UpdateString` not being called When a new `LocalizedString` was assigned to `LocalizeStringEvent.StringReference`.
- Fixed `UpdateAsset` not being called When a new `LocalizedAsset` was assigned to `LocalizeStringEvent.AssetReference`.
- Fixed `LocalizedString` and `LocalizedAsset` trying to localize when they are empty. (case LOC-88)
- Localized String editor now clips Locale labels instead of letting them overlap into the preview label. (case LOC-84)
- Fixed Available Locales List is not updating when a Locale is added or removed from the project. (case LOC-79)
- Fixed `LocalizedAsset` and `LocalizedString` editor not updating when a Locale is added or removed from the project.
- Fixed `SharedTableData` Metadata child properties not being visible or editable in the Table Window. (case LOC-85)
- Fixed Smart Format toggle field overflowing into Metadata button when Table Window columns were narrow. (case LOC-95)
- Fixed `LocalizedStringDatabase` and `LocalizedAssetDatabase` using the `SelectedLocale` before `AvailableLocales` was initialized. (case 1225937)
- Fixed Asset Tables losing data due to not being set dirty when a change was made. (case LOC-82)
- Fixed ListFormatter Names property going null in player builds.

## [0.6.1] - 2020-02-18

### Added
- Added IEquatable to LocaleIdentifier.

### Changed
- Changed collection properties to be read only (rule CA2227).
- Made AddressableEntryNotFoundException public.

### Fixed
- SystemLocaleSelector will now check the CultureInfo before checking Application.SystemLanguage to allow for selecting regional languages first.
- Fixed `LocalizedAssetTexture` and `LocalizeAudioClipBehaviour` not serializing the asset reference field. (case 1216072)

## [0.6.0] - 2020-01-27

### Added
- Added PseudoLocale and Pseudo-localization methods(Accenter, CharacterSubstitutor, Encapsulator and Expander). This is used to generate Pseudo-localization using various methods during runtime(See docs for further info).
- Added support for loading multiple Locales from String and Asset Databases. By Default the `LocalizationSettings.SelectedLocale` will be used however it is now possible to provide a Locale as an argument. This means Localization data for multiple Locales can exist which allows for fallback Locales or just showing multiple languages at once.
- Added exception to LocalizedTable.TableName if the SharedTableData is null.
- Added `Locale Generator` button to `New Table` tab.

### Changed
- Renamed `KeyDatabase` to `SharedTableData`.
- Changed `Comment` metadata TimeStamp property type to DateTime.
- Disabled Addressables Settings asset creation during Localization OnPostprocessAllAssets. This could cause corruption of Addressables data due to OnPostprocessAllAssets execution order when importing a project for the first time.
- Updated to Addressables version *1.5.1*.
- Addressable groups now use the NoHash BundleNamingStyle by default.

### Fixed
- Fixed `NullReferenceException` sometimes being thrown by LocalizedString or LocalizedAsset during *ClearLoadingOperation*.
- Fixed `NullReferenceException` when adding SmartFormatter Source in String Database inspector.
- Fixed `ArgumentOutOfRangeException:` in StringTables when enabling a Missing Tables column.
- Fixed LocalizedReference field label overflowing when a selected key had multiple lines.
- Fixed LocalizedString property drawer adding too much vertical whitespace.
- Fixed items in Reorderable Lists ignoring the right border.
- Fixed LocalizedTableT entry Id not being updated when entry was moved or removed.
- Fixed GameViewLanguageMenu causing LocalizationSettings to Initialize when there was no Addressables/Locales in the project.
- Fixed Key name being unchangeable when its name was empty in the Asset Tables window.
- Fixed Search field border being too small in the Asset Tables Window.
- Fixed `New Table - Create` button being enabled when no locales were selected or existed in the project.
- Fixed `LocalizedString` and `LocalizedAsset` not automatically updating when the `TableReference` or `TableEntryReference` was changed and a `ChangeHandler` was being used.
- Fixed Addressable flags including `Preload` being removed during AddOrUpdateTable.

## [0.5.1] - 2019-11-22
- Updated to Addressables version *1.3.8*.
- Fixed `ArgumentOutOfRangeException:` in the Tables window when only 1 string or asset table existed.
- Exposed *CreateAssetTableCollection* in *LocalizationEditorSettings*.
- Implemented *IDictionary* to *LocalizedTableT* and Removed *TableEntries* property.
- Fixed LocalizedTableT not serializing changes made when using the Dictionary interface.
- Added *LocalizedTableT.CreateTableEntry*, this should be used when creating a *StringTableEntry* or *AssetTableEntry*.
- *ArgumentNullException* is now thrown when using a null ChangeHandler with *LocalizedString.RegisterChangeHandler* or *LocalizedAsset.RegisterChangeHandler*.
- Fixed *PluralLocalizationFormatter* using the Selected Locale when a CultureInfo provider was used.
- Current loading operations are now cleared when using *ClearChangeHandler* with *LocalizedAsset* and *LocalizedString*
- Added default constructor to FallbackLocale Metadata.
- Fixed error spamming when adding Metadata that does not have a default constructor.
- Removed missing image from LocalizationTablesWindow manual page.

## [0.5.0] - 2019-11-04
- Updated to Addressables version *1.2.4*.
- Removed type specific asset tables. The AssetTable class can now hold mixed types per entry.
- Removed Custom Locale support from Locale Generator Window. You should use Metadata to modify the Locale instead.
- Removed Preload behaviour from Localization Settings. Preload behaviour can now be controlled per table through the Asset Tables Window.
- Removed Addressable tests. These would cause issues when run in a project already using Addressables. They have been moved to an internal test suite.
- Removed *Key Database* field from *New Table* panel. Key databases should not be shared now as they act as the Table Name Guid.
- Added Metadata to Locale, LocalizedTable and TableEntry. Metadata can be used to add additional data that implements the IMetadata interface and is serializable.
- Added PreloadAssetTable Metadata. Add this to an AssetTable to configure the preload behaviour for individual assets contained in the table. By default the assets will not be preloaded if this is not present.
- Added PreloadAsset Metadata. Add this to Asset Table entries when using PreloadBehaviour.PreloadSelected to select the entry for preloading.
- Added Comment Metadata. This can be added to any Metadata to provide comments. In the future these comments will be extracted when exporting to external formats.
- Added SmartFormat for StringTables. This allows for advanced formatting of strings. Comes with support for named placeholders, lists, pluralization, gender, code reflection support and much more.
- Added Table Name Guid. Table Names can now be referenced by a Guid so that changes to the name do not break references.
- Fixed Locale Generator Window button `Generate Locales` staying disabled when using `Select All`.
- LocaleGeneratorListView now shows Locales that are already in the project.
- New Locales are now added to Addressables when created.
- Changed the name format for Tables for Addressables and asset creation to start with the name followed by the locale.
- Improved LocalizedString editor. It is now possible to modify the selected entry details in the Inspector without opening the Asset Tables Window.
- Moved Localization Table editor window option to *Window/Asset Management/Localization Tables*.

## [0.4.1] - 2019-07-09
- Updated to Addressables version '1.1.4-preview'.

## [0.4.0] - 2019-06-14
- Added AutoUpdate to LocalizedStringReference class. When enabled, the string will automatically be loaded any time a change is detected and the UpdateString event can be used to update the string. With AutoUpdate enabled calling GetLocalizedString is not necessary.
- Fixed StringTableEditor changes being lost when using inline editing.
- Fixed LocalizationBehaviour not updating the first time when LocalizationSettings are already initialized.
- Fixed LocalizationBehaviour not updating correctly if the asset or table has already been loaded.
- Improved drop down label in LocalizedStringReferencePropertyDrawer. Multi-line text is now correctly shown. (LOC-57)
- Added KeyDatabase.FindSimilarKey. Returns the closest match using a Levenshtein approximation.
- Improved key matching when using Localize context menu for Text components.
- Updated to Addressables version '0.8.6-preview'.

## [0.3.2] - 2019-05-09
- Added support for ugui package in 2019.2.0a15. ugui support is optional and will be enabled automatically if the ugui package is in the project.
- Removed support for 2018.3. The ugui support requires optional assemblies which are not supported in 2018.3.
- AssemblyScannerCache now uses UnityEditor.TypeCache in 2019.2 which should provide improved performance in Editor.
- Added an error when adding non-persistent assets to asset tables.
- Fixed StringTableEditor changes being lost due to not setting the table dirty.

## [0.3.1] - 2019-05-02
- Added KeyDatabase. All string keys are now contained within this and mapped to a unique(to the database) id value which is used by the table.
- Assets are now grouped in Addressables by the locale. This allows you to serve them on-demand for a particular language, instead of bundling them with the game release. Shared assets are stored in a separate group.
- Assets are now removed from Addressables when they are not referenced by any Asset Tables.
- Fixed UIElements issues in 2019.1+.
- Merged LocalizationPlayerSettings into LocalizationEditorSettings.
- Removed AddressableAssetTableT.GetAsset. GetAssetAsync should be used instead.
- Added option in `Asset Tables Windows` to add a missing table if a table does not exist for all available Locales.
- Fixed UIElements compilation issues in 2019.1+
- Updated to Addressables version '0.7.5-preview'.
- Removed support for .Net 3.5, this is due to a limitation in Addressables.

## [0.2.5] - 2019-03-01
- Updated to Addressables version '0.6.7-preview'.

## [0.2.3] - 2019-02-20
- Fixed adding Locales and Tables to Addressables not working when the Addressables settings asset did not exist(LOC-24).
- Fixed exception when changing a table name in the Table Editor window.
- Fixed AssetTablesField exception when no Asset Tables exist in the project.
- Updated to Addressables version '0.5.3-preview'.

## [0.2.2] - 2018-12-17
- Fixed NullRefException when opening the Asset Tables window and no Asset Tables exist in the project

## [0.2.1] - 2018-12-14
- Removed stray file `README - External.md`

## [0.2.0] - 2018-12-11

### This is the first release of *Unity Package Localization*.
- Contains preview of asset and string localization support.
