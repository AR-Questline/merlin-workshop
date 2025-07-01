using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Sources;
using Awaken.Utility.Animations;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.WyrdArthur.SoulsOverview {
    [UsesPrefab("CharacterSheet/WyrdArthur/" + nameof(VWyrdArthurPower))]
    public class VWyrdArthurPower : View<WyrdArthurPower>, IUIAware, UnityUpdateProvider.IWithUpdateGeneric {
        [SerializeField, TemplateType(typeof(TalentTreeTemplate))] TemplateReference talentTree;
        [SerializeField] Transform treeParent;
        [SerializeField] Transform contentParent;

        [Title("Gamepad input")] 
        [SerializeField, Range(0, 2)] float directionWeight = 0.9f;
        [SerializeField, Range(0, 1)] float stickDirectionThreshold = 0.7f;
        [SerializeField, Range(0, 1)] float dpadDirectionThreshold = 0.5f;
        [SerializeField, Range(0, 2)] float distanceWeight = 0.1f;
        [SerializeField] float gamepadAxisThreshold = 0.5f; 
        [SerializeField] float inputCooldown = 0.2f; 
        
        public override Transform DetermineHost() => Target.ParentModel.View.PowerTalentHost;
        public Transform TreeParent => treeParent;

        TalentTreeTemplate _tree;
        public TalentTreeTemplate Tree => _tree = _tree ? _tree : talentTree.Get<TalentTreeTemplate>();

        Hero Hero => Hero.Current;
        VWyrdTalentTreePattern _spawnedPattern;
        NavigationData[] _selectableElements;
        NavigationData _currentSelected;
        float _lastInputTime;
        Vector2 _lastInputDir;
        
        protected override void OnInitialize() {
            _tree = talentTree.Get<TalentTreeTemplate>();
        }
        
        public void SetupPattern(VWyrdTalentTreePattern pattern) {
            _spawnedPattern = pattern;
            _selectableElements = new NavigationData[_spawnedPattern.TalentNodes.Count + _spawnedPattern.WyrdTalentTree.Count];
            PrepareSubtrees();
            UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, Target));
        }
        
        public void SpawnSlot(WyrdTalentTreeSlotUI talentSlot, int index) {
            var talentNode = _spawnedPattern.TalentNodes[index];
            VWyrdTalentTreeSlotUI slot = World.SpawnView<VWyrdTalentTreeSlotUI>(talentSlot, true, true, talentNode.UISlot);
            var slotNaviData = new NavigationData { focusTarget = slot.Button, naviTarget = slot.Button };
            _selectableElements[_spawnedPattern.WyrdTalentTree.Count + index] = slotNaviData;
        }
        
        void PrepareSubtrees() {
            NavigationData firstFocusable = null;
            
            for (int index = 0; index < _spawnedPattern.WyrdTalentTree.Count; index++) {
                WyrdTalentSubTree subTree = _spawnedPattern.WyrdTalentTree[index];
                var subTreeNaviData = new NavigationData { focusTarget = subTree.ButtonConfig.button, naviTarget = subTree.WyrdMainTalentSlot };
                _selectableElements[index] = subTreeNaviData;
                
                bool active = Hero.Development.WyrdSoulFragments.UnlockedFragments.Contains(subTree.WyrdTalentType.FragmentType);
                subTree.SetSectionState(active);

                subTree.ButtonConfig.InitializeButton();
                subTree.ButtonConfig.button.OnHover += (isHovering) => OnSlotHovered(isHovering, subTree);
                subTree.ButtonConfig.button.OnSelected += (isSelected) => OnSlotSelected(isSelected, subTree);
                firstFocusable ??= subTreeNaviData.focusTarget.isActiveAndEnabled ? subTreeNaviData : null;
            }
            
            if (firstFocusable != null) {
                FocusCurrentSubtree(firstFocusable).Forget();
            }
        }
        
        void OnSlotHovered(bool isHovering, WyrdTalentSubTree subTree) {
            if (RewiredHelper.IsGamepad) return;
            ShowTooltipOnHover(isHovering, subTree);
        }
        
        void OnSlotSelected(bool isSelected, WyrdTalentSubTree subTree) {
            if (RewiredHelper.IsGamepad == false) return;
            ShowTooltipOnHover(isSelected, subTree);
        }
        
        void ShowTooltipOnHover(bool hover, WyrdTalentSubTree subTree) {
            var tooltip = Target.Tooltip;
            if (hover) {
                tooltip.SetPosition(subTree.TooltipPositionLeft, subTree.TooltipPositionRight);
                tooltip.Show(subTree.WyrdMainTalent.Name, subTree.WyrdMainTalent.GetLevel(1).GetDebugDescriptionBlueprint());
            } else {
                tooltip.Hide();
            }
        }
        
        async UniTaskVoid FocusCurrentSubtree(NavigationData toFocus) {
            if (await AsyncUtil.DelayFrame(Target)) {
                SetSelected(toFocus);
            }
        }
        
        // Axis input handling
        public void UnityUpdate() {
            if (!RewiredHelper.IsGamepad) return;

            Vector2 input = Vector2.zero;//RewiredHelper.Player.GetAxis2DRaw("Horizontal", "Vertical");
            if (Time.unscaledTime - _lastInputTime < inputCooldown) return;
            
            if (input.magnitude >= gamepadAxisThreshold && input != _lastInputDir) {
                ProcessNavi(input, stickDirectionThreshold);
                _lastInputDir = input;
                _lastInputTime = Time.unscaledTime;
            }
            
            if (input.magnitude < gamepadAxisThreshold) {
                _lastInputDir = Vector2.zero;
            }
        }
        
        void SetSelected(NavigationData newSelection) {
            _currentSelected = newSelection;
            World.Only<Focus>().Select(newSelection.focusTarget);
        }
        
        void ProcessNavi(Vector2 inputDir, float dirThreshold) {
            FindBestCandidate(inputDir, dirThreshold, out NavigationData bestCandidate);
            
            if (bestCandidate != null) {
                SetSelected(bestCandidate);
            }
        }

        void FindBestCandidate(Vector2 inputDir, float dirThreshold, out NavigationData bestCandidate) {
            Vector2 currentPos = _currentSelected.naviTarget.transform.position;
            bestCandidate = null;
            float bestScore = Mathf.Infinity;
            
            foreach (var candidate in _selectableElements) {
                if (candidate == _currentSelected || !candidate.focusTarget.isActiveAndEnabled) continue;

                Vector2 candidatePos = candidate.naviTarget.transform.position;
                Vector2 toCandidate = candidatePos - currentPos;

                // Check if the candidate is approximately in the direction given by the input
                float dot = Vector2.Dot(toCandidate.normalized, inputDir.normalized);
                
                // Reject elements that are at an angle greater than the threshold
                if (dot < dirThreshold) continue;
                
                float score = (toCandidate.magnitude * distanceWeight) / (dot * directionWeight);
                
                if (score < bestScore) {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }
        }
        
        public UIResult Handle(UIEvent evt) {
            if (Target is { HasBeenDiscarded: false } && !RewiredHelper.IsGamepad) return UIResult.Ignore;
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
                ProcessNavi(input, dpadDirectionThreshold); 
                return UIResult.Accept;
            }
            
            return UIResult.Ignore;
        }
        
        protected override IBackgroundTask OnDiscard() {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            return base.OnDiscard();
        }

        class NavigationData {
            public ARButton focusTarget;
            public Component naviTarget;
        }
    }
}