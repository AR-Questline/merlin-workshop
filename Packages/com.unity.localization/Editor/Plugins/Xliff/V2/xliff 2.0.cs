using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEditor.Localization.Plugins.XLIFF.Common;
using UnityEngine.Localization;

namespace UnityEditor.Localization.Plugins.XLIFF.V20
{
    public partial class xliff : IXliffDocument
    {
        [XmlIgnore]
        public string Version => version;

        [XmlIgnore]
        public string SourceLanguage
        {
            get => srcLang;
            set => srcLang = value;
        }

        [XmlIgnore]
        public string TargetLanguage
        {
            get => trgLang;
            set => trgLang = value;
        }

        [XmlIgnore]
        public int FileCount => file.Count;

        public IFile GetFile(int index) => file[index];

        public void AddFile(IFile f)
        {
            var f12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<file>(f);
            file.Add(f12);
        }

        public IFile AddNewFile()
        {
            var f = new file();
            file.Add(f);
            return f;
        }

        public void RemoveFile(IFile f)
        {
            var f12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<file>(f);
            file.Remove(f12);
        }

        public void Serialize(Stream stream)
        {
            var ser = new XmlSerializer(typeof(xliff));
            ser.Serialize(stream, this);
        }
    }

    public partial class file : IFile
    {
        [XmlIgnore]
        public string Id
        {
            get => id;
            set => id = value;
        }

        [XmlIgnore]
        public int GroupCount => Items.GetItemCount<IGroup>();

        [XmlIgnore]
        public int TranslationUnitCount => Items.GetItemCount<ITranslationUnit>();

        [XmlIgnore]
        public string Original
        {
            get => original;
            set => original = value;
        }

        [XmlIgnore]
        public int NoteCount => notes == null ? 0 : notes.Count;

        public IGroup GetGroup(int index) => Items.GetItem<IGroup>(index);

        public void AddGroup(IGroup grp)
        {
            var g20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<group>(grp);
            Items.Add(g20);
        }

        public IGroup AddNewGroup()
        {
            var grp = new group();
            Items.Add(grp);
            return grp;
        }

        public void RemoveGroup(IGroup grp)
        {
            var g20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<group>(grp);
            Items.Remove(g20);
        }

        public ITranslationUnit GetTranslationUnit(int index) => Items.GetItem<ITranslationUnit>(index);

        public void AddTranslationUnit(ITranslationUnit tu)
        {
            var tu20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<unit>(tu);
            Items.Add(tu20);
        }

        public ITranslationUnit AddNewTranslationUnit()
        {
            var tu20 = new unit();
            Items.Add(tu20);
            return tu20;
        }

        public void RemoveTranslationUnit(ITranslationUnit tu)
        {
            var tu20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<unit>(tu);
            Items.Remove(tu20);
        }

        public INote AddNewNote()
        {
            if (notes == null)
                notes = new List<note>();

            var n20 = new note();
            notes.Add(n20);
            return n20;
        }

        public void AddNote(INote note)
        {
            var n20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<note>(note);

            if (notes == null)
                notes = new List<note>();

            notes.Add(n20);
        }

        public INote GetNote(int index) => notes == null ? null : notes[index];

        public void RemoveNote(INote note)
        {
            if (notes == null)
                return;

            var n20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<note>(note);
            notes.Remove(n20);

            if (notes.Count == 0)
                notes = null;
        }
    }

    public partial class note : INote
    {
        [XmlIgnore]
        public NoteType AppliesTo
        {
            get
            {
                if (appliesToFieldSpecified)
                {
                    if (appliesTo == appliesTo.source)
                        return NoteType.Source;
                    return NoteType.Target;
                }
                return NoteType.General;
            }
            set
            {
                if (value == NoteType.General)
                {
                    appliesToFieldSpecified = false;
                    return;
                }

                if (value == NoteType.Source)
                    appliesTo = appliesTo.source;
                else
                    appliesTo = appliesTo.target;
                appliesToFieldSpecified = true;
            }
        }

        [XmlIgnore]
        public string NoteText
        {
            get
            {
                if (Text == null)
                    return null;

                if (Text.Length == 0)
                    return textField[0];

                using (StringBuilderPool.Get(out var sb))
                {
                    foreach (var t in Text)
                    {
                        sb.Append(t);
                    }
                    return sb.ToString();
                }
            }
            set => textField = new[] { value };
        }
    }

