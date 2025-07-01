using System;

namespace Sirenix.OdinInspector
{
    public class ListDrawerSettingsAttribute : Attribute
    {
        public bool HideAddButton;
      public bool HideRemoveButton;
      public string ListElementLabelName;
      public string CustomAddFunction;
      public string CustomRemoveIndexFunction;
      public string CustomRemoveElementFunction;
      public string OnBeginListElementGUI;
      public string OnEndListElementGUI;
      public bool AlwaysAddDefaultValue;
      public bool AddCopiesLastElement;
      public string ElementColor;
      private string onTitleBarGUI;
      private int numberOfItemsPerPage;
      private bool paging;
      private bool draggable;
      private bool isReadOnly;
      private bool showItemCount;
      private bool pagingHasValue;
      private bool draggableHasValue;
      private bool isReadOnlyHasValue;
      private bool showItemCountHasValue;
      private bool numberOfItemsPerPageHasValue;
      private bool showIndexLabels;
      private bool showIndexLabelsHasValue;
      private bool defaultExpandedStateHasValue;
      private bool defaultExpandedState;
      public bool ShowFoldout = true;

      public bool ShowPaging
      { 
        get => default;
        set { }
      }

      public bool DraggableItems
      {
        get => default;
        set { }
      }

      public int NumberOfItemsPerPage
      {
        get => default;
        set { }
      }

      public bool IsReadOnly
      {
        get => default;
        set { }
      }

      public bool ShowItemCount
      {
        get => default;
        set { }
      }

      public bool Expanded
      {
        get => default;
        set { }
      }

      public bool DefaultExpandedState
      {
        get => default;
        set { }
      }

      public bool ShowIndexLabels
      {
        get => default;
        set { }
      }

      public string OnTitleBarGUI
      {
        get => default;
        set { }
      }
    }
}