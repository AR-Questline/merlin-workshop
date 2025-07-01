using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public interface IWithLookAt : IModel {
        Transform LookAtTarget { get; }
    }
}