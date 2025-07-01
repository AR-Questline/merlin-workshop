using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityEditor.Localization.UI.Toolkit
{
    class ReorderableList : VisualElement
    {
        public static readonly string UssClassName = "reorderable-list";
        public static readonly string EmptyUssClassName = UssClassName + "__empty";
        public static readonly string ItemUssClassName = UssClassName + "-item";
        public static readonly string ItemContainerUssClassName = ItemUssClassName + "__container";
        public static readonly string ItemHandleUssClassName = ItemUssClassName + "__handle";
        public static readonly string ItemHandleDraggerUssClassName = ItemHandleUssClassName + "__dragger";
        public static readonly string SelectedItemUssClassName = ItemUssClassName + "--selected";

        internal class ListItem : VisualElement
        {
            public VisualElement Handle { get; set; }

            public VisualElement Container { get; set; }

            internal ValueAnimation<StyleValues> Animator { get; set; }

            VisualElement m_Handle1;
            VisualElement m_Handle2;

            public ListItem(bool draggable)
            {
                style.flexDirection = FlexDirection.Row;
                focusable = true;

                if (draggable)
                {
                    Handle = new VisualElement { name = "handle" };
                    Handle.AddToClassList(ItemHandleUssClassName);
                    AddToClassList(ItemUssClassName);

                    m_Handle1 = new VisualElement();
                    m_Handle1.AddToClassList(ItemHandleDraggerUssClassName);
                    Handle.Add(m_Handle1);
                    m_Handle2 = new VisualElement();
                    m_Handle2.AddToClassList(ItemHandleDraggerUssClassName);
                    Handle.Add(m_Handle2);
                    Add(Handle);
                }

                Container = new VisualElement { name = "container" };
                Container.AddToClassList(ItemContainerUssClassName);
                Add(Container);
            }

            public void SetSelected(bool selected)
            {
                if (selected)
                {
                    RemoveFromClassList(ItemUssClassName);
                    AddToClassList(SelectedItemUssClassName);
                }
                else
                {
                    AddToClassList(ItemUssClassName);
                    RemoveFromClassList(SelectedItemUssClassName);
                }
            }
        }

        int m_SelectedIndex = -1;
        internal ScrollView m_ScrollView;
        Button m_AddButton;
        Button m_RemoveButton;
        bool m_Init;

        public int Count
        {
            get => ListProperty != null ? ListProperty.arraySize : List.Count;
        }

        public SerializedProperty ListProperty { get; private set; }

        public IList List { get; private set; }

        public int Selected
        {
            get => m_SelectedIndex;
            set => Select(value);
        }

        public bool DisplayRemoveButton
        {
            get => m_RemoveButton.style.display == DisplayStyle.Flex;
            set => m_RemoveButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public string HeaderTitle
        {
            get => this.Q<Label>("header-label").text;
            set => this.Q<Label>("header-label").text = value;
        }

        public Texture HeaderIcon
        {
            get => this.Q<Image>("header-icon").image;
            set => this.Q<Image>("header-icon").image = value;
        }

        public string HeaderTooltip
        {
            get => this.Q<Label>("header-label").tooltip;
            set => this.Q<Label>("header-label").tooltip = value;
        }

        public bool DisplayAddButton
        {
            get => m_AddButton.style.display == DisplayStyle.Flex;
            set => m_AddButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public bool DisplayHeader
        {
            get => this.Q("header").style.display == DisplayStyle.Flex;
            set => this.Q("header").style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public bool DisplayFooter
        {
            get => this.Q("footer").style.display == DisplayStyle.Flex;
            set => this.Q("footer").style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public bool Draggable { get; set; } = true;

        /// <summary>
        /// If the list is nested then when an UndoRedo occurs the list will not refresh itself but instead let the root list do it.
        /// When a nested list has been moved and an undo occurs the nested list property path may now be incorrect so refreshing would cause errors.
        /// </summary>
        public bool IsNestedList { get; set; }

        public delegate VisualElement CreateHeaderContentsCallbackDelegate(ReorderableList list);
        public delegate void CreateItemCallbackDelegate(ReorderableList list, int index, VisualElement root);
        public delegate VisualElement CreateEmptyListItemCallbackDelegate(ReorderableList list);
        public delegate void AddItemCallbackDelegate(ReorderableList list, int insertIndex);
        public delegate void RemoveItemCallbackDelegate(ReorderableList list, int removeIndex);
        public delegate void ReorderCallbackDelegate(ReorderableList list, int oldIndex, int newIndex);
        public delegate void RefreshListCallbackDelegate(ReorderableList list);

        public RefreshListCallbackDelegate RefreshListCallback;
        public CreateHeaderContentsCallbackDelegate CreateHeaderContentsCallback { get; set; }
        public CreateItemCallbackDelegate CreateItemCallback { get; set; }
        public CreateEmptyListItemCallbackDelegate CreateEmptyCallback { get; set; }
        public AddItemCallbackDelegate AddCallback { get; set; }
        public RemoveItemCallbackDelegate RemoveCallback { get; set; }
        public ReorderCallbackDelegate ReorderCallback { get; set; }

        public ReorderableList(SerializedProperty listProperty)
        {
            ListProperty = listProperty.Copy();
            InitList();

            HeaderTitle = listProperty.displayName;
        }

        public ReorderableList(IList list)
        {
            List = list;
            InitList();
        }

        void InitList()
        {
            Resources.GetTemplateAsset(nameof(ReorderableList)).CloneTree(this);

            // We cant use the Style tag in uxml as its not supported in 2019.4 so we assign it this way.
            var style = Resources.GetStyleSheetAsset("LocalizationStyles");
            styleSheets.Add(style);

            m_AddButton = this.Q<Button>("addButton");
            m_AddButton.clicked += OnAddItem;

            m_RemoveButton = this.Q<Button>("removeButton");
            m_RemoveButton.clicked += OnRemoveSelected;
            m_RemoveButton.SetEnabled(false);

            m_ScrollView = this.Q<ScrollView>("listView");
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        public void RefreshList()
        {
            m_Init = true;
            m_ScrollView.Clear();
            var oldSelection = m_SelectedIndex;
            m_SelectedIndex = -1;

            RefreshListCallback?.Invoke(this);

            if (CreateHeaderContentsCallback != null)
            {
                var header = this.Q("header");
                header.Clear();
                header.Add(CreateHeaderContentsCallback(this));
            }

            var reorderManip = Draggable ? new ReorderManipulator(this) : null;

            // TODO: Can we do binding?
            if (!IsNestedList)
                ListProperty?.serializedObject.Update();

            for (int i = 0; i < Count; ++i)
            {
                var index = i;
                var item = new ListItem(Draggable);

                if (Draggable)
                {
                    item.Handle.AddManipulator(reorderManip);
                }

                item.RegisterCallback<MouseDownEvent>(evt =>
                {
                    Select(index);

                    // Prevent nested parent items being selected.
                    evt.StopPropagation();
                });

                CreateItemCallback?.Invoke(this, i, item.Container);
                m_ScrollView.Add(item);
            }

            Select(oldSelection);
            m_RemoveButton.SetEnabled(m_SelectedIndex != -1);

            if (Count == 0)
            {
                m_ScrollView.Add(CreateEmptyItem());
            }
        }

        public void Select(int index)
        {
            if (m_SelectedIndex == index)
                return;

            Deselect();
            if (index < 0 || index >= m_ScrollView.childCount)
            {
                return;
            }

            var selected = GetItemAtIndex(index);
            m_RemoveButton.SetEnabled(true);
            selected.SetSelected(true);
            m_SelectedIndex = index;
        }

        internal void Swap(int from, int to)
        {
            if (from == to)
                return;

            // Update the selection
            if (m_SelectedIndex == from)
                m_SelectedIndex = to;

            ReorderCallback?.Invoke(this, from, to);

            // The bindings are incorrect now so just refresh the list
            RefreshList();
        }

        internal ListItem GetItemAtIndex(int index)
        {
            ListItem item = null;
            if (index >= 0 && index < m_ScrollView.childCount)
            {
                item = m_ScrollView[index] as ListItem;
            }
            return item;
        }

        internal int GetItemIndex(ListItem item)
        {
            var index = m_ScrollView.IndexOf(item);
            return index;
        }

        void Deselect()
        {
            m_RemoveButton.SetEnabled(false);
            GetItemAtIndex(m_SelectedIndex)?.SetSelected(false);
            m_SelectedIndex = -1;
        }

        VisualElement CreateEmptyItem()
        {
            if (CreateEmptyCallback != null)
            {
                return CreateEmptyCallback(this);
            }
            var label = new Label("List is Empty");
            label.AddToClassList(EmptyUssClassName);
            return label;
        }

        void OnUndoRedo()
        {
            if (!IsNestedList)
                RefreshList();
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null || m_Init)
                return;

            RefreshList();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
                return;

            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        void OnAddItem()
        {
            int insertIndex = Selected == -1 ? Count : Mathf.Clamp(Selected, 0, Count);
            AddCallback?.Invoke(this, insertIndex);
        }

        void OnRemoveSelected()
        {
            var selected = GetItemAtIndex(m_SelectedIndex);
            if (selected != null)
            {
                m_ScrollView.RemoveAt(m_SelectedIndex);
                //m_ScrollView.Remove(selected);
                RemoveCallback?.Invoke(this, m_SelectedIndex);

                var newSelection = Mathf.Min(Count - 1, m_SelectedIndex);
                Deselect();
                Select(newSelection);
                RefreshList();
            }
        }
    }
}
