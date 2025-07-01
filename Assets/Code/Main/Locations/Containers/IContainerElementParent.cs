using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Locations.Containers {
    public interface IContainerElementParent : IModel {
        bool ContainerElementsSpawned { get; }
        [UnityEngine.Scripting.Preserve] public void OnItemClicked(ContainerElement containerElement);
        [UnityEngine.Scripting.Preserve] Prompt PromptForElementInteractions() => null;
        
        // == Events
        public static class Events {
            public static readonly Event<IContainerElementParent, IContainerElementParent> ContainerElementsSpawned =
                new(nameof(ContainerElementsSpawned));
        }
    }
}