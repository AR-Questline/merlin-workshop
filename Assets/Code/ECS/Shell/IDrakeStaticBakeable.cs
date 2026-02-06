using Awaken.CommonInterfaces;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public interface IDrakeStaticBakeable : IWithUnityRepresentation {
        GameObject gameObject { get; }
        bool IsStatic { get; }
        void BakeStatic();
    }
}
