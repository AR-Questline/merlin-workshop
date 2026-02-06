using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Sources;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.TreeUI {
    [UsesPrefab("CharacterSheet/TalentTree/" + nameof(VTalentTreeUI))]
    public class VTalentTreeUI : View<TalentTreeUI>, IUIAware, UnityUpdateProvider.IWithUpdateGeneric {
        static readonly int GreyScale = Shader.PropertyToID("_Grayscale");

        [SerializeField] Transform treeParent;
        [SerializeField] Transform contentParent;
        [SerializeField] VCTalentTreeTooltip tooltipParent;
        [SerializeField] Transform zoomInParent;
        [SerializeField] Image icon;
        [SerializeField] Image subTreeIcon;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(Sprite), typeof(Texture2D)}, true)] 
        ShareableSpriteReference lineSpriteReference;

        [Title("Gamepad input")] 
        [SerializeField, Range(0, 2)] float directionWeight = 0.9f;
        [SerializeField, Range(0, 1)] float stickDirectionThreshold = 0.7f;
        [SerializeField, Range(0, 1)] float dpadDirectionThreshold = 0.5f;
        [SerializeField, Range(0, 2)] float distanceWeight = 0.1f;
        [SerializeField, Range(0, 1)] float stickNeighbourFactor = 0.75f;
        [SerializeField, Range(0, 1)] float dpadNeighbourFactor = 0.8f;
        [SerializeField] float gamepadAxisThreshold = 0.5f; 
        [SerializeField] float inputCooldown = 0.2f; 

        VTalentTreePattern _pattern;
        Vector3 _contentPosition;
        Material _material;
        
        float _lastInputTime;
        Vector2 _lastInputDir;
        VTalentTreeSlotUI[] _selectableElements;
        VTalentTreeSlotUI _currentSelected;
        AlwaysPresentHandlers _presentHandler;
        SkillTalentSubTree _currentSubTreeBase;
        IVTalentOverview _vTalentOverviewUI;
        SpriteReference _lineSpriteReferenceCache;
        
        public Transform TreeParent => treeParent;
        public override Transform DetermineHost() => Target.ParentModel.View<ITabParentView>().ContentHost;
        public SpriteReference LineSpriteReference => _lineSpriteReferenceCache ?? lineSpriteReference.Get();
        public bool IsValid => this.IsValidForUIHandle();
        
        TalentTreeTemplate Template => Target.ParentModel.Tree;
        bool IsTreeIconSet => Template.Icon is { IsSet: true };

        protected override void OnFullyInitialized() {
            _material = new Material(subTreeIcon.material);
            subTreeIcon.material = _material;
            subTreeIcon.material.SetFloat(GreyScale, 1);
            subTreeIcon.TrySetActiveOptimized(false);
            _vTalentOverviewUI = Target.ParentModel.View<IVTalentOverview>();
        }

        public void SetupPattern(VTalentTreePattern vPattern) {
            SetupIcon();
            _contentPosition = contentParent.position;
            _pattern = vPattern;
            PrepareSubtrees();
            _selectableElements = new VTalentTreeSlotUI[Target.CurrentTable.TreeTemplate.TalentNodes.Count];
        }
        
        void SetupIcon() {
            if (!IsTreeIconSet) {
                return;
            }
            
            var treeIcon = Target.ParentModel.Tree.Icon;
            if (Template.ShowMainIcon) {
                treeIcon.RegisterAndSetup(this, icon);
            } else {
                icon.TrySetActiveOptimized(false);
            }
            
            if (Template.ShowSubtreeIcon) {
                treeIcon.RegisterAndSetup(this, subTreeIcon);
            } else {
                subTreeIcon.TrySetActiveOptimized(false);
            }
        }
        
        void PrepareSubtrees() {
            foreach (var subTree in _pattern.SkillTalentTree) {
                subTree.SetupName();
                subTree.ButtonConfig.InitializeButton(() => ChooseSubtree(subTree));
            }
        }

        void ChooseSubtree(SkillTalentSubTree subTree) {
            _currentSubTreeBase = subTree;
            ZoomSubtree(true);
            Target.GoToSubTree();
        }
        
        public void Back() {
            ZoomSubtree(false);
            if (RewiredHelper.IsGamepad) {
                FocusCurrentSubtree(_currentSubTreeBase.ButtonConfig.button).Forget();
            }
            _currentSubTreeBase = default;
        }

        void ZoomSubtree(bool zoomIn) {
            _vTalentOverviewUI.SetupRequiredInfo(!zoomIn);
            _currentSubTreeBase.SetNameActive(!zoomIn);
            ToggleIcon(!zoomIn);
            
            Target.ParentModel.RefreshFeedback(!zoomIn);
            Target.Trigger(TalentTreeUI.Events.TreeZoomedIn, zoomIn);

            if (zoomIn) {
                ZoomInSubtree();
            } else {
                ZoomOutSubtree();
            }
        }
        
        void ToggleIcon(bool mainIconState) {
            if (!IsTreeIconSet) {
                return;
                
            }
            
            if (Template.ShowMainIcon) {
                icon.TrySetActiveOptimized(mainIconState);
            }
            
            if (Template.ShowSubtreeIcon) {
                subTreeIcon.TrySetActiveOptimized(!mainIconState);
            }
        }

        void ZoomOutSubtree() {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            _presentHandler.Discard();
            _pattern.ShowOthersSubtrees();

            contentParent.localScale = Vector3.one;
            contentParent.position = _contentPosition;
            HideTooltip();
        }
        
        void ZoomInSubtree() {
            UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
            _presentHandler = World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, Target));
            
            _pattern.HideOthersSubtree(_currentSubTreeBase);
            contentParent.localScale = Vector3.one * _currentSubTreeBase.ZoomInScale;
            Vector3 iconOffset = _currentSubTreeBase.IconPosition - contentParent.position.XY();
            contentParent.position = zoomInParent.position - iconOffset;

            if (_pattern.FirstSlot != null && _pattern.TryGetComponentInChildren<VTalentTreeSlotUI>(out var firstSlot)) {
                SetSelected(firstSlot);
            }
        }
        
        public async UniTaskVoid FocusCurrentSubtree(Component toFocus) {
            toFocus = toFocus ? toFocus : _pattern.FirstSelectedSubtree;
            if (await AsyncUtil.DelayFrame(Target)) {
                World.Only<Focus>().Select(toFocus);
            }
        }
        
        public void SetupSlot(VTalentTreeSlotUI slotView, int index) {
            _selectableElements[index] = slotView;
        }
        
        public void ShowTooltip(Talent talent) {
            tooltipParent.ShowTooltip(talent).Forget();
        }
        
        public void HideTooltip() {
            tooltipParent.HideTooltip();
        }
        
        // Axis input handling
        public void UnityUpdate() {
            if (!Target.InCategory && !RewiredHelper.IsGamepad) return;

            Vector2 input = Vector3.zero;//RewiredHelper.Player.GetAxis2DRaw("Horizontal", "Vertical");
            if (Time.unscaledTime - _lastInputTime < inputCooldown) return;
            
            if (input.magnitude >= gamepadAxisThreshold && input != _lastInputDir) {
                ProcessNavi(input, stickDirectionThreshold, stickNeighbourFactor);
                _lastInputDir = input;
                _lastInputTime = Time.unscaledTime;
            }
            
            if (input.magnitude < gamepadAxisThreshold) {
                _lastInputDir = Vector2.zero;
            }
        }
        
        public void SetSelected(VTalentTreeSlotUI newSelection) {
            _currentSelected = newSelection;
            _currentSelected.Focus();
        }
        
        void ProcessNavi(Vector2 inputDir, float dirThreshold, float neighbourFactor) {
            FindBestCandidate(inputDir, dirThreshold, neighbourFactor, out VTalentTreeSlotUI bestCandidate);
            
            if (bestCandidate != null) {
                SetSelected(bestCandidate);
            }
        }

        void FindBestCandidate(Vector2 inputDir, float dirThreshold, float neighbourFactor, out VTalentTreeSlotUI bestCandidate) {
            Vector2 currentPos = _currentSelected.transform.position;
            bestCandidate = null;
            float bestScore = Mathf.Infinity;
            
            foreach (var candidate in _selectableElements) {
                if (candidate == _currentSelected || !candidate.isActiveAndEnabled) continue;

                Vector2 candidatePos = candidate.transform.position;
                Vector2 toCandidate = candidatePos - currentPos;

                // Check if the candidate is approximately in the direction given by the input
                float dot = Vector2.Dot(toCandidate.normalized, inputDir.normalized);
                
                // Reject elements that are at an angle greater than the threshold
                if (dot < dirThreshold) continue;
                
                float score = (toCandidate.magnitude * distanceWeight) / (dot * directionWeight);

                // Bonus for slots that are children or parent of the current selected slot
                if (_currentSelected.Children.Contains(candidate) || _currentSelected.Parent == candidate) {
                    score *= neighbourFactor;
                }

                if (score < bestScore) {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }
        }
        
        public UIResult Handle(UIEvent evt) {
            if (Target is { InCategory: false } && !RewiredHelper.IsGamepad) return UIResult.Ignore;
            Vector2 input = Vector2.zero;

            if (evt is UIKeyDownAction action) {
                if (action.Name == KeyBindings.Gamepad.DPad_Down) {
                    input = Vector2.down;
                }

                if (action.Name == KeyBindings.Gamepad.DPad_Up) {
                    input = Vector2.up;
                }

                if (action.Name == KeyBindings.Gamepad.DPad_Left) {
                    input = Vector2.left;
                }

                if (action.Name == KeyBindings.Gamepad.DPad_Right) {
                    input = Vector2.right;
                }
            }
            
            if (input != Vector2.zero) {
                ProcessNavi(input, dpadDirectionThreshold, dpadNeighbourFactor); 
                return UIResult.Accept;
            }
            
            return UIResult.Ignore;
        }
        
        protected override IBackgroundTask OnDiscard() {
            Destroy(_material);
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            _lineSpriteReferenceCache?.Release();
            _lineSpriteReferenceCache = null;
            return base.OnDiscard();
        }
    }
}