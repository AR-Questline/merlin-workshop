namespace Awaken.TG.Main.Settings.Graphics {
    public enum UpScalingType {
        [UnityEngine.Scripting.Preserve] Invalid = -1, //Used to assign proper value for debugging when casted from int (ex. (UpScalingType)_upscaleType.OptionInt)
        None = 0,
        STP = 1,
        DLSS = 2,
    }
}