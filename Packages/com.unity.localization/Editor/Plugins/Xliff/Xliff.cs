using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Localization.Plugins.XLIFF.Common;
using UnityEditor.Localization.Reporting;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;

namespace UnityEditor.Localization.Plugins.XLIFF
{
    /// <summary>
    /// XML Localisation Interchange File Format.
    /// The purpose of XLIFF is to store localizable data and carry it from one step of the localization process to the other,
    /// while allowing interoperability between and among tools.
    /// </summary>
    public static class Xliff
    {
        /// <summary>
        /// Determines how notes should be handled when importing XLIFF data.
        /// </summary>
        public enum ImportNotesBehavior
        {
            /// <summary>
            /// Does nothing with the notes and comments.
            /// </summary>
            Ignore,

            /// <summary>
            /// Default behavior. Replaces all comments with notes.
            /// </summary>
            Replace,

            /// <summary>
            /// Attempts to merge the notes with existing comments by checking the comment contents.
            /// Comments that have the same text as a note will be ignored.
            /// </summary>
            Merge,
        }

        /// <summary>
        /// Optional import options which can be used to configure the importing behavior.
        /// </summary>
        public class ImportOptions
        {
            /// <summary>
            /// Should the source language tables be updated using <see cref="ITranslationUnit.Source"/>?
            /// </summary>
            public bool UpdateSourceTable { get; set; } = true;

            /// <summary>
            /// Should the target language tables be updated using <see cref="ITranslationUnit.Target"/>?
            /// </summary>
            public bool UpdateTargetTable { get; set; } = true;

            /// <summary>
            /// Where to create new <see cref="StringTableCollection"/>'s when importing and a matching collection could not be found.
            /// If empty a save prompt will be shown.
            /// </summary>
            public string NewCollectionDirectory { get; set; }

            /// <summary>
            /// Controls how notes will be imported.
            /// </summary>
            public ImportNotesBehavior ImportNotes { get; set; } = ImportNotesBehavior.Replace;
        }

        static readonly ImportOptions k_DefaultOptions = new ImportOptions();

        /// <summary>
        /// Exports all <see cref="StringTable"/> in <paramref name="collections"/> as 1 or more XLIFF files where each file represents a single language.
        /// </summary>
        /// <param name="source">This is the language that will be used as the source language for all generated XLIFF files.</param>
        /// <param name="directory">The directory to output the generated XLIFF files.</param>
        /// <param name="name">The default name for all generated XLIFF files. Files will be saved with the full name "[name]_[Language Code].xlf"</param>
        /// <param name="version">The XLIFF version to generate the files in.</param>
        /// <param name="collections">1 or more <see cref="StringTableCollection"/>. The collections will be combines into language groups where each file represents a single </param>
        /// <param name="reporter">Optional reporter which can report the current progress.</param>
        public static void Export(LocaleIdentifier source, string directory, string name, XliffVersion version, ICollection<StringTableCollection> collections, ITaskReporter reporter = null)
        {
            if (collections == null)
                throw new ArgumentNullException(nameof(collections));

            var dict = new Dictionary<StringTableCollection, HashSet<int>>();
            foreach (var c in collections)
            {
                dict[c] = new HashSet<int>(Enumerable.Range(0, c.StringTables.Count));
            }

            ExportSelected(source, directory, name, version, dict, reporter);
        }

