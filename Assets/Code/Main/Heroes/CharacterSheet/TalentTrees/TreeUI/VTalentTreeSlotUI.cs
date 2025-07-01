using System;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.TreeUI {
    [UsesPrefab("CharacterSheet/TalentTree/TreeUI/" + nameof(VTalentTreeSlotUI))]
    public class VTalentTreeSlotUI : View<TalentTreeSlotUI> {
        [SerializeField] ButtonConfig talentSlot;
        [SerializeField] Image runeIcon;
        [SerializeField] GameObject lockedIcon;
        [SerializeField] TalentTreeLevelSlot[] levelSlots = new TalentTreeLevelSlot[5];

        public VTalentTreeSlotUI[] Children => _children ??= Target.FindTalentChildren().ToArray();
        public VTalentTreeSlotUI Parent { get; private set; }
        TalentTreeUI TalentTreeUI => Target.ParentModel;
        
        VTalentTreeSlotUI[] _children;
        Tween _runeColorTween;
        Tween _lineColorTween;
        
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Talent.Events.TalentChanged, this, RefreshTalent);
            talentSlot.InitializeButton(OnClicked);
            talentSlot.button.OnHover += OnSlotHovered;
            talentSlot.button.OnSelected += OnSlotSelected;

            for (int index = 0; index < levelSlots.Length; index++) {
                levelSlots[index].Icon.color = ARColor.DarkerGrey;

                if (index > Target.Talent.MaxLevel - 1 && index < levelSlots.Length) {
                    levelSlots[index].Content.SetActiveOptimized(false);
                }
            }

            SetupTalent();
        }

        protected override void OnMount() {
            SetupLine();
            RefreshLines().Forget();
            talentSlot.button.ClearAllOnClickAudioFeedback();
        }

        void SetupLine() {
            if (Target.Talent.Parent == null) return;
            
            Parent = Target.FindTalentParent();
        }
        
        public void Focus() {
            World.Only<Focus>().Select(talentSlot.button);
        }

        public void ColorLine(ARColor color, float duration = UITweens.ColorChangeDuration) {
            
        }

        void OnSlotHovered(bool isHovering) {
            if (RewiredHelper.IsGamepad) return;
            Select(isHovering);
        }
        
        void OnSlotSelected(bool isSelected) {
            if (RewiredHelper.IsGamepad == false) return;
            Select(isSelected);
        }

        void OnClicked() {
            talentSlot.button.PlayClickAudioFeedback(Target.Talent.CanBeUpgraded && TalentTree.IsUpgradeAvailable, false);
        }

        void Select(bool state) {
            TalentTreeUI.SelectTalent(Target, state);
        }
        
        void SetupTalent() {
            _runeColorTween = runeIcon.DOGraphicColor(Target.Talent.IsUpgraded 
                ? Target.IsLocked ? ARColor.MainGrey : ARColor.MainAccent 
                : Target.IsLocked ? ARColor.MainGrey : ARColor.MainWhite, 
                UITweens.ColorChangeDuration);
            lockedIcon.SetActive(Target.IsLocked);
            
            for (int index = 0; index < Target.Talent.MaxLevel; index++) {
                levelSlots[index].CanvasGroup.alpha = Target.IsLocked ? 0.25f : 1f;
                levelSlots[index].Icon.color = index > Target.Talent.EstimatedLevel - 1 ? ARColor.DarkerGrey : ARColor.MainAccent;
            }
        }

        void RefreshTalent() {
            SetupTalent();
            RefreshLines().Forget();
        }

        async UniTaskVoid RefreshLines() {
            if (await AsyncUtil.DelayFrame(Target) == false) return;

            foreach (var child in Children) {
                bool relationInactive = Target.IsLocked || (child.Target.IsLocked && Target.IsLocked == false);
                bool relationActive = child.Target.IsUpgraded && Target.IsUpgraded;

                child.ColorLine(relationInactive ? ARColor.MainGrey : relationActive ? ARColor.MainAccent : ARColor.LightGrey);
            }    
        }

        protected override IBackgroundTask OnDiscard() {
            UITweens.DiscardTween(ref _runeColorTween);
            UITweens.DiscardTween(ref _lineColorTween);
            return base.OnDiscard();
        }
    }

    [Serializable]
    public struct TalentTreeLevelSlot {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] GameObject content;
        [SerializeField] Image icon;
        
        public CanvasGroup CanvasGroup => canvasGroup;
        public GameObject Content => content;
        public Image Icon => icon;
    }
}