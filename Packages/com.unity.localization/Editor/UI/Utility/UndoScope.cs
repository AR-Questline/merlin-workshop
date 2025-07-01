using System;

namespace UnityEditor.Localization
{
    readonly struct UndoScope : IDisposable
    {
        readonly int m_Group;
        readonly bool m_CreateUndo;

        public UndoScope(string name, bool createUndo)
        {
            m_CreateUndo = createUndo;
            m_Group = 0;

            if (m_CreateUndo)
            {
                m_Group = Undo.GetCurrentGroup();
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName(name);
            }
        }

        public void Dispose()
        {
            if (m_CreateUndo)
                Undo.CollapseUndoOperations(m_Group);
        }
    }
}
