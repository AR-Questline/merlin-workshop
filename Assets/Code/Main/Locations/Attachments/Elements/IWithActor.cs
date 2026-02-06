using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public interface ILocationElementWithActor : IElement<Location>, IWithActor, ILocationNameModifier { }
    public interface IWithActor : IModel {
        Actor Actor { get; }
        Transform ActorTransform { get; }
    }
}