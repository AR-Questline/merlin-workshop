using System.Collections.Generic;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Awaken.TG.MVC.UI.Sources {
    /// <summary>
    /// This source does a raycast using Unity's EventSystem and returns
    /// everything that was hit, top to bottom.
    /// </summary>
    public partial class RaycastHandlerSource : Element<GameUI>, IUIHandlerSource {
        public sealed override bool IsNotSaved => true;

        PointerEventData _pointerEventData;
        List<RaycastResult> _raycastResults = new List<RaycastResult>(32);
        List<IUIAware> _handlersInGameObjectBuffer = new List<IUIAware>(8);
        List<IUIAware> _distinctHandlersBuffer = new List<IUIAware>(16);

        // === Configuration

        public UIContext Context => UIContext.Mouse;
        public int Priority => 0;

        // === Initialization

        public RaycastHandlerSource() {
            _pointerEventData = new PointerEventData(EventSystem.current);
        }

        // === Operation

        public void ProvideHandlers(UIPosition mousePosition, List<IUIAware> handlers) {
            // use the event system for raycasting
            // we "forge" a PointerEventData to be able to cast, just the position matters
            _pointerEventData.position = mousePosition.screen;
            EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);
            // did we hit something?
            foreach (RaycastResult result in _raycastResults) {
                if (result.gameObject.layer == RenderLayers.Walkable) {
                    continue;
                }
                FindHandlers(result.gameObject, _handlersInGameObjectBuffer);

                for (int i = 0; i < _handlersInGameObjectBuffer.Count; i++) {
                    if (!_distinctHandlersBuffer.Contains(_handlersInGameObjectBuffer[i])) {
                        _distinctHandlersBuffer.Add(_handlersInGameObjectBuffer[i]);
                    }
                }
                _handlersInGameObjectBuffer.Clear();
            }
            _raycastResults.Clear();

            handlers.AddRange(_distinctHandlersBuffer);
            _distinctHandlersBuffer.Clear();
        }

        static void FindHandlers(GameObject gameObject, List<IUIAware> handlersPool) {
            while (gameObject != null) {
                // any IUIAwares over here?
                gameObject.GetComponents(handlersPool);
                if (handlersPool.Count > 0) {
                    return;
                }
                // nope, go up the stack
                gameObject = gameObject.transform.parent?.gameObject;
            }
        }
    }
}
