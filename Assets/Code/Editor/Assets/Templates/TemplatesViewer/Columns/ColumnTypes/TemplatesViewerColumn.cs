using System;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes {

    public abstract class TemplatesViewerColumn : ScriptableObject {
        [SerializeField] int id;
        [SerializeField] bool foldout = true;
        [SerializeField] TemplatesViewerCategory owner;

        public int ID {
            get => id;
            set => id = value;
        }

        public TemplatesViewerCategory Owner {
            get => owner;
            set => owner = value;
        }

        public bool Foldout {
            get => foldout;
            set => foldout = value;
        }

        public abstract void DrawCell(Rect cellRect, TemplatesViewerTreeItem item);
        public abstract object GetSortingObject(TemplatesViewerTreeItem item);

        public virtual void OnGUI() {
        }
        public virtual void Refresh() {
        }

        public virtual float GetRowHeight(TemplatesViewerTreeItem item) {
            return ColumnPropertyDrawer.DefaultRowHeight;
        }
        
        public object SortingObject(TemplatesViewerTreeItem item) {
            var obj = GetSortingObject(item);
            if (obj is null or IComparable or IComparable<object>) {
                return obj;
            }
            return obj.ToString();
        }
    }
}