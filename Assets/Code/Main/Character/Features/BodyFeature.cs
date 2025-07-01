using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Character.Features {
    public abstract partial class BodyFeature {
        public abstract ushort TypeForSerialization { get; }
        
        public BodyFeatures Features { get; private set; }

        public void Init(BodyFeatures features) {
            Features = features;
        }

        public abstract UniTask Spawn();
        
        /// <param name="prettySwap">for character creator swapping having no blank frames, release will be called after new data is already loaded</param>
        public abstract UniTask Release(bool prettySwap = false);

        public abstract BodyFeature GenericCopy();
    }
}
