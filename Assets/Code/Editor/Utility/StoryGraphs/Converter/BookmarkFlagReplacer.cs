using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Main.Stories.Core;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Converter {
    public class BookmarkFlagReplacer : OdinEditorWindow {
        [FolderPath(AbsolutePath = false)] public string[] directoryPaths = { "Assets/Data/Templates/Stories" };
        [SerializeField] List<ReplacementData> _replacementData = new ();

        [MenuItem("TG/Graphs/Bookmark Flag Replacer")]
        public static void ShowWindow() {
            var window = CreateWindow<BookmarkFlagReplacer>();
            window.Show();
        }

        [Button]
        public void ReplaceBookmarkFlags() {
            var graphs = TemplatesSearcher.FindAllOfType<StoryGraph>()
                .Where(g => directoryPaths.Any(p => AssetDatabase.GetAssetPath(g).Contains($"{p}/")))
                .ToList();

            foreach (StoryGraph graph in graphs) {
                foreach (var data in _replacementData) {
                    var bookmarkToReplace = graph.Bookmarks.FirstOrDefault(b => b.flag == data.bookmarkToReplace);
                    if (bookmarkToReplace) {
                        bookmarkToReplace.flag = data.replacementBookmark;
                    }
                }

                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssets();
            }
        }
        
        [Serializable]
        public class ReplacementData {
            public string bookmarkToReplace;
            public string replacementBookmark;
        }
    }
}