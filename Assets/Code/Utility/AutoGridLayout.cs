using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Awaken.TG.Utility {
    [ExecuteInEditMode]
    public class AutoGridLayout : MonoBehaviour {
        public GridLayoutGroup grid;
        public RectTransform viewport;
        public UnityAction<int> onConstraintChange;

        public bool updateOnGUI = false;

        void Start() {
            UpdateConstraint();
        }

        void OnGUI() {
            if (!updateOnGUI) {
                return;
            }

            UpdateConstraint();
        }

        void UpdateConstraint() {
            if (grid.constraint == GridLayoutGroup.Constraint.Flexible) {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            }

            float size;
            float cellSize;
            float spacing;
            if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount) {
                size = viewport.rect.width - grid.padding.left - grid.padding.right;
                spacing = grid.spacing.x;
                cellSize = grid.cellSize.x;
            } else {
                size = viewport.rect.height - grid.padding.bottom - grid.padding.top;
                spacing = grid.spacing.y;
                cellSize = grid.cellSize.y;
            }

            cellSize += spacing;
            size += spacing;

            int newConstraint = (int) (size / cellSize);
            if (newConstraint != grid.constraintCount) {
                grid.constraintCount = newConstraint;
                onConstraintChange?.Invoke(newConstraint);
            }
        }
    }
}