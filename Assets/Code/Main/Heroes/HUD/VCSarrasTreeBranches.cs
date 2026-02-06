using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.Development.SarrasPowers;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.Components.PadShortcuts;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCSarrasTreeBranches : ViewComponent<Hero>, IUIPlayerInput {
        [SerializeField] Image fillImage;
        [SerializeField] float buttonHoldDuration = 0.8f;
        [SerializeField] GameObject parentObject;
        [SerializeField] Image firstBranchIcon;
        [SerializeField] Image secondBranchIcon;
        [SerializeField] Image thirdBranchIcon;
        [SerializeField, ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true)] ShareableSpriteReference mageIconReference;
        [SerializeField, ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true)] ShareableSpriteReference rogueIconReference;
        [SerializeField, ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true)] ShareableSpriteReference warriorIconReference;
        
        Image[] _branchIcons;
        readonly SpriteReference[] _spriteReferences = new SpriteReference[3];
        ShareableSpriteReference[] _shareableSpriteReferences;
        TapShortcut _tapShortcut;
        HoldWithFillShortcut _holdWithFillShortcut;

        readonly TalentTreeBranchType[] _branchTypesBaseOrder = {
            TalentTreeBranchType.SarrasMage,
            TalentTreeBranchType.SarrasRogue,
            TalentTreeBranchType.SarrasWarrior
        };

        public IEnumerable<KeyBindings> PlayerKeyBindings => KeyBindings.Gameplay.Interact.Yield();
        public int InputPriority => -1;
        public bool IsValid => this.IsValidForUIHandle();

        protected override void OnAttach() {
            _branchIcons = new[] { firstBranchIcon, secondBranchIcon, thirdBranchIcon };
            _shareableSpriteReferences = new[] { mageIconReference, rogueIconReference, warriorIconReference };
            
            Target.AfterFullyInitialized(() => {
                var sarrasHeroTreeBranches = Target.Development.SarrasHeroTreeBranches;
                sarrasHeroTreeBranches.ListenTo(SarrasHeroTreeBranches.Events.TalentTreeBranchChanged, UpdateBranchIcons, this);
                UpdateBranchIcons(sarrasHeroTreeBranches.CurrentlySelected);
                AddShortcuts();
            });
            Target.ListenTo(Model.Events.BeforeDiscarded, AfterDiscarded, this);
        }

        // unfortunately advanced hack. GameUI handles keyboard, PlayerInput handles Gamepad
        void AddShortcuts() {
            _tapShortcut = new TapShortcut(KeyBindings.UI.HUD.ChangeActiveSarrasBranch, NextBranch, ControlSchemeFlag.KeyboardAndMouse);
            World.Only<GameUI>().AddElement(_tapShortcut);
            
            // it's not used as a regular Element in this case
            _holdWithFillShortcut = new HoldWithFillShortcut(KeyBindings.Gameplay.Interact, fillImage, buttonHoldDuration, NextBranch, ControlSchemeFlag.Gamepad);
            World.Only<PlayerInput>().RegisterPlayerInput(this, Target);
            return;
            
            void NextBranch() {
                Target.Development.SarrasHeroTreeBranches.NextBranch();
                FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.SarrasSkillTreeBranchSelectedSound);
            }
        }
        
        void UpdateBranchIcons(TalentTreeBranchType activeBranch) {
            var isActive = activeBranch != TalentTreeBranchType.None;
            parentObject.SetActiveOptimized(isActive);
            
            if (!isActive) {
                return;
            }
            
            var activeIndex = System.Array.IndexOf(_branchTypesBaseOrder, activeBranch);
            for (var i = 0; i < _branchIcons.Length; i++) {
                var nextIndex = (activeIndex + i) % _branchTypesBaseOrder.Length;
                _spriteReferences[i]?.Release();
                _spriteReferences[i] = _shareableSpriteReferences[nextIndex].Get();
                _spriteReferences[i].SetSprite(_branchIcons[i]);
            }
        }

        void AfterDiscarded() {
            foreach (var spriteReference in _spriteReferences) {
                spriteReference?.Release();
            }
            _tapShortcut?.Discard();
        }

        public UIResult Handle(UIEvent evt) {
            return RewiredHelper.IsGamepad ? _holdWithFillShortcut.Handle(evt) : UIResult.Ignore;
        }
    }
}