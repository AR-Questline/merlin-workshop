using System;
using UnityEngine;

namespace Awaken.TG.Main.UIToolkit.PresenterData {
    [Serializable]
    public struct PContainerElementData : IPresenterData {
        [field: SerializeField] public PBaseData BaseData { get; private set; }
    }
}