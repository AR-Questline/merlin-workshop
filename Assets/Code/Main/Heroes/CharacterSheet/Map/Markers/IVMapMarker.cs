using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    public interface IVMapMarker : IView<MapMarker> {
        public void Init(MapSceneUI mapSceneUI);
    }
}