using System;
using System.Collections.Generic;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    public class VCQuickCustomAction : VCQuickUseAction {
        [Space(10f)]
        [SerializeField] CustomAction action;
        [SerializeField, LocStringCategory(Category.UI)] LocString actionName;
        [SerializeField, LocStringCategory(Category.UI)] LocString actionDescription;
        [SerializeField, LocStringCategory(Category.UI)] LocString actionPromptName;
        [SerializeField] GameObject icon;
        
        EventReference _hoverSound;

        readonly Dictionary<CustomAction, CustomActionData> _customActions = new() {
            { CustomAction.CallMount, new CustomActionData(CallMount, HeroHasMount) },
        };

        bool RequirementMet => _customActions[action].requirementMet?.Invoke() ?? false;

        protected override void Start() {
            base.Start();
            previewObject.SetActive(!RequirementMet);
            icon.SetActive(RequirementMet);
            _hoverSound = CommonReferences.Get.AudioConfig.ButtonSelectedSound;
        }
        
        static bool HeroHasMount() => Hero.Current.HasMount;

        static void CallMount() {
            var hero = Hero.Current;
            hero.Trigger(Hero.Events.HideWeapons, true);
            hero.Trigger(ToolInteractionFSM.Events.MountCalled, hero);
        }

        public override void OnHoverStart() {
            base.OnHoverStart();
            
            if (_hoverSound.IsNull) {
                return;
            }
            
            FMODManager.PlayOneShot(_hoverSound);
        }

        protected override void NotifyHover() { }

        protected override void OnShow() {
            if (RequirementMet) {
                VQuickUseWheel.Description.ShowCustomAction(actionName, actionDescription);
            }
        }

        protected override void OnHide() {
            VQuickUseWheel.Description.HideCustomAction(actionName);
        }

        public override OptionDescription Description => new(true, string.IsNullOrEmpty(actionPromptName) ? LocTerms.Use.Translate() : actionPromptName);
        
        public override void OnSelect(bool onClose) {
            if (RequirementMet) {
                _customActions[action].action?.Invoke();
                RadialMenu.Close();
            } else {
                FMODManager.PlayOneShot(_selectNegativeSound);
            }
        }

        enum CustomAction : byte {
            CallMount = 0,
        }
        
        struct CustomActionData {
            public readonly Action action;
            public readonly Func<bool> requirementMet;
            
            public CustomActionData(Action action, Func<bool> requirementMet) {
                this.action = action;
                this.requirementMet = requirementMet;
            }
        }
    }
}