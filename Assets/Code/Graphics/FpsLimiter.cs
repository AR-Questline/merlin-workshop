using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Graphics {
    public class FpsLimiter : IService {
        const int LargeNumber = 999;
        public const int DefaultUIFpsLimit = 60;
        
        readonly Dictionary<IModel, int> _limitByModel = new();
        bool _isBlocked;
        
        // Instead of explicit calls maybe nice would be to also listen for UIStatePushed and do limit also based on that
        public void RegisterLimit(IModel source, int limit) {
            World.EventSystem.RemoveAllListenersBetween(source, this);
            source.ListenTo(Model.Events.BeforeDiscarded, ReleaseLimit, this);
            // To simplify calculations replace -1 with large number
            _limitByModel[source] = limit == -1 ? LargeNumber : limit;
            RefreshTargetFrameRate();
        }
        
        public void ReleaseLimit(IModel source) {
            if (!_limitByModel.Remove(source)) {
                return;
            }
            World.EventSystem.RemoveAllListenersBetween(source, this);
            RefreshTargetFrameRate();
        }

        [UnityEngine.Scripting.Preserve]
        public void TemporarilyRemoveAllLimitsAndBlockNewLimits() {
            _isBlocked = true;
            RefreshTargetFrameRate();
        }

        [UnityEngine.Scripting.Preserve]
        public void ReturnLimitsAndUnblock() {
            _isBlocked = false;
            RefreshTargetFrameRate();
        }
        
        void RefreshTargetFrameRate() {
            if (_isBlocked) {
                Application.targetFrameRate = -1;
                return;
            }
            if (_limitByModel.Count < 1) {
                World.Any<ScreenResolution>()?.Refresh();
                return;
            }
            var limit = _limitByModel.Min(p => p.Value);
            if (limit >= LargeNumber) {
                limit = -1;
            }
            Application.targetFrameRate = limit;
        }
    }
}
