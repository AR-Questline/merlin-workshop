using Awaken.TG.MVC;

namespace Awaken.TG.Main.NewGamePlus {
    public interface IWithNewGamePlusLevel : IModel {
        public int NewGamePlusLevel { get; }
    }
}