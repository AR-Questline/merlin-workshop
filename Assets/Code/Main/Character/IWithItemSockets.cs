using Awaken.TG.MVC;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Character {
    public interface IWithItemSockets : IModel {
        Transform HeadSocket { get; }
        Transform MainHandSocket { get; }
        [CanBeNull] Transform MainHandWristSocket => null;
        [CanBeNull] Transform AdditionalMainHandSocket => null;
        Transform OffHandSocket { get; }
        [CanBeNull] Transform OffHandWristSocket => null;
        [CanBeNull] Transform AdditionalOffHandSocket => null;
        Transform HipsSocket { get; }
        Transform RootSocket { get; }
    }
}