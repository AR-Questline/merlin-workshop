using UnityEngine.UIElements;
using System.Linq;
using static UnityEditor.Localization.UI.Toolkit.ReorderableList;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements.Experimental;

namespace UnityEditor.Localization.UI.Toolkit
{
    class ReorderManipulator : MouseManipulator
    {
        int m_DragStart;
        int m_CurrentIndex;
        ListItem m_Item;
        ReorderableList m_List;
        VisualElement m_CurrentTarget;
        float m_ScrollViewHeight;
        float m_DragAreaTop, m_DragAreaBottom;
        List<VisualElement> m_Children;
        bool m_Dragging;
        bool m_ListFrozen;

        public ReorderManipulator(ReorderableList list)
        {
            m_List = list;
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        static T GetAncestorOfType<T>(VisualElement visualElement) where T : class
        {
            if (visualElement is T t)
                return t;
            return visualElement.GetFirstAncestorOfType<T>();
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (m_Dragging)
            {
                evt.StopImmediatePropagation();
                return;
            }

            if (CanStartManipulation(evt))
            {
                m_CurrentTarget = evt.currentTarget as VisualElement;

                m_Item = GetAncestorOfType<ListItem>(m_CurrentTarget);

                m_ListFrozen = false;
                m_DragStart = m_List.GetItemIndex(m_Item);
                m_CurrentIndex = m_DragStart;
                m_List.Select(m_DragStart);
                m_Dragging = true;
                m_CurrentTarget.CaptureMouse();
                evt.StopPropagation();
            }
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (!m_Dragging)
                return;

            // We don't freeze immediately as it breaks focus when we refresh the list.
            if (!m_ListFrozen)
            {
                m_DragAreaBottom = FreezeScrollView();
                m_ListFrozen = true;
            }

            var newPos = Mathf.Clamp(m_Item.style.top.value.value + evt.mouseDelta.y, m_DragAreaTop, m_DragAreaBottom - m_Item.layout.height);

            int index = 0;
            float y = 0;
            float offset = 0;
            m_CurrentIndex = -1;
            foreach (ListItem child in m_Children)
            {
                if (child == m_Item)
                    continue;

                if (m_CurrentIndex == -1 && newPos < y + child.layout.height * 0.5f)
                {
                    offset += m_Item.layout.height;
                    m_CurrentIndex = index;
                }

                if (child.Animator?.to.top != offset)
                {
                    child.Animator?.Stop();
                    child.Animator?.Recycle();
                    child.Animator = child.experimental.animation.Start(new StyleValues { top = offset }, 500);
                    child.Animator.KeepAlive();
                }

                y += child.layout.height;
                offset += child.layout.height;
                index++;
            }

            if (m_CurrentIndex == -1)
                m_CurrentIndex = m_Children.Count - 1;

            m_Item.style.top = newPos;
            m_Item.BringToFront();
            evt.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (m_Dragging && CanStopManipulation(evt))
            {
                if (m_ListFrozen)
                {
                    RestoreScrollView();

                    if (m_DragStart != m_CurrentIndex)
                    {
                        m_List.Swap(m_DragStart, m_CurrentIndex);
                    }
                    else
                    {
                        m_List.RefreshList();
                    }
                }

                m_Dragging = false;
                m_CurrentTarget.ReleaseMouse();
                m_CurrentTarget = null;
                evt.StopPropagation();
            }
        }

        float FreezeScrollView()
        {
            // We don't have the position values but we do have the height and width so we can calculate the y offset for each item.
            float y = 0;
            m_DragAreaTop = 0;

            // Set the scroll view container to visible or we wont see the items
            m_Item.hierarchy.parent.style.overflow = Overflow.Visible;

            // Fix everything in place while we drag
            m_ScrollViewHeight = m_Item.parent.layout.height;
            m_Item.parent.style.height = m_ScrollViewHeight;
            m_Children = m_List.m_ScrollView.Children().ToList();
            foreach (var child in m_Children)
            {
                child.style.position = Position.Absolute;
                child.style.height = child.layout.height;
                child.style.width = child.layout.width;
                child.style.top = y;
                y += child.layout.height;
                child.BringToFront();
            }

            return y;
        }

        void RestoreScrollView()
        {
            m_Item.hierarchy.parent.style.overflow = Overflow.Hidden;
            m_ScrollViewHeight = m_Item.parent.layout.height;
            m_Item.parent.style.height = StyleKeyword.Auto;

            //// Restore the order of the items
            //m_List.m_ScrollView.Clear();

            //foreach(var child in m_Children)
            //{
            //    child.style.position = Position.Relative;
            //    child.style.height = StyleKeyword.Auto;
            //    child.style.width = StyleKeyword.Auto;
            //    child.style.top = StyleKeyword.Auto;
            //    m_List.m_ScrollView.Add(child);
            //}
        }
    }
}