    public partial class unit : ITranslationUnit
    {
        [XmlIgnore]
        public string Id
        {
            get => id;
            set => id = value;
        }

        [XmlIgnore]
        public string Name
        {
            get => name;
            set => name = value;
        }

        [XmlIgnore]
        public segment Segment
        {
            get
            {
                if (Items == null)
                {
                    Items = new[] { new segment() };
                }
                return Items[0] as segment;
            }
        }

        [XmlIgnore]
        public string Source
        {
            get
            {
                if (Items == null)
                    return null;

                using (StringBuilderPool.Get(out var sb))
                {
                    foreach (var i in Items)
                    {
                        if (i is segment seg && seg.source != null && seg.source.Text != null)
                        {
                            foreach (var t in seg.source.Text)
                            {
                                sb.Append(t);
                            }
                        }
                    }
                    return sb.ToString();
                }
            }
            set => Segment.source = new source { Text = new[] { value } };
        }

        [XmlIgnore]
        public string Target
        {
            get
            {
                if (Items == null)
                    return null;

                using (StringBuilderPool.Get(out var sb))
                {
                    foreach (var i in Items)
                    {
                        if (i is segment seg && seg.target != null && seg.target.Text != null)
                        {
                            foreach (var t in seg.target.Text)
                            {
                                sb.Append(t);
                            }
                        }
                    }
                    return sb.ToString();
                }
            }
            set => Segment.target = new target { Text = new[] { value } };
        }

        [XmlIgnore]
        public int NoteCount => notes == null ? 0 : notes.Count;

        public INote AddNewNote()
        {
            if (notes == null)
                notes = new List<note>();

            var n20 = new note();
            notes.Add(n20);
            return n20;
        }

        public void AddNote(INote note)
        {
            var n20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<note>(note);

            if (notes == null)
                notes = new List<note>();

            notes.Add(n20);
        }

        public INote GetNote(int index) => notes == null ? null : notes[index];

        public void RemoveNote(INote note)
        {
            if (notes == null)
                return;

            var n20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<note>(note);

            notes.Remove(n20);

            if (notes.Count == 0)
                notes = null;
        }
    }

    public partial class group : IGroup
    {
        [XmlIgnore]
        public string Name
        {
            get => name;
            set => name = value;
        }

        [XmlIgnore]
        public string Id
        {
            get => id;
            set => id = value;
        }

        [XmlIgnore]
        public int GroupCount => Items.GetItemCount<IGroup>();

        [XmlIgnore]
        public int NoteCount => notes == null ? 0 : notes.Count;

        [XmlIgnore]
        public int TranslationUnitCount => Items.GetItemCount<ITranslationUnit>();

        public IGroup GetGroup(int index) => Items.GetItem<IGroup>(index);

        public void AddGroup(IGroup grp)
        {
            var g20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<group>(grp);
            Items.Add(g20);
        }

        public IGroup AddNewGroup()
        {
            var grp = new group();
            Items.Add(grp);
            return grp;
        }

        public void RemoveGroup(IGroup grp)
        {
            var g20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<group>(grp);
            Items.Remove(g20);
        }

        public ITranslationUnit GetTranslationUnit(int index) => Items.GetItem<ITranslationUnit>(index);

        public void AddTranslationUnit(ITranslationUnit tu)
        {
            var tu20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<unit>(tu);
            Items.Add(tu20);
        }

        public ITranslationUnit AddNewTranslationUnit()
        {
            var tu20 = new unit();
            Items.Add(tu20);
            return tu20;
        }

        public void RemoveTranslationUnit(ITranslationUnit tu)
        {
            var tu20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<unit>(tu);
            Items.Remove(tu20);
        }

        public INote AddNewNote()
        {
            if (notes == null)
                notes = new List<note>();

            var n20 = new note();
            notes.Add(n20);
            return n20;
        }

        public void AddNote(INote n)
        {
            var n20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<note>(n);

            if (notes == null)
                notes = new List<note>();

            notes.Add(n20);
        }

        public INote GetNote(int index) => notes == null ? null : notes[index];

        public void RemoveNote(INote n)
        {
            if (notes == null)
                return;

            var n20 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<note>(n);
            notes.Remove(n20);

            if (notes.Count == 0)
                notes = null;
        }
    }
}
