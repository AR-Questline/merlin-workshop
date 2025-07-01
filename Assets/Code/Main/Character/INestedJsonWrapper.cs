using Newtonsoft.Json;

namespace Awaken.TG.Main.Character {
    public interface INestedJsonWrapper<T> {
        void WriteSavables(T parent, JsonWriter jsonWriter, JsonSerializer serializer) { }
    }
}
