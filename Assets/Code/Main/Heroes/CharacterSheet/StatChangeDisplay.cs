using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using Awaken.Utility;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    public class StatChangeDisplay : MonoBehaviour {
        public TextMeshProUGUI statNameText;
        public TextMeshProUGUI statChangeText;
        public bool showIconOnly;

        Tween _tween;
        
        Color LockedColor => new(0.385f, 0.385f, 0.385f, 1);
        string LockedName => LocTerms.LockedStat.Translate();

        public void SetTextsInstant(StatType stat, int statValue, bool locked = false) {
            var statName = GetStatName(stat, locked);
            statName = statName.FormatSprite();
            SetTextsInstant(statName, statValue, locked);
        }

        public void SetTextsInstant(string statName, int statValue, bool locked = false) {
            if (statNameText != null) {
                statNameText.text = statName;
            }
            statChangeText.text = statValue.ToString();
            statChangeText.gameObject.SetActive(!locked);
        }

        public void SetTexts(StatType stat, int statChange, int startValue = 0, bool locked = false) {
            var statName = GetStatName(stat, locked);
            statName = statName.FormatSprite();
            SetTexts(statName, statChange, startValue, locked);
        }
        
        public void SetTexts(string statName, int statChange, int startValue = 0, bool locked = false) {
            if (statNameText != null) {
                statNameText.text = statName;
            }
            statChangeText.gameObject.SetActive(!locked);

            if (!locked) {
                int i = startValue;
                _tween = DOTween.To(() => i, x => {
                    i = x;
                    statChangeText.text = i.ToString();
                }, statChange, Mathf.Clamp(Mathf.Log(statChange + 1) / 3.5f, 2f, 5f)).SetEase(Ease.InOutQuad);
            }
        }

        public void Complete() {
            _tween?.Complete();
            _tween = null;
        }
        
        string GetStatName(StatType stat, bool locked) {
            return showIconOnly ? stat.IconTag.ColoredText(ARColor.LightGrey) : locked ? 
                stat.IconTagNoTooltip.ColoredText(LockedColor) + LockedName.ColoredText(LockedColor) : 
                stat.IconTag.ColoredText(stat.Color) + stat.DisplayName;
        }
    }
}