using Awaken.TG.Assets;

namespace Awaken.TG.Main.Utility.VSDatums.TypeInstances {
    public class VSDatumTypeInstanceAsset : VSDatumTypeInstance<ARAssetReference> {
        public static readonly VSDatumTypeInstanceAsset Instance = new();
        public override ARAssetReference GetDatumValue(in VSDatumValue value) => value.Asset;

        public override VSDatumValue ToDatumValue(ARAssetReference value) => new() { Asset = value };
    }
}