        /// <summary>
        /// Export the values in <paramref name="tables"/> using <paramref name="sourceLanguage"/> as the source language to one or more XLIFF files.
        /// </summary>
        /// <param name="sourceLanguage">This is the table that will be used as the source language for all generated XLIFF files.</param>
        /// <param name="directory">The directory where all generated XLIFF files will be saved to.</param>
        /// <param name="version">The XLIFF version to generate the files in.</param>
        /// <param name="tables">1 or more <see cref="StringTable"/> that will be used as the target language for each XLIFF file. 1 XLIFF file will be generated for each table.</param>
        /// <param name="reporter">Optional reporter which can report the current progress.</param>
        public static void Export(StringTable sourceLanguage, string directory, XliffVersion version, ICollection<StringTable> tables, ITaskReporter reporter = null)
        {
            if (sourceLanguage == null)
                throw new ArgumentNullException(nameof(sourceLanguage));
            if (tables == null)
                throw new ArgumentNullException(nameof(tables));

            try
            {
                // Used for reporting
                float taskStep = 1.0f / (tables.Count * 2.0f);
                float progress = 0;
                if (reporter != null && reporter.Started != true)
                    reporter.Start($"Exporting {tables.Count} String Tables to XLIFF", string.Empty);

                // We need the key, source value and translated value.
                foreach (var stringTable in tables)
                {
                    reporter?.ReportProgress($"Exporting {stringTable.name}", progress);
                    progress += taskStep;

                    var doc = CreateDocument(sourceLanguage.LocaleIdentifier, stringTable.LocaleIdentifier, version);
                    AddTableToDocument(doc, sourceLanguage, stringTable);

                    var cleanName = CleanFileName(stringTable.name);
                    var fileName = $"{cleanName}.xlf";
                    var filePath = Path.Combine(directory, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        doc.Serialize(stream);
                    }
                }
                reporter?.Completed($"Finished exporting");
            }
            catch (Exception e)
            {
                reporter?.Fail(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates an empty XLIFF document ready for populating.
        /// </summary>
        /// <param name="source">The source language. The language used when populating <see cref="ITranslationUnit.Source"/>.</param>
        /// <param name="target">The target language. The language used when populating <see cref="ITranslationUnit.Target"/>.</param>
        /// <param name="version">The XLIFF file version.</param>
        /// <returns></returns>
        public static IXliffDocument CreateDocument(LocaleIdentifier source, LocaleIdentifier target, XliffVersion version)
        {
            var doc = XliffDocument.Create(version);
            doc.SourceLanguage = source.Code;
            doc.TargetLanguage = target.Code;
            return doc;
        }

        /// <summary>
        /// Populate the document with the entries from <paramref name="target"/> using <paramref name="source"/> as the source reference.
        /// Note: The source and target tables must be part of the same collection, they must both use the same <see cref="SharedTableData"/>.
        /// </summary>
        /// <param name="document">The XLIFF document to add the entries to.</param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public static void AddTableToDocument(IXliffDocument document, StringTable source, StringTable target)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (source.SharedData != target.SharedData)
                throw new Exception("Source and Target StringTables must be part of the same collection and use the same SharedTableData.");

            var file = document.AddNewFile();
            var filePath =  AssetDatabase.GetAssetPath(target);
            file.Original = filePath;
            file.Id = AssetDatabase.AssetPathToGUID(filePath);

            var group = file.AddNewGroup();
            group.Id = TableReference.StringFromGuid(target.SharedData.TableCollectionNameGuid);
            group.Name = target.SharedData.TableCollectionName;

            AddNotesFromMetadata(group, target.SharedData.Metadata, NoteType.General);
            AddNotesFromMetadata(group, source, NoteType.Source);

            if (source != target)
                AddNotesFromMetadata(group, target, NoteType.Target);

            foreach (var row in StringTableCollection.GetRowEnumerator(source, target))
            {
                if (row.TableEntries[0] != null && row.TableEntries[0].SharedEntry.Metadata.HasMetadata<ExcludeEntryFromExport>())
                    continue;

                var unit = group.AddNewTranslationUnit();

                unit.Id = row.KeyEntry.Id.ToString();
                unit.Name = row.KeyEntry.Key;
                unit.Source = row.TableEntries[0]?.Value;

                // Dont add a value if its empty.
                if (row.TableEntries[1] != null && !string.IsNullOrEmpty(row.TableEntries[1].Value) && !string.IsNullOrEmpty(row.TableEntries[1].Key))
                    unit.Target = row.TableEntries[1].Value;

                // Add notes
                AddNotesFromMetadata(unit, row.KeyEntry.Metadata, NoteType.General);
                AddNotesFromMetadata(unit, row.TableEntries[0], NoteType.Source);

                if (source != target)
                    AddNotesFromMetadata(unit, row.TableEntries[1], NoteType.Target);
            }
        }

        static void AddNotesFromMetadata(INoteCollection noteCollection, IMetadataCollection metadata, NoteType noteType)
        {
            // May be null if the entry is missing for the current row
            if (metadata == null)
                return;

            using (ListPool<Comment>.Get(out var comments))
            {
                metadata.GetMetadatas<Comment>(comments);
                foreach (var com in comments)
                {
                    var note = noteCollection.AddNewNote();
                    note.AppliesTo = noteType;
                    note.NoteText = com.CommentText;
                }
            }
        }

        /// <summary>
        /// Imports all XLIFF files with the extensions xlf or xliff into existing <see cref="StringTableCollection"/> or new ones if a matching one could not be found.
        /// </summary>
        /// <param name="directory">The directory to search. Searches sub directories as well.</param>
        /// <param name="importOptions">Optional import options which can be used to configure the importing behavior.</param>
        /// <param name="reporter">Optional reporter which can report the current progress.</param>
        public static void ImportDirectory(string directory, ImportOptions importOptions = null, ITaskReporter reporter = null)
        {
            try
            {
                if (reporter != null && reporter.Started != true)
                    reporter.Start("Importing XLIFF files in directory", "Finding xlf and xliff files.");

                var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                var filteredFiles = files.Where(s => s.EndsWith(".xlf") || s.EndsWith(".xliff"));

                float taskStep = filteredFiles.Count() / 1.0f;
                float progress = taskStep;

                foreach (var f in filteredFiles)
                {
                    reporter?.ReportProgress($"Importing {f}", progress);
                    progress += taskStep;

                    // Don't pass the reporter in as it will be Completed after each file and we only want to do that at the end.
                    ImportFile(f, importOptions);
                }
                reporter?.Completed("Finished importing XLIFF files");
            }
            catch (Exception e)
            {
                reporter?.Fail(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Imports a single XLIFF file into the project.
        /// Attempts to find matching <see cref="StringTableCollection"/>'s, if one could not be found then a new one is created.
        /// </summary>
        /// <param name="file">The XLIFF file.</param>
        /// <param name="importOptions">Optional import options which can be used to configure the importing behavior.</param>
        /// <param name="reporter">Optional reporter which can report the current progress.</param>
        public static void ImportFile(string file, ImportOptions importOptions = null, ITaskReporter reporter = null)
        {
            if (reporter != null && reporter.Started != true)
                reporter.Start("Importing XLIFF", $"Importing {file}");
            try
            {
                if (!File.Exists(file))
                    throw new FileNotFoundException($"Could not find file {file}");

                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    reporter?.ReportProgress("Parsing XLIFF", 0.1f);
                    var document = XliffDocument.Parse(stream);
                    ImportDocument(document, importOptions, reporter);
                }
            }
            catch (Exception e)
            {
                reporter?.Fail(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Imports a single XLIFF document into the project.
        /// Attempts to find matching <see cref="StringTableCollection"/>'s, if one could not be found then a new one is created.
        /// </summary>
        /// <param name="document">The root XLIFF document.</param>
        /// <param name="importOptions">Optional import options which can be used to configure the importing behavior.</param>
        /// <param name="reporter">Optional reporter which can report the current progress.</param>
        public static void ImportDocument(IXliffDocument document, ImportOptions importOptions = null, ITaskReporter reporter = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (reporter != null && reporter.Started != true)
                reporter.Start("Importing XLIFF", "Importing document");

            try
            {
                float progress = reporter == null ? 0.1f : reporter.CurrentProgress + 0.1f;
                reporter?.ReportProgress("Importing XLIFF into project", progress);

                float progressStep = document.FileCount / (1.0f - progress);
                var options = importOptions ?? k_DefaultOptions;
                for (int i = 0; i < document.FileCount; ++i)
                {
                    var f = document.GetFile(i);
                    progress += progressStep;
                    reporter?.ReportProgress($"Importing({i + 1}/{document.FileCount}) {f.Id}", progress);
                    ImportFileNode(f, document.SourceLanguage, document.TargetLanguage, options);
                }

                reporter?.Completed("Finished importing XLIFF");
            }
            catch (Exception e)
            {
                reporter?.Fail(e.Message);
                throw;
            }
        }

        static void ImportFileNode(IFile file, LocaleIdentifier source, LocaleIdentifier target, ImportOptions importOptions)
        {
            // Find the string table and collection for this file
            var collection = FindProjectCollection(file);

            // Import translation units which have no groups.
            INoteCollection extraNodes = file;
            if (file.TranslationUnitCount > 0)
            {
                if (collection == null)
                {
                    var dir = importOptions.NewCollectionDirectory;
                    if (string.IsNullOrEmpty(dir))
                        dir = EditorUtility.SaveFolderPanel($"Create new String Table Collection {file.Id}", "Assets", file.Id);

                    if (!string.IsNullOrEmpty(dir))
                    {
                        var newCollection = LocalizationEditorSettings.CreateStringTableCollection(file.Id, dir);
                        extraNodes = null;
                        ImportFileIntoCollection(newCollection, file, source, target, importOptions);
                    }
                }
                else
                {
                    extraNodes = null;
                    ImportFileIntoCollection(collection, file, source, target, importOptions);
                    collection.SaveChangesToDisk();
                }
            }

            for (int i = 0; i < file.GroupCount; ++i)
            {
                var group = file.GetGroup(i);
                var groupCollection = FindProjectCollection(group) ?? collection;
                if (groupCollection == null)
                {
                    // Use the provided directory otherwise ask the user to provide one
                    var dir = importOptions.NewCollectionDirectory;
                    if (string.IsNullOrEmpty(dir))
                    {
                        dir = EditorUtility.SaveFolderPanel($"Create new String Table Collection {file.Id}", "Assets", file.Id);
                        if (string.IsNullOrEmpty(dir))
                            continue;
                    }
                    var collectionName = string.IsNullOrEmpty(group.Name) ? group.Id : group.Name;
                    groupCollection = LocalizationEditorSettings.CreateStringTableCollection(collectionName, dir);
                }

                ImportGroupIntoCollection(groupCollection, group, extraNodes, source, target, importOptions);
                groupCollection.SaveChangesToDisk();
            }
        }

        static StringTableCollection FindProjectCollection(IFile file)
        {
            // When exporting we use the ID for the file GUID.
            string path = AssetDatabase.GUIDToAssetPath(file.Id);

            var filePath = string.IsNullOrEmpty(path) ? file.Original : path;
            if (!string.IsNullOrEmpty(filePath))
            {
                var table = AssetDatabase.LoadAssetAtPath<StringTable>(filePath);
                if (table != null)
                    return LocalizationEditorSettings.GetCollectionFromTable(table) as StringTableCollection;
            }

            return null;
        }

        static StringTableCollection FindProjectCollection(IGroup group)
        {
            // Is the Id the Shared table data GUID?
            if (!string.IsNullOrEmpty(group.Id))
            {
                var path = AssetDatabase.GUIDToAssetPath(group.Id);
                if (!string.IsNullOrEmpty(path))
                {
                    var sharedTableData = AssetDatabase.LoadAssetAtPath<SharedTableData>(path);
                    if (sharedTableData != null)
                        return LocalizationEditorSettings.GetCollectionForSharedTableData(sharedTableData) as StringTableCollection;
                }
            }

            // Try table name instead
            return LocalizationEditorSettings.GetStringTableCollection(group.Id) ?? LocalizationEditorSettings.GetStringTableCollection(group.Name);
        }

        /// <summary>
        /// Import the XLIFF file into the collection.
        /// </summary>
        /// <param name="collection">The collection to import all the XLIFF data into.</param>
        /// <param name="file">The XLIFF file path.</param>
        /// <param name="importOptions">Optional import options which can be used to configure the importing behavior.</param>
        /// <param name="reporter">Optional reporter which can report the current progress.</param>
        public static void ImportFileIntoCollection(StringTableCollection collection, string file, ImportOptions importOptions = null, ITaskReporter reporter = null)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (reporter != null && reporter.Started != true)
                reporter.Start("Importing XLIFF", $"Importing {file}");
            try
            {
                if (!File.Exists(file))
                    throw new FileNotFoundException($"Could not find file {file}");

                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    reporter?.ReportProgress("Parsing XLIFF", 0.1f);
                    var document = XliffDocument.Parse(stream);

                    float progress = 0.3f;
                    reporter?.ReportProgress("Importing XLIFF into project", progress);

                    float progressStep = document.FileCount / 1.0f * 0.7f;
                    var options = importOptions ?? k_DefaultOptions;
                    for (int i = 0; i < document.FileCount; ++i)
                    {
                        var f = document.GetFile(i);
                        progress += progressStep;
                        reporter?.ReportProgress($"Importing({i + 1}/{document.FileCount}) {f.Id}", progress);
                        ImportFileIntoCollection(collection, f, document.SourceLanguage, document.TargetLanguage, options);
                    }

                    reporter?.Completed("Finished importing XLIFF");
                }
            }
            catch (Exception e)
            {
                reporter?.Fail(e.Message);
                throw;
            }
        }

        static void ImportFileIntoCollection(StringTableCollection collection, IFile file, LocaleIdentifier source, LocaleIdentifier target, ImportOptions importOptions)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            var sourceTable = collection.GetTable(source) ?? collection.AddNewTable(source);
            var targetTable = collection.GetTable(target) ?? collection.AddNewTable(target);

            // Extract file comments?
            AddMetadataCommentsFromNotes(file, collection.SharedData.Metadata, NoteType.General, importOptions.ImportNotes);
            AddMetadataCommentsFromNotes(file, sourceTable, NoteType.Source, importOptions.ImportNotes);

            if (sourceTable != targetTable)
                AddMetadataCommentsFromNotes(file, targetTable, NoteType.Target, importOptions.ImportNotes);

            ImportIntoTables(file, sourceTable as StringTable, targetTable as StringTable, importOptions);

            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
        }

        static void ImportGroupIntoCollection(StringTableCollection collection, IGroup group, INoteCollection extraNotes, LocaleIdentifier source, LocaleIdentifier target, ImportOptions importOptions)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            var sourceTable = collection.GetTable(source) ?? collection.AddNewTable(source);
            var targetTable = collection.GetTable(target) ?? collection.AddNewTable(target);

            // Extract file comments?
            var generalNotes = AddMetadataCommentsFromNotes(group, collection.SharedData.Metadata, NoteType.General, importOptions.ImportNotes);
            var sourceNotes = AddMetadataCommentsFromNotes(group, sourceTable, NoteType.Source, importOptions.ImportNotes);
            int targetNotes = sourceTable != targetTable ? AddMetadataCommentsFromNotes(group, targetTable, NoteType.Target, importOptions.ImportNotes) : 0;

            // If we are importing a group and the file contains notes that were not used then we can include them as extras here.
            if (extraNotes != null)
            {
                // If we imported some notes from the group then we need to switch to merge or we will lose those notes.
                var overrideBehavior = generalNotes > 0 ? ImportNotesBehavior.Merge : importOptions.ImportNotes;
                AddMetadataCommentsFromNotes(extraNotes, collection.SharedData.Metadata, NoteType.General, overrideBehavior);

                overrideBehavior = sourceNotes > 0 ? ImportNotesBehavior.Merge : importOptions.ImportNotes;
                AddMetadataCommentsFromNotes(extraNotes, sourceTable, NoteType.Source, overrideBehavior);

                overrideBehavior = targetNotes > 0 ? ImportNotesBehavior.Merge : importOptions.ImportNotes;
                if (sourceTable != targetTable)
                    AddMetadataCommentsFromNotes(extraNotes, targetTable, NoteType.Target, overrideBehavior);
            }

            ImportIntoTables(group, sourceTable as StringTable, targetTable as StringTable, importOptions);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
        }

        static void ImportIntoTables(ITranslationUnitCollection unitCollection, StringTable source, StringTable target, ImportOptions importOptions = null)
        {
            var options = importOptions ?? k_DefaultOptions;
            var sharedTableData = target.SharedData;

            EditorUtility.SetDirty(sharedTableData);

            if (importOptions.UpdateSourceTable)
                EditorUtility.SetDirty(source);

            if (importOptions.UpdateTargetTable)
                EditorUtility.SetDirty(target);

            for (int i = 0; i < unitCollection.TranslationUnitCount; ++i)
            {
                var tu = unitCollection.GetTranslationUnit(i);
                var sharedTableEntry = GetOrCreateEntryFromTranslationUnit(sharedTableData, tu);
                AddMetadataCommentsFromNotes(tu, sharedTableEntry.Metadata, NoteType.General, options.ImportNotes);

                if (options.UpdateSourceTable)
                {
                    var sourceEntry = source.AddEntry(sharedTableEntry.Id, tu.Source);
                    AddMetadataCommentsFromNotes(tu, sourceEntry, NoteType.Source, options.ImportNotes);
                }

                if (options.UpdateTargetTable)
                {
                    var targetEntry = target.AddEntry(sharedTableEntry.Id, tu.Target);
                    AddMetadataCommentsFromNotes(tu, targetEntry, NoteType.Target, options.ImportNotes);
                }
            }

            // Nested groups
            if (unitCollection is IGroupCollection groupCollection)
            {
                for (int i = 0; i < groupCollection.GroupCount; ++i)
                {
                    var group = groupCollection.GetGroup(i);
                    ImportIntoTables(group, source, target, options);
                }
            }
        }

        /// <summary>
        /// Import an XLIFF file into the target table, ignoring <see cref="IXliffDocument.TargetLanguage"/>.
        /// </summary>
        /// <param name="file">The XLIFF file path.</param>
        /// <param name="target">The target table that will be populated with the translated values.</param>
        /// <param name="importNotesBehavior">How should the notes be imported?</param>
        /// <param name="reporter">Optional reporter which can report the current progress.</param>
        public static void ImportFileIntoTable(string file, StringTable target, ImportNotesBehavior importNotesBehavior = ImportNotesBehavior.Replace, ITaskReporter reporter = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (reporter != null && reporter.Started != true)
                reporter.Start("Importing XLIFF", $"Importing {file}");
            try
            {
                if (!File.Exists(file))
                    throw new FileNotFoundException($"Could not find file {file}");

                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    reporter?.ReportProgress("Parsing XLIFF", 0.1f);
                    var document = XliffDocument.Parse(stream);
                    ImportDocumentIntoTable(document, target, importNotesBehavior, reporter);
                }
            }
            catch (Exception e)
            {
                reporter?.Fail(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Import an XLIFF document into the target table, ignoring <see cref="IXliffDocument.TargetLanguage"/>.
        /// </summary>
        /// <param name="document">The XLIFF document to import.</param>
        /// <param name="target">The target table that will be populated with the translated values.</param>
        /// <param name="importNotesBehavior">How should the notes be imported?</param>
        /// <param name="reporter">Optional reporter which can report the current progress.</param>
        public static void ImportDocumentIntoTable(IXliffDocument document, StringTable target, ImportNotesBehavior importNotesBehavior = ImportNotesBehavior.Replace, ITaskReporter reporter = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            EditorUtility.SetDirty(target);

            float progress = reporter == null ? 0 : reporter.CurrentProgress + 0.1f;
            reporter?.ReportProgress("Importing XLIFF into table", progress);

            float progressStep = document.FileCount / (1.0f - progress);

            var options = new ImportOptions { UpdateSourceTable = false, ImportNotes = importNotesBehavior };
            for (int i = 0; i < document.FileCount; ++i)
            {
                var f = document.GetFile(i);
                progress += progressStep;
                reporter?.ReportProgress($"Importing({i + 1}/{document.FileCount}) {f.Id}", progress);
                ImportIntoTables(f, null, target, options);
            }

            var collection = LocalizationEditorSettings.GetCollectionFromTable(target);
            if (collection != null)
            {
                LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(document, collection);
                collection.SaveChangesToDisk();
            }

            reporter?.Completed("Finished importing XLIFF");
        }

        static SharedTableData.SharedTableEntry GetOrCreateEntryFromTranslationUnit(SharedTableData sharedTableData, ITranslationUnit unit)
        {
            // Does it contain an id?
            long keyId = SharedTableData.EmptyId;
            string name = null;
            if (!string.IsNullOrEmpty(unit.Id))
            {
                if (long.TryParse(unit.Id, out keyId))
                {
                    var entry = sharedTableData.GetEntry(keyId);
                    if (entry != null)
                        return entry;
                }
                else
                {
                    // Is the Id a name?
                    var entry = sharedTableData.GetEntry(unit.Id);
                    if (entry != null)
                        return entry;
                    name = unit.Id;
                }
            }

            // Use the name
            if (!string.IsNullOrEmpty(unit.Name))
            {
                var entry = sharedTableData.GetEntry(unit.Name);
                if (entry != null)
                    return entry;
                name = unit.Name;
            }

            // Create a new entry
            if (keyId != SharedTableData.EmptyId)
                return sharedTableData.AddKey(name, keyId);
            return sharedTableData.AddKey(name);
        }

        static int AddMetadataCommentsFromNotes(INoteCollection notes, IMetadataCollection metadata, NoteType requiredNoteType, ImportNotesBehavior importNotes)
        {
            if (importNotes == ImportNotesBehavior.Ignore)
                return 0;

            int count = 0;
            using (ListPool<Comment>.Get(out var comments))
            {
                metadata.GetMetadatas<Comment>(comments);
                if (importNotes == ImportNotesBehavior.Replace)
                {
                    foreach (var com in comments)
                    {
                        metadata.RemoveMetadata(com);
                    }
                    comments.Clear();
                }

                for (int i = 0; i < notes.NoteCount; ++i)
                {
                    var n = notes.GetNote(i);
                    if (n.AppliesTo != requiredNoteType)
                        continue;

                    if (importNotes == ImportNotesBehavior.Merge)
                    {
                        // See if the note already exists
                        if (comments.Any(c => c.CommentText == n.NoteText))
                            continue;
                    }

                    // Add a new note
                    metadata.AddMetadata(new Comment { CommentText = n.NoteText });
                    count++;
                }
            }
            return count;
        }

        internal static void ExportSelected(LocaleIdentifier source, string dir, string name, XliffVersion version, Dictionary<StringTableCollection, HashSet<int>> collectionsWithSelectedIndexes, ITaskReporter reporter = null)
        {
            var documents = DictionaryPool<LocaleIdentifier, IXliffDocument>.Get();

            try
            {
                // Used for reporting
                int totalTasks = collectionsWithSelectedIndexes.Sum(c => c.Value.Count);
                float taskStep = 1.0f / (totalTasks * 2.0f);
                float progress = 0;
                if (reporter != null && reporter.Started != true)
                    reporter.Start($"Exporting {totalTasks} String Tables to XLIFF", string.Empty);

                foreach (var kvp in collectionsWithSelectedIndexes)
                {
                    var stringTableCollection = kvp.Key;
                    var sourceTable = stringTableCollection.GetTable(source) as StringTable;
                    if (sourceTable == null)
                    {
                        var message = $"Collection {stringTableCollection.TableCollectionName} does not contain a table for the source language {source}";
                        reporter?.Fail(message);
                        throw new Exception(message);
                    }

                    foreach (var stringTableIndex in kvp.Value)
                    {
                        var stringTable = stringTableCollection.StringTables[stringTableIndex];

                        reporter?.ReportProgress($"Generating document for {stringTable.name}", progress);
                        progress += taskStep;

                        if (!documents.TryGetValue(stringTable.LocaleIdentifier, out var targetDoc))
                        {
                            targetDoc = CreateDocument(source, stringTable.LocaleIdentifier, version);
                            documents[stringTable.LocaleIdentifier] = targetDoc;
                        }

                        AddTableToDocument(targetDoc, sourceTable, stringTable);
                    }
                }

                // Now write the files
                foreach (var doc in documents)
                {
                    var cleanName = CleanFileName(name);
                    var fileName = $"{cleanName}_{doc.Key.Code}.xlf";
                    var filePath = Path.Combine(dir, fileName);

                    reporter?.ReportProgress($"Writing {fileName}", progress);
                    progress += taskStep;
                    using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        doc.Value.Serialize(stream);
                    }
                }

                reporter?.Completed($"Finished exporting");
            }
            catch (Exception e)
            {
                reporter?.Fail(e.Message);
                throw;
            }
            finally
            {
                DictionaryPool<LocaleIdentifier, IXliffDocument>.Release(documents);
            }
        }

        static string CleanFileName(string fileName)
        {
            // Removes invalid characters from the filename
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), "_"));
        }
    }
}
