using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Debugging.UI.UIEventTypes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.UI {
    public class UIEventsCollector {
        MultiMap<UIEventType, int> _framesBeEvent = new();
        FrameData[] _framesData;
        int _currentIndex;
        int _currentFrame;

        public UIEventsCollector(int frameLimit) {
            _framesData = new FrameData[frameLimit];
            _currentIndex = 0;
            _currentFrame = -1;
        }
        
        // == Getters

        public FrameData Frame(int frame) {
            return _framesData[InternalIndex(frame)];
        }

        public IEnumerable<(UIEventType, List<int>)> FramesByEvent() {
            return _framesBeEvent.Select(pair => {
                var list = pair.Value.Select(ExternalIndex).ToList();
                list.Sort();
                return (pair.Key, list);
            });
        }
        
        int InternalIndex(int externalIndex) {
            return (_currentIndex + 1 + externalIndex) % _framesData.Length;
        }
        int ExternalIndex(int internalIndex) {
            return (internalIndex - _currentIndex - 1 + _framesData.Length) % _framesData.Length;
        }
        
        // == Modifying

        public void Clear() {
            _framesBeEvent.Clear();
            for (int i = 0; i < _framesData.Length; i++) {
                _framesData[i] = null;
            }
            _currentIndex = 0;
        }
        
        void NewFrame() {
            _currentFrame = Time.frameCount;
            _currentIndex = (_currentIndex + 1) % _framesData.Length;
            _framesData[_currentIndex]?.Events.ForEach(e => _framesBeEvent.Remove(e, _currentIndex));
            _framesData[_currentIndex] = new FrameData();
        }
        
        public void AddEvent(UIEvent evt, IEnumerable<ISmartHandler> handlers, IEnumerable<IUIAware> uiAwares) {
            if (Time.frameCount != _currentFrame) {
                NewFrame();
            }
            var eventType = UIEventType.CreateFor(evt);
            _framesData[_currentIndex].AddEvent(eventType, handlers, uiAwares);
            _framesBeEvent.Add(eventType, _currentIndex);
        }

        public void AddResultOfHandlerBeforeDelivery(UIEvent evt, ISmartHandler handler, UIResult result) {
            _framesData[_currentIndex].AddResultOfHandlerBeforeDelivery(UIEventType.CreateFor(evt), handler, result);
        }

        public void AddResultOfHandling(UIEvent evt, IUIAware aware, UIResult result) {
            _framesData[_currentIndex].AddResultOfHandling(UIEventType.CreateFor(evt), aware, result);
        }
        public void AddResultOfHandlerBeforeHandling(UIEvent evt, IUIAware aware, ISmartHandler handler, UIResult result) {
            _framesData[_currentIndex].AddResultOfHandlerBeforeHandling(UIEventType.CreateFor(evt), aware, handler, result);
        }
        public void AddResultOfHandlerAfterHandling(UIEvent evt, IUIAware aware, ISmartHandler handler, UIResult result) {
            _framesData[_currentIndex].AddResultOfHandlerAfterHandling(UIEventType.CreateFor(evt), aware, handler, result);
        }
    }
}