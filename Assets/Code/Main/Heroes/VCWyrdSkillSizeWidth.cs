using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public class VCWyrdSkillSizeWidth : VCWyrdSkillActive {
        [SerializeField] RectTransform wyrdSkillAdjustmentTarget;
        [SerializeField] float withWyrdSkillWidth = 280f;
        [SerializeField] float withoutWyrdSkillWidth = 310f;

        protected override void RefreshState() {
            float x = _isWyrdSkillActive ? withWyrdSkillWidth : withoutWyrdSkillWidth;
            float y = wyrdSkillAdjustmentTarget.sizeDelta.y;
            wyrdSkillAdjustmentTarget.sizeDelta = new Vector2(x, y);
        }
        
        [Button]
        void CaptureWithWyrdSkillWidth() {
            withWyrdSkillWidth = wyrdSkillAdjustmentTarget.sizeDelta.x;
        }

        [Button]
        void CaptureWithoutWyrdSkillWidth() {
            withoutWyrdSkillWidth = wyrdSkillAdjustmentTarget.sizeDelta.x;
        }
        
        [Button]
        void SetWithWyrdSkillWidth() {
            wyrdSkillAdjustmentTarget.sizeDelta = new Vector2(withWyrdSkillWidth, wyrdSkillAdjustmentTarget.sizeDelta.y);
        }

        [Button]
        void SetWithoutWyrdSkillWidth() {
            wyrdSkillAdjustmentTarget.sizeDelta = new Vector2(withoutWyrdSkillWidth, wyrdSkillAdjustmentTarget.sizeDelta.y);
        }

        void Reset() {
            wyrdSkillAdjustmentTarget = GetComponent<RectTransform>();
        }
    }
}
    