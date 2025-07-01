using System.IO;

namespace UnityEditor.Localization.Plugins.XLIFF.Common
{
    /// <summary>
    /// The root element of an XLIFF document is &lt;xliff&gt;.
    /// It contains a collection of &lt;file&gt; elements. Typically, each &lt;file&gt; element contains a
    /// set of &lt;unit&gt; elements that contain the text to be translated in the &lt;source&gt;
    /// child of one or more &lt;segment&gt; elements.
    /// Translations are stored in the &lt;target&gt; child of each &lt;segment&gt; element.
    /// </summary>
    public interface IXliffDocument
    {
        /// <summary>
        /// The XLIFF version.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// The language that was translated from to <see cref="TargetLanguage"/>
        /// </summary>
        string SourceLanguage { get; set; }

        /// <summary>
        /// The language that was translated to from <see cref="SourceLanguage"/>.
        /// </summary>
        string TargetLanguage { get; set; }

        /// <summary>
        /// The number of files in the document.
        /// </summary>
        int FileCount { get; }

        /// <summary>
        /// Returns the file for the requested index.
        /// </summary>
        /// <param name="index">The file index.</param>
        /// <returns>The requested file of null if one does not exist.</returns>
        IFile GetFile(int index);

        /// <summary>
        /// Adds a new file to the document.
        /// </summary>
        /// <param name="f"></param>
        void AddFile(IFile f);

        /// <summary>
        /// Remove the files from the document.
        /// </summary>
        /// <param name="f"></param>
        void RemoveFile(IFile f);

        /// <summary>
        /// Add a new files to the document and returns it.
        /// </summary>
        /// <returns>The new file.</returns>
        IFile AddNewFile();

        /// <summary>
        /// Serialize the document into XLIFF.
        /// </summary>
        /// <param name="stream"></param>
        void Serialize(Stream stream);
    }

    /// <summary>
    /// The target of the note.
    /// </summary>
    public enum NoteType
    {
        /// <summary>
        /// General note that applies to the whole entry.
        /// </summary>
        General,

        /// <summary>
        /// Note that only applies to the source language.
        /// </summary>
        Source,

        /// <summary>
        /// Note that only applies to the target language.
        /// </summary>
        Target
    }

    /// <summary>
    /// Readable comments and annotations.
    /// </summary>
    public interface INote
    {
        /// <summary>
        /// The target of the note.
        /// </summary>
        NoteType AppliesTo { get; set; }

        /// <summary>
        /// The contents of the note.
        /// </summary>
        string NoteText { get; set; }
    }

    /// <summary>
    /// Holds a collection of <see cref="INote"/>.
    /// </summary>
    public interface INoteCollection
    {
        /// <summary>
        /// Returns the number of notes in the collection.
        /// </summary>
        int NoteCount { get; }

        /// <summary>
        /// Returns the note for the requested index.
        /// </summary>
        /// <param name="index">The note index.</param>
        /// <returns>The requested note or <see langword="null"/> if one does not exist.</returns>
        INote GetNote(int index);

        /// <summary>
        /// Adds a note to the collection.
        /// </summary>
        /// <param name="note">The note to add.</param>
        void AddNote(INote note);

        /// <summary>
        /// Removes the note from the collection.
        /// </summary>
        /// <param name="note">The note to be removed.</param>
        void RemoveNote(INote note);

        /// <summary>
        /// Add a new note to the collection and returns it.
        /// </summary>
        /// <returns>The new note.</returns>
        INote AddNewNote();
    }

    /// <summary>
    /// Container for localization material extracted from an entire single document, or another high-level,
    /// self-contained logical node in a content structure that cannot be described in the terms of documents.
    /// </summary>
    public interface IFile : IGroupCollection, ITranslationUnitCollection, INoteCollection
    {
        /// <summary>
        /// The Id of the original document.
        /// By default this is the <see cref="UnityEngine.Localization.Tables.StringTable"/> asset guid.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// The location of the original document from which the content of the enclosing elements are extracted.
        /// By default this is the <see cref="UnityEngine.Localization.Tables.StringTable"/> path.
        /// </summary>
        string Original { get; set; }
    }

