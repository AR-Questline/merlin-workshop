using System;
using UnityEngine;

namespace Awaken.TG.Main.UIToolkit.PresenterData {
    [Serializable]
    public struct PArenaSpawnerData : IPresenterData {
        [field: SerializeField] public PBaseData BaseData { get; private set; }
        [field: SerializeField] public PBaseData ArenaContainerElementData { get; private set; }
    }
}