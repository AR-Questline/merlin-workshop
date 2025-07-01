using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.UI;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility.Animations;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using DG.Tweening;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.MVC.UI.Handlers.Tooltips {
    /// <summary>
    /// Generic tooltip view.
    /// </summary>
    [UsesPrefab("UI/Tooltip/VTooltip")]
    public class VTooltip : View<ITooltip> {
        public override Transform DetermineHost() {
            if (Target.Constructor.AttachToParent && Target.Parent != null) {
                return Target.Parent.View<VTooltip>().subTooltipParent;
            }
            return Services.Get<ViewHosting>().OnTooltipCanvas();
        }

        public CanvasGroup canvasGroup;
        public Transform contentParent;
        public Transform horizontalContentParent;
        public Transform splittedParent;
        public Transform subTooltipParent;
        
        bool _hadOneUpdate;
        RectTransform _canvasTransform;
        RectTransform _contentTransform;
        RectTransform _horizontalContentTransform;
        Vector3 _defaultContentLocalPosition;
        Dictionary<TooltipValue, GameObject> _spawnedContents = new Dictionary<TooltipValue, GameObject>();
        Tween _alphaTween;

        // === Initialize
        protected override void OnInitialize() {
            _canvasTransform = Services.Get<CanvasService>().MainTransform;
            _contentTransform = (RectTransform) canvasGroup.transform;
            _horizontalContentTransform = (RectTransform)horizontalContentParent.transform;
            _defaultContentLocalPosition = _contentTransform.localPosition;
            
            Target.ListenTo(Model.Events.AfterChanged, UpdateContent, this);
            UpdateContent();
            SyncPosition();
            canvasGroup.alpha = 0f;
            _alphaTween = DOTween.To(() => canvasGroup.alpha, v => canvasGroup.alpha = v, 1, 0.2f).SetUpdate(true);
        }

        void UpdateContent() {
            var contentToSpawn = Target.Constructor.ElementsToSpawn.Where(e => !_spawnedContents.Keys.Contains(e)).ToArray();
            var prefabsToRemove = _spawnedContents.Where(k => !Target.Constructor.ElementsToSpawn.Contains(k.Key)).ToArray();

            foreach (var newPrefab in contentToSpawn) {
                SpawnNewContentView(newPrefab);
            }

            foreach (var prefabToRemove in prefabsToRemove) {
                RemoveContentView(prefabToRemove.Key, prefabToRemove.Value);
            }

            if (contentToSpawn.Any()) {
                EnsureContentOrder();
            }

            if (Target.Constructor.ElementsToSpawn.Any() && !gameObject.activeSelf) {
                gameObject.SetActive(true);
            }else if (!Target.Constructor.ElementsToSpawn.Any() && gameObject.activeSelf) {
                gameObject.SetActive(false);
            }
            
            _alphaTween.Kill();
            _alphaTween = DOTween.To(() => canvasGroup.alpha, v => canvasGroup.alpha = v, 1, 0.2f).SetUpdate(true);
        }

        void SpawnNewContentView(TooltipValue tooltip) {
            var prefabPath = World.ResourcesPrefabPath(tooltip.Element.PrefabName);
            var loadedPrefab = Resources.Load<GameObject>(prefabPath);
            var spawned = UnityEngine.Object.Instantiate(loadedPrefab, splittedParent);
            var element = spawned.GetComponentInChildren<VCTooltipElement>();
            if (element == null) {
                Destroy(spawned);
                Log.Important?.Error($"Prefab at path {prefabPath} do not have VCTooltipElement");
                return;
            }

            foreach (var vc in spawned.GetComponentsInChildren<ViewComponent>()) {
                vc.Attach(Services, Target, this);
            }
            element.UpdateContent(tooltip.Value);
            Transform verticalParent = element.TooltipElement.AutoBackground ? contentParent : splittedParent;
            element.transform.SetParent(element.TooltipElement == TooltipElement.HorizontalCard ? horizontalContentParent : verticalParent);
            if (element.TooltipElement.AutoBackground) {
                contentParent.gameObject.SetActive(true);
            }
            _spawnedContents[tooltip] = spawned;

            InitializeViewComponents(spawned.transform);
        }

        void RemoveContentView(TooltipValue key, GameObject prefab) {
            Destroy(prefab);
            _spawnedContents.Remove(key);
        }

        void EnsureContentOrder() {
            var orderedElements = _spawnedContents
                .Values
                .Select(go => (go, go.GetComponentInChildren<VCTooltipElement>()))
                .OrderBy(e => e.Item2.TooltipElement.Order)
                .Select(e => e.Item1)
                .ToArray();

            for (int i = orderedElements.Length - 1; i >= 0; i--) {
                var element = orderedElements[i];
                element.transform.SetSiblingIndex(i);
            }
        }

        // === Stick to mouse
        void Update() {
            if (_contentTransform == null) return;
            if (Target.MoveWithMouse) {
                SyncPosition();
                UpdatePivot();
                UpdateHorizontalCardsPivot();
            } else if (!_hadOneUpdate) {
                StartCoroutine(UpdatePosition());
                UpdateHorizontalCardsPivot();
                _hadOneUpdate = true;
            }
        }

        IEnumerator UpdatePosition() {
            for (int i = 0; i < 5; i++) {
                SyncPosition();
                UpdatePivot();
                yield return null;
            }
        }
        
        void UpdatePivot() {
            _contentTransform.pivot = Target.TargetPivot;
        }

        void SyncPosition() {
            if (transform.parent == null) return;
            if (Target.Constructor.AttachToParent) {
                transform.localPosition = Vector3.zero;
                return;
            }
            var position = transform.parent.InverseTransformPoint(Target.TargetPosition);
            position.z = 0f;
            transform.localPosition = position;
            transform.localScale = Vector3.one * Target.Scale;
            
            ConstrainContentPosition();
        }

        void ConstrainContentPosition() {
            _contentTransform.localPosition = _defaultContentLocalPosition;
            if (!Target.Constructor.StaticPositioning?.allowOffset ?? false) { return; }
            var canvasRect = GetWorldRect(_canvasTransform);
            var contentRect = GetWorldRect(_contentTransform);
            _contentTransform.position += new Vector3(GetOffset(0, canvasRect, contentRect), GetOffset(1, canvasRect, contentRect));
        }

        Rect GetWorldRect(RectTransform transform) {
            var rect = transform.rect;
            var min = transform.TransformPoint(rect.min);
            var max = transform.TransformPoint(rect.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        float GetOffset(int axis, Rect canvas, Rect content) {
            var leftOff = canvas.min[axis] - content.min[axis]; // how much it goes off in negative direction
            if (leftOff > 0f) {
                return leftOff;
            } else {
                var rightOff = content.max[axis] - canvas.max[axis]; // how much it goes off in positive direction
                if (rightOff > 0f) {
                    return -rightOff;
                }
            }
            return 0f;
        }

        void UpdateHorizontalCardsPivot() {
            var rect = GetWorldRect(_contentTransform);
            var min = rect.min.x;
            // only do this when in 25% of left side of the screen
            bool isOnLeftSide = min < Screen.width / 4f;
            _horizontalContentTransform.pivot = new Vector2(isOnLeftSide ? -1 : 1, 0f);
        }
        
        // -- Discard
        protected override IBackgroundTask OnDiscard() {
            if (gameObject.activeInHierarchy) {
                Tween tween = DOTween.To(() => canvasGroup.alpha, v => canvasGroup.alpha = v, 0f, 0.1f).SetUpdate(true);
                return new TweenTask(tween);
            }
            return base.OnDiscard();
        }
    }
}