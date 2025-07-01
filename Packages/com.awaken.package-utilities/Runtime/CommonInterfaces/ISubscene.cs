using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Awaken.CommonInterfaces {
    public interface ISubscene {
        static readonly Dictionary<int, ISubscene> SceneHandleToSubsceneMap = new();

        Scene OwnerScene { get; set; }
    }
}
