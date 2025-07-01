using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset.CustomWindow
{
    // public class StoryGraphPopupToolEditor : OdinMenuEditorWindow {
    //     static OdinMenuTree s_tree;
    //     static StoryGraphPopupToolEditor s_window;
    //     
    //     public static void OpenWindowAndExecute<TResult, TResultEntry>(StoryGraphUtilityTool<TResult, TResultEntry> tool, string title) 
    //         where TResult : IResult<TResultEntry>, new()
    //         where TResultEntry : IResultEntry {
    //         s_tree = new OdinMenuTree(false) { { title, tool } };
    //         
    //         if (s_window != null) {
    //             s_window.Close();  
    //         }
    //         
    //         s_window = GetWindow<StoryGraphPopupToolEditor>(title);
    //         s_window.Show();
    //         
    //         AssemblyReloadEvents.beforeAssemblyReload += () => s_window?.Close(); 
    //         tool.Execute();
    //     }
    //     
    //     protected override OdinMenuTree BuildMenuTree() {
    //         return s_tree;
    //     }
    // }
}
