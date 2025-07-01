using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.Playables;

namespace Awaken.TG.Graphics.ScriptedEvents.Timeline {
    public class ScriptedEventReceiver : MonoBehaviour, IScriptedEventHolder, INotificationReceiver {
        public ScriptedEvent ScriptedEvent { get; set; }
        int _internalProlongedRefCount = 0;
        
        void OnIncreaseProlongedAssetRefCount() {
            _internalProlongedRefCount++;
        }
        
        void OnDecreaseProlongedAssetRefCount() {
            _internalProlongedRefCount--;
        }
        
        void OnDestroy() {
            if (_internalProlongedRefCount == 0) {
                return;
            }

            if (_internalProlongedRefCount > 0) {
                Log.Minor?.Error("[ScriptedEvent] IncreaseProlongedAssetRefCount and DecreaseProlongedAssetRefCount are not balanced.");
                while (_internalProlongedRefCount > 0) {
                    OnDecreaseProlongedAssetRefCount();
                }
            } else {
                Log.Minor?.Error("[ScriptedEvent] Internal reference count is negative. DecreaseProlongedAssetRefCount must have been called too many times.");
            }
        }

        public void OnNotify(Playable origin, INotification notification, object context) {
            if (notification is ScriptedEventMarker marker) {
                if (marker.Type == ScriptedEventEventType.IncreaseProlongedAssetRefCount) {
                    OnIncreaseProlongedAssetRefCount();
                } else if (marker.Type == ScriptedEventEventType.DecreaseProlongedAssetRefCount) {
                    OnDecreaseProlongedAssetRefCount();
                }
                ScriptedEvent.ReceiveEvent(marker.Type);
            }
        }
    }
}