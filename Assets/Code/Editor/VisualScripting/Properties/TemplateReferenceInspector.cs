using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Editor.Main.Templates;
using Awaken.TG.Editor.VisualScripting.Utils;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.VisualScripting.Properties {
    [Inspector(typeof(TemplateReference))]
    public class TemplateReferenceInspector : Inspector {

        List<string> _paths = new(20);
        List<string> _shrunkPaths = new(20);
        string _search;
        int? _currentIndex;

        MetadataAccessor<string> Guid => new(this, "_guid");

        public TemplateReferenceInspector(Metadata metadata) : base(metadata) {
            metadata.value ??= new TemplateReference();
        }

        protected override float GetHeight(float width, GUIContent label) {
            var height = EditorGUIUtility.singleLineHeight;

            if (metadata.HasAttribute<TemplateTypeAttribute>()) {
                height += EditorGUIUtility.singleLineHeight;
            }

            return height + 2;
        }

        public override float GetAdaptiveWidth() {
            return 150;
        }

        protected override void OnGUI(Rect position, GUIContent label) {
            ValidateCurrentGUID();
            
            var templateType = metadata.GetAttribute<TemplateTypeAttribute>()?.Type;
            string assetPath = null;
            if (templateType != null) {
                position = Inspector.BeginLabeledBlock(Guid.Metadata, position, label);
                
                var propertyDrawerRects = new PropertyDrawerRects(position);
                var row1 = propertyDrawerRects.AllocateTop((int)EditorGUIUtility.singleLineHeight);
                Rect row2 = (Rect) propertyDrawerRects;
                row1.y += 5;
                row2.y += 5;

                var drawerRectRow1 = new PropertyDrawerRects(row1);
                var row1Search = drawerRectRow1.AllocateLeft(Mathf.Min((int)(row1.width/3-1), 250));
                var row1Instance = (Rect)drawerRectRow1;

                _search = EditorGUI.TextField(row1Search, _search);
                
                assetPath = DrawInstanceField(row1Instance, GUIContent.none, templateType);
                
                string dropdownLabel = TemplateReferenceDrawer.GetDropdownLabel(Guid.Get(), templateType);
                bool dropdown = EditorGUI.DropdownButton(row2, new GUIContent(dropdownLabel), FocusType.Keyboard);
                if (dropdown) {
                    _paths.Clear();
                    _paths.AddRange(TemplateReferenceDrawer.ObtainPaths(templateType).Where(path => string.IsNullOrWhiteSpace(_search) || path.Contains(_search)));
                    _shrunkPaths.Clear();
                    _shrunkPaths.AddRange(TemplateReferenceDrawer.ShrinkPaths(_paths));

                    // Check current selected popup index
                    _currentIndex ??= TemplateReferenceDrawer.CurrentIndex(Guid.Get(), _paths);
                    GenericMenu menu = TemplateReferenceDrawer.CreateGenericMenu(_shrunkPaths, Guid.Get(), 
                        i => i == _currentIndex!.Value, i => _currentIndex = i);
                    menu.DropDown(row2);
                } else if (_currentIndex != null) {
                    if (_currentIndex >= 0) {
                        assetPath = _paths.ElementAt(_currentIndex.Value);
                        Guid.Set(AssetDatabase.AssetPathToGUID(assetPath));
                    } else if (_currentIndex == -2) {
                        Guid.Set(null);
                    }
                    _currentIndex = null;
                }
            } else {
                Inspector.BeginBlock(Guid.Metadata, position);
                assetPath = DrawInstanceField(position, label, typeof(Template));
            }

            if (Inspector.EndBlock(Guid.Metadata)) {
                Guid.Set(string.IsNullOrWhiteSpace(assetPath) ? null : AssetDatabase.AssetPathToGUID(assetPath));
            }
            
            ManageDragAndDrop(position, templateType);
        }
        
        void ValidateCurrentGUID() {
            if (!string.IsNullOrWhiteSpace(Guid.Get())) {
                string path = AssetDatabase.GUIDToAssetPath(Guid.Get());
                if (string.IsNullOrWhiteSpace(path)) {
                    Log.Important?.Error($"Invalid GUID ({Guid}) was assigned to template reference");
                }
            }
        }
        
        string DrawInstanceField(Rect position, GUIContent label, Type assetType) {
            Object asset = null;
            if (!string.IsNullOrWhiteSpace(Guid.Get())) {
                asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(Guid.Get()));
            }
            asset = EditorGUI.ObjectField(position, label, asset, assetType, false);
            return AssetDatabase.GetAssetPath(asset);
        }

        void ManageDragAndDrop(Rect position, Type assetType) {
            if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition)) {
                DragAndDrop.visualMode = TemplateReferenceDrawer.CanAcceptDrag(assetType) ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
                Event.current.Use();
            } else if (Event.current.type == EventType.DragPerform && position.Contains(Event.current.mousePosition)) {
                if(TemplateReferenceDrawer.CanAcceptDrag(assetType)) {
                    Object templateObject = DragAndDrop.objectReferences[0];
                    string assetPath = AssetDatabase.GetAssetPath(templateObject);
                    Guid.Set(string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.AssetPathToGUID(assetPath));
                    DragAndDrop.AcceptDrag();
                    Event.current.Use();
                }
            }
        }
    }
}