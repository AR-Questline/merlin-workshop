using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Debugging.UI.InteractionTrees;
using Awaken.TG.Editor.Debugging.UI.InteractionTrees.Containers;
using Awaken.TG.Editor.Debugging.UI.UIEventTypes;
using Awaken.TG.Editor.Utility.Tables;
using Awaken.TG.MVC.UI;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.UI {
    public static class UIDebugLayout {
        const float NameColumnWidth = 150;
        const float DataColumnWidth = 100;
        const float ResultColumnWidth = 60;
        const float HandlerColumnWidth = 100;
        
        const int FrameButtonsInLine = 3;
        const float FrameButtonWidth = 40;
        const float FrameButtonsMargin = 5;
        
        public const float StartStopButtonWidth = 100;
        public const float TablesTop = 40;
        public const float TablesSpacing = 10;
        public const float LookupWidth = 450;
        public const float LookupTabHeight = 40;
        public const float ArrowButtonWidth = 30;

        public const string FrameTab = "Frames";
        public const string EventTab = "Events";

        static EditorTable CreateConstDepthTable(EditorTableHeader header, IEnumerable<Func<int, TreeViewItem>> itemCreators) {
            bool any = false;
            
            var root = new TreeViewItem(-1, -1, "root");
            int id = 0;
            foreach (var creator in itemCreators) {
                any = true;
                root.AddChild(creator(id++));
            }

            return any ? new EditorTable(header, root) : null;
        }

        public static EditorTable CreateEventsInFrameTable(FrameData data) {   
            if (data == null) return null;

            var header = new EditorTableHeader(EventsInFrameName, EventsInFrameData, EventsInFrameResult, EventsInFrameHandler);
            var creators = data.Events.Select<UIEventType, Func<int, TreeViewItem>>(e => id => new EventInFrameItem(id, e, data));
            return CreateConstDepthTable(header, creators);
        }

        public static EditorTable CreateFramesByEventTable(UIEventsCollector collector) {
            var header = new EditorTableHeader(FramesByEventName, FramesByEventData, FramesByEventFrames);
            var creators = collector.FramesByEvent().Select<(UIEventType, List<int>), Func<int, TreeViewItem>>(pair => id => new FramesByEventItem(id, pair.Item1, pair.Item2));
            return CreateConstDepthTable(header, creators);
        }
        
        public static EditorTable CreateInteractionTree(InteractionTree tree) {
            int id = -1;
            return new EditorTable(new EditorTableHeader(InteractionName, InteractionResult), CreateInteractionItem(ref id, tree, -1));
        }
        static InteractionTreeItem CreateInteractionItem(ref int id, IHandlerItem handlerItem, int depth) {
            var treeItem = new InteractionTreeItem(id++, handlerItem, depth);
            if (handlerItem is HandlerContainer handlerContainer) {
                foreach (var childHandler in handlerContainer.Handlers) {
                    treeItem.AddChild(CreateInteractionItem(ref id, childHandler, depth+1));
                }
            }
            return treeItem;
        }

        public static string NameOf(UIEventType evt) {
            return evt?.NameColumn ?? "none";
        }
        
        static readonly EditorTableColumn
            EventsInFrameName = EditorTableColumn.Name("Name", NameColumnWidth),
            EventsInFrameData = EditorTableColumn.Custom("Data", DataColumnWidth, (rect, item) => {
                if (item is EventInFrameItem {EventType: { } evt}) {
                    EditorGUI.LabelField(rect, evt.DataColumn);
                }
            }),
            EventsInFrameResult = EditorTableColumn.Custom("Result", ResultColumnWidth, (rect, item) => {
                if (item is EventInFrameItem {Result: {} result}) {
                    EditorGUI.LabelField(rect, $"[{result}]");
                }
            }),
            EventsInFrameHandler = EditorTableColumn.Custom("Handler", HandlerColumnWidth, (rect, item) => {
                if (item is EventInFrameItem {Handler: {} handler}) {
                    EditorGUI.LabelField(rect, handler);
                }
            }),
            
            FramesByEventName = EditorTableColumn.Name("Name", NameColumnWidth),
            FramesByEventData = EditorTableColumn.Custom("Data", DataColumnWidth, (rect, item) => {
                if (item is FramesByEventItem {EventType: { } evt}) {
                    EditorGUI.LabelField(rect, evt.DataColumn);
                }
            }),
            FramesByEventFrames = EditorTableColumn.Custom("Frames", FrameButtonsInLine*FrameButtonWidth + 2*FrameButtonsMargin + 2*ArrowButtonWidth + 7, (rect, item) => {
                if (item is FramesByEventItem {EventType: {} evt, Frames: {} frames} framesByEventItem) {
                    float positionX = rect.x + ArrowButtonWidth + FrameButtonsMargin;
                    int index = framesByEventItem.FrameButtonsOffset;
                    while (index < frames.Count && positionX + FrameButtonWidth <= rect.x + rect.width - FrameButtonsMargin - ArrowButtonWidth) {
                        var buttonRect = new Rect(positionX, rect.y, FrameButtonWidth, rect.height);
                        if (GUI.Button(buttonRect, frames[index].ToString())) {
                            UIDebug.Instance.SelectFrameAndEvent(frames[index], evt);
                        }

                        index++;
                        positionX += FrameButtonWidth;
                    }
                    
                    if (GUI.Button(new Rect(rect.x, rect.y, ArrowButtonWidth, rect.height), "<")) {
                        framesByEventItem.FrameButtonsOffset--;
                    }
                    if (GUI.Button(new Rect(rect.x + rect.width - ArrowButtonWidth, rect.y, ArrowButtonWidth, rect.height), ">")) {
                        framesByEventItem.FrameButtonsOffset++;
                    }
                }
            }),

            InteractionName = EditorTableColumn.Name("Name", NameColumnWidth),
            InteractionResult = EditorTableColumn.Custom("Result", ResultColumnWidth, (rect, item) => {
                if (item is InteractionTreeItem {Result: {} result}) {
                    EditorGUI.LabelField(rect, $"[{result}]");
                }
            });
    }
    
    [Serializable]
    public class EventInFrameItem : TreeViewItem {
        public UIEventType EventType { get; }
        public UIResult? Result { get; }
        public string Handler { get; }

        public EventInFrameItem(int id, UIEventType evt, FrameData frameData) : base(id, 0, UIDebugLayout.NameOf(evt)) {
            EventType = evt;
            (Handler, Result) = frameData.HandlerAndResultOf(evt);
        }
    }

    [Serializable]
    public class FramesByEventItem : TreeViewItem {
        int _frameButtonsOffset;
        public UIEventType EventType { get; }
        public List<int> Frames { get; }

        public int FrameButtonsOffset {
            get => _frameButtonsOffset;
            set {
                if (value < 0) value = 0;
                if (value >= Frames.Count) value = Frames.Count - 1;
                _frameButtonsOffset = value;
            }
        }

        public FramesByEventItem(int id, UIEventType evt, List<int> frames) : base(id, 0, UIDebugLayout.NameOf(evt)) {
            EventType = evt;
            Frames = frames;
        }
    }
    
    [Serializable]
    public class InteractionTreeItem : TreeViewItem {
        public UIResult? Result { get; }

        public InteractionTreeItem(int id, IHandlerItem item, int depth) : base(id, depth, item.Name) {
            Result = item.Result;
        }
    }
}