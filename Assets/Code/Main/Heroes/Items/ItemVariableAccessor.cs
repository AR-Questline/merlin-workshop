namespace Awaken.TG.Main.Heroes.Items {
    /// <summary>
    /// Smaller utility class for accessing item-related variables
    /// </summary>
    public static class ItemVariableAccessor {
        const string HealValueVariableName = "AddValue";
        const string HealDurationVariableName = "Duration";

        public static float GetHealValue(this Item item) => (item.GetVariable(HealValueVariableName) ?? 0.0f) * (item.GetVariable(HealDurationVariableName) ?? 0.0f);
    }
}
