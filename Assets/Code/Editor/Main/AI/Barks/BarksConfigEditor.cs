using System.Linq;
using Awaken.TG.Main.AI.Barks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.Main.AI.Barks
{
    [CustomEditor(typeof(BarksConfig))]
    public class BarksConfigEditor : UnityEditor.Editor {
        const string LastSelectedBookmarkKey = "LastSelectedBookmarkIndex";
        
        public override VisualElement CreateInspectorGUI() {
            VisualElement root = new ();
            
            var prop = serializedObject.FindProperty("bookmarks");
            if (prop == null) {
                return root;
            }
            
            var bookmarkNames = new string[prop.arraySize];
            for (int i = 0; i < prop.arraySize; i++) {
                var element = prop.GetArrayElementAtIndex(i);
                bookmarkNames[i] = element.objectReferenceValue.name;
            }
            
            // Retrieve the last selected bookmark index from EditorPrefs
            int lastSelectedIndex = EditorPrefs.GetInt(LastSelectedBookmarkKey, 0);
            
            // Create a container for the buttons and bookmark selection
            VisualElement buttonRoot = new ();
            buttonRoot.style.flexDirection = FlexDirection.Row;
            root.Add(buttonRoot);
            
            var bookmarkField = new PopupField<string>("Select Bookmark", bookmarkNames.ToList(), lastSelectedIndex) {
                style = {
                    flexGrow = 1,
                    marginRight = 5
                }
            };
            
            var separator = new VisualElement();
            separator.style.width = 1;
            separator.style.backgroundColor = new StyleColor(Color.gray);
            separator.style.marginLeft = 5;
            separator.style.marginRight = 5;
            
            Button importAvailabilityButton = new (BookmarksAvailabilityImporter.ImportBookmarksAvailability) {
                text = "Import Availability Config",
                tooltip = "Import bookmarks availability from Google Sheets, " +
                          "set's AvailableBookmarks property in Actors within Actors prefab (Actors Register)",
                style = { flexGrow = 1 }    
            };
            
            Button importButton = new (() => ((BarksConfig)target).ImportFromGoogleSheet()) {
                text = "Import",
                style = { flexGrow = 1 }    
            };

            Button exportButton = new (() => { ((BarksConfig)target).ExportToGoogleSheet(); }) {
                text = "Export",
                style = { flexGrow = 1 }    
            };

            Button syncButton = new (() => { ((BarksConfig)target).SyncGraphs(); }) {
                text = "Sync",
                style = { flexGrow = 1 }    
            };
            
            buttonRoot.Add(bookmarkField);
            buttonRoot.Add(separator);
            buttonRoot.Add(importAvailabilityButton);
            buttonRoot.Add(importButton);
            buttonRoot.Add(exportButton);
            buttonRoot.Add(syncButton);
            
            var descriptionLabel = bookmarkNames.Length > lastSelectedIndex ? BarkBookmarks.Editor_GetBookmarkDescription(bookmarkNames[lastSelectedIndex]) : "";
            var descriptionHelpBox = new HelpBox(descriptionLabel, HelpBoxMessageType.Info);
            descriptionHelpBox.Q<Label>().style.fontSize = 12f;
            root.Add(descriptionHelpBox);
            
            // Create container for selected bookmark inspector
            var bookmarkInspectorContainer = new VisualElement();
            root.Add(bookmarkInspectorContainer);
            
            // Update inspector when selection changes
            bookmarkField.RegisterValueChangedCallback(evt => {
                bookmarkInspectorContainer.Clear(); 
                DrawSelectedBookmark(bookmarkInspectorContainer, prop, bookmarkField.index);
                
                lastSelectedIndex = bookmarkField.index;
                EditorPrefs.SetInt(LastSelectedBookmarkKey, lastSelectedIndex);
                descriptionHelpBox.text = BarkBookmarks.Editor_GetBookmarkDescription(bookmarkNames[lastSelectedIndex]);
                bookmarkField.tooltip = descriptionHelpBox.text;
            });
            
            // Display the last selected bookmark by default
            if (prop.arraySize > 0) {
                DrawSelectedBookmark(bookmarkInspectorContainer, prop, lastSelectedIndex);
            }
            
            return root;
        }
        
        void DrawSelectedBookmark(VisualElement container, SerializedProperty prop, int index) {
            var selectedBookmark = prop.GetArrayElementAtIndex(index).objectReferenceValue;
            if (selectedBookmark != null) {
                container.Add(new InspectorElement(selectedBookmark));
            }
        }
    }
}