    /// <summary>
    /// Static container for a dynamic structure of elements holding the extracted translatable source text, aligned with the translated text.
    /// </summary>
    public interface ITranslationUnit : INoteCollection
    {
        /// <summary>
        /// The unique Id of the translation unit. By default this is the <see cref="UnityEngine.Localization.Tables.SharedTableData.SharedTableEntry.Id"/>.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// The unique name of the translation unit. By default this is the <see cref="UnityEngine.Localization.Tables.SharedTableData.SharedTableEntry.Key"/>.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The source text taken from the <see cref="UnityEngine.Localization.Tables.StringTable"/> for the source <see cref="UnityEngine.Localization.Locale"/>.
        /// </summary>
        string Source { get; set; }

        /// <summary>
        /// The target text taken from the <see cref="UnityEngine.Localization.Tables.StringTable"/> for the source <see cref="UnityEngine.Localization.Locale"/>.
        /// </summary>
        string Target { get; set; }
    }

    /// <summary>
    /// Holds a collection of <see cref="ITranslationUnit"/>.
    /// </summary>
    public interface ITranslationUnitCollection
    {
        /// <summary>
        /// The number of translation units in the collection.
        /// </summary>
        int TranslationUnitCount { get; }

        /// <summary>
        /// Returns the translation unit for the selected index.
        /// </summary>
        /// <param name="index">The index of the translation unit to return.</param>
        /// <returns>The translation unit or <see langword="null"/> if one does not exist for the selected index.</returns>
        ITranslationUnit GetTranslationUnit(int index);

        /// <summary>
        /// Adds the translation unit to the collection.
        /// </summary>
        /// <param name="tu">The translation unit to add to the collection.</param>
        void AddTranslationUnit(ITranslationUnit tu);

        /// <summary>
        /// Removes the translation unit from the collection.
        /// </summary>
        /// <param name="tu"></param>
        void RemoveTranslationUnit(ITranslationUnit tu);

        /// <summary>
        /// Adds a new translation unit to the collection and returns it.
        /// </summary>
        /// <returns>The newly created translation unit.</returns>
        ITranslationUnit AddNewTranslationUnit();
    }

    /// <summary>
    /// Provides a way to organize units into a structured hierarchy.
    /// </summary>
    public interface IGroup : IGroupCollection, INoteCollection, ITranslationUnitCollection
    {
        /// <summary>
        /// The unique Id of the group. By default this is mapped to the <see cref="UnityEngine.Localization.Tables.SharedTableData.TableCollectionNameGuid"/>.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// The unique name of the group. By default this is mapped to the <see cref="UnityEngine.Localization.Tables.SharedTableData.TableCollectionName"/>.
        /// </summary>
        string Name { get; set; }
    }

    /// <summary>
    /// Holds a collection of <see cref="IGroup"/>.
    /// </summary>
    public interface IGroupCollection
    {
        /// <summary>
        /// Returns the number of groups in the collection.
        /// </summary>
        int GroupCount { get; }

        /// <summary>
        /// Returns the group for the index.
        /// </summary>
        /// <param name="index">The index of the group to return.</param>
        /// <returns>The requested group or <see langword="null"/> if one does not exist.</returns>
        IGroup GetGroup(int index);

        /// <summary>
        /// Adds the group to the collection.
        /// </summary>
        /// <param name="grp">The group to add to the collection.</param>
        void AddGroup(IGroup grp);

        /// <summary>
        /// Removes the group from the collection.
        /// </summary>
        /// <param name="grp">The group to remove.</param>
        void RemoveGroup(IGroup grp);

        /// <summary>
        /// Adds a new group to the collection and returns it.
        /// </summary>
        /// <returns>The newly created group.</returns>
        IGroup AddNewGroup();
    }
}
