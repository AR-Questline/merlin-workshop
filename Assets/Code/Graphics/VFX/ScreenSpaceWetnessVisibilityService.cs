using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX {
    public class ScreenSpaceWetnessVisibilityService : IService {
        public bool IsRequestedToDisable => _disableRequestSources.Count != 0;
        
        StructList<Object> _disableRequestSources = new(1);
        
        public void DisableWetness(Object disableRequestSource) {
            _disableRequestSources.AddUnique(disableRequestSource);
        }

        public void EnableWetness(Object source) {
            _disableRequestSources.Remove(source);
        }
    }
}