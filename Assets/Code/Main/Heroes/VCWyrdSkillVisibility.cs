using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public class VCWyrdSkillVisibility : VCWyrdSkillActive {
        const float FadeSpeed = 2f;
        
        [SerializeField] CanvasGroup wyrdSkillBarCanvasGroup;
        [SerializeField] CanvasGroup heroBarsCanvasGroup;
        
        protected override void RefreshState() {
            wyrdSkillBarCanvasGroup.alpha = _isWyrdSkillActive ? 1 : 0;
            wyrdSkillBarCanvasGroup.gameObject.SetActive(_isWyrdSkillActive);
        }

        void Update() {
            float maxDelta = FadeSpeed * Time.unscaledDeltaTime;
            if (_isWyrdSkillActive && wyrdSkillBarCanvasGroup.alpha < 1) {
                wyrdSkillBarCanvasGroup.alpha = Mathf.MoveTowards(wyrdSkillBarCanvasGroup.alpha, 1, maxDelta);
            }
        }
    }
}