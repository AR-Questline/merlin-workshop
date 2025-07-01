using System.IO;
using System.Xml.Serialization;
using UnityEditor.Localization.Plugins.XLIFF.Common;

namespace UnityEditor.Localization.Plugins.XLIFF.V12
{
    public partial class xliff : IXliffDocument
    {
        const string k_FileDataType = "plaintext";

        string m_SourceLang;
        string m_TargetLang;

        [XmlIgnore]
        public string Version
        {
            get
            {
                switch (version)
                {
                    case AttrType_Version.Item12:
                        return "1.2";
                    case AttrType_Version.Item11:
                        return "1.1";
                    case AttrType_Version.Item10:
                        return "1.0";
                }
                return "Unknown";
            }
        }

        [XmlIgnore]
        public string SourceLanguage
        {
            get
            {
                if (file.Count > 0)
                    return file[0].sourcelanguage;
                return m_SourceLang;
            }
            set
            {
                m_SourceLang = value;
                foreach (var f in file)
                {
                    f.sourcelanguage = value;
                }
            }
        }

        [XmlIgnore]
        public string TargetLanguage
        {
            get
            {
                if (file.Count > 0)
                    return file[0].targetlanguage;
                return m_TargetLang;
            }
            set
            {
                m_TargetLang = value;
                foreach (var f in file)
                {
                    f.targetlanguage = value;
                }
            }
        }

        [XmlIgnore]
        public int FileCount => file.Count;

        public IFile GetFile(int index) => file[index];

        public void AddFile(IFile f)
        {
            var f12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<file>(f);
            f12.sourcelanguage = m_SourceLang;
            f12.targetlanguage = m_TargetLang;
            f12.datatype = k_FileDataType;
            file.Add(f12);
        }

        public IFile AddNewFile()
        {
            var f = new file { sourcelanguage = m_SourceLang, targetlanguage = m_TargetLang, datatype = k_FileDataType };
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
            get => (header.skl?.Item as externalfile)?.uid;
            set => ExternalFile.uid = value;
        }

        externalfile ExternalFile
        {
            get
            {
                if (header.skl == null)
                    header.skl = new ElemType_ExternalReference();
                if (header.skl.Item == null)
                    header.skl.Item = new externalfile();
                return header.skl.Item as externalfile;
            }
        }

        [XmlIgnore]
        public int GroupCount => body.GetItemCount<IGroup>();

        [XmlIgnore]
        public int TranslationUnitCount => body.GetItemCount<ITranslationUnit>();

        [XmlIgnore]
        public string Original
        {
            get => original;
            set
            {
                original = value;
                ExternalFile.href = value;
            }
        }

        [XmlIgnore]
        public int NoteCount => header == null ? 0 : header.Items.GetItemCount<INote>();

        public IGroup GetGroup(int index) => body.GetItem<IGroup>(index);

        public void AddGroup(IGroup grp)
        {
            var g12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<group>(grp);
            body.Add(g12);
        }

        public IGroup AddNewGroup()
        {
            var grp = new group();
            body.Add(grp);
            return grp;
        }

        public void RemoveGroup(IGroup grp)
        {
            var g12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<group>(grp);
            body.Remove(g12);
        }

        public ITranslationUnit GetTranslationUnit(int index) => body.GetItem<ITranslationUnit>(index);

        public void AddTranslationUnit(ITranslationUnit tu)
        {
            var tu12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<transunit>(tu);
            body.Add(tu12);
        }

        public ITranslationUnit AddNewTranslationUnit()
        {
            var tu12 = new transunit();
            body.Add(tu12);
            return tu12;
        }

        public void RemoveTranslationUnit(ITranslationUnit tu)
        {
            var tu12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<transunit>(tu);
            body.Remove(tu12);
        }

        public INote AddNewNote()
        {
            var n12 = new note();
            var items =  header.Items ?? new object[0];
            ArrayUtility.Add(ref items, n12);
            header.Items = items;

            var choice = header.ItemsElementName ?? new ItemsChoiceType[0];
            ArrayUtility.Add(ref choice, ItemsChoiceType.note);
            header.ItemsElementName = choice;

            return n12;
        }

        public void AddNote(INote note)
        {
            var n12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<note>(note);

            var items =  header.Items ?? new object[0];
            ArrayUtility.Add(ref items, n12);

            var choice = header.ItemsElementName ?? new ItemsChoiceType[0];
            ArrayUtility.Add(ref choice, ItemsChoiceType.note);
            header.ItemsElementName = choice;

            header.Items = items;
        }

        public INote GetNote(int index) => header?.Items.GetItem<INote>(index);

        public void RemoveNote(INote note)
        {
            var n12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<note>(note);

            var items =  header.Items;
            if (items == null)
                return;
            ArrayUtility.Remove(ref items, note);
            header.Items = items;
        }
    }

    public partial class note : INote
    {
        [XmlIgnore]
        public NoteType AppliesTo
        {
            get
            {
                if (annotates == AttrType_annotates.source)
                    return NoteType.Source;
                if (annotates == AttrType_annotates.target)
                    return NoteType.Target;
                return NoteType.General;
            }
            set
            {
                if (value == NoteType.General)
                    annotates = AttrType_annotates.general;
                else if (value == NoteType.Source)
                    annotates = AttrType_annotates.source;
                else
                    annotates = AttrType_annotates.target;
            }
        }

        [XmlIgnore]
        public string NoteText
        {
            get => Value;
            set => Value = value;
        }
    }

    public partial class transunit : ITranslationUnit
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
            get => resname;

            set => resname = value;
        }

        [XmlIgnore]
        public string Source
        {
            get => source?.Text ? [0];
            set => source = new source { Text = new[] { value } };
        }

        [XmlIgnore]
        public string Target
        {
            get => target?.Text ? [0];
            set => target = new target { Text = new[] { value } };
        }

        [XmlIgnore]
        public int NoteCount => Items.GetItemCount<INote>();

        public INote AddNewNote()
        {
            var n12 = new note();
            Items.Add(n12);
            return n12;
        }

        public void AddNote(INote note)
        {
            var n12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<note>(note);
            Items.Add(n12);
        }

        public INote GetNote(int index) => Items.GetItem<INote>(index);

        public void RemoveNote(INote note)
        {
            var n12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<note>(note);
            Items.Remove(n12);
        }
    }

    public partial class group : IGroup
    {
        [XmlIgnore]
        public string Name
        {
            get => resname;
            set => resname = value;
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
        public int NoteCount => note.Count;

        [XmlIgnore]
        public int TranslationUnitCount => Items.GetItemCount<ITranslationUnit>();

        public IGroup GetGroup(int index) => Items.GetItem<IGroup>(index);

        public void AddGroup(IGroup grp)
        {
            var g12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<group>(grp);
            Items.Add(g12);
        }

        public IGroup AddNewGroup()
        {
            var grp = new group();
            Items.Add(grp);
            return grp;
        }

        public void RemoveGroup(IGroup grp)
        {
            var g12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<group>(grp);
            Items.Remove(g12);
        }

        public ITranslationUnit GetTranslationUnit(int index) => Items.GetItem<ITranslationUnit>(index);

        public void AddTranslationUnit(ITranslationUnit tu)
        {
            var tu12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<transunit>(tu);
            Items.Add(tu12);
        }

        public ITranslationUnit AddNewTranslationUnit()
        {
            var tu12 = new transunit();
            Items.Add(tu12);
            return tu12;
        }

        public void RemoveTranslationUnit(ITranslationUnit tu)
        {
            var tu12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<transunit>(tu);
            Items.Remove(tu12);
        }

        public INote AddNewNote()
        {
            var n12 = new note();
            note.Add(n12);
            return n12;
        }

        public void AddNote(INote n)
        {
            var n12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<note>(n);
            note.Add(n12);
        }

        public INote GetNote(int index) => note[index];

        public void RemoveNote(INote n)
        {
            var n12 = TypeVersionCheck.GetConcreteTypeThrowIfTypeVersionMismatch<note>(n);
            note.Remove(n12);
        }
    }
}
