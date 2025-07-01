using UnityEditor.Localization.UI;
using UnityEngine.Assertions;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization
{
    static class CharacterSetMenuItems
    {
        [MenuItem("CONTEXT/StringTableCollection/Export/Character Set...")]
        public static void ExportCollection(MenuCommand command)
        {
            var collection = command.context as StringTableCollection;
            Assert.IsTrue(collection != null, "Expected StringTableCollection");
            var wnd = ExportCharacterSetWindow.ShowWindow();
            wnd.SelectedTables.SetSelection(collection);
        }

        [MenuItem("CONTEXT/StringTable/Export/Character Set...")]
        public static void ExportStringTable(MenuCommand command)
        {
            var table = command.context as StringTable;
            Assert.IsTrue(table != null, "Expected StringTable");
            var wnd = ExportCharacterSetWindow.ShowWindow();
            wnd.SelectedTables.SetSelection(table);
        }
    }
}
