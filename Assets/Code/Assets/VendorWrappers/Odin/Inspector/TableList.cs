using System;

namespace Sirenix.OdinInspector
{
    public class TableListAttribute : Attribute
    {
        public int NumberOfItemsPerPage;
        public bool IsReadOnly;
        public int DefaultMinColumnWidth = 40;
        public bool ShowIndexLabels;
        public bool DrawScrollView = true;
        public int MinScrollViewHeight = 350;
        public int MaxScrollViewHeight;
        public bool AlwaysExpanded;
        public bool HideToolbar;
        public int CellPadding = 2;

        public bool ShowPaging
        {
            get => true;
            set { }
        }

        public int ScrollViewHeight
        {
            get => 0;
            set { }
        }
    }
}