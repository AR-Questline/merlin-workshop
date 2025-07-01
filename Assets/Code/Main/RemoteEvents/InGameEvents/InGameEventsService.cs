using System.Collections.Generic;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.RemoteEvents.InGameEvents {
    public class InGameEventsService : MonoBehaviour, IService {
        [SerializeField] List<string> actives;

        public void Init() {
            actives.ForEach(active => transform.Find(active)?.GetComponent<IModification>()?.Apply());
        }
    }

}