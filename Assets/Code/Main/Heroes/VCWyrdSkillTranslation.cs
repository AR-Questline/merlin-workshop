using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public class VCWyrdSkillTranslation : VCWyrdSkillActive {
        [SerializeField] RectTransform wyrdSkillAdjustmentTarget;
        [SerializeField] Vector2 withWyrdSkillPosition = new(0f, 0f);
        [SerializeField] Vector2 withoutWyrdSkillPosition = new(0f, 0f);

        protected override void RefreshState() {
            wyrdSkillAdjustmentTarget.anchoredPosition = _isWyrdSkillActive ? withWyrdSkillPosition : withoutWyrdSkillPosition;
        }

        [Button]
        void CaptureWithWyrdSkillPosition() {
            withWyrdSkillPosition = wyrdSkillAdjustmentTarget.anchoredPosition;
        }

        [Button]
        void CaptureWithoutWyrdSkillPosition() {
            withoutWyrdSkillPosition = wyrdSkillAdjustmentTarget.anchoredPosition;
        }

        [Button]
        void MoveToWithWyrdSkillPosition() {
            wyrdSkillAdjustmentTarget.anchoredPosition = withWyrdSkillPosition;
        }
        
        [Button]
        void MoveToWithoutWyrdSkillPosition() {
            wyrdSkillAdjustmentTarget.anchoredPosition = withoutWyrdSkillPosition;
        }

        void Reset() {
            wyrdSkillAdjustmentTarget = GetComponent<RectTransform>();
        }
    }
}