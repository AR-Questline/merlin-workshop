namespace Awaken.TG.Main.Saving {
    /// <summary>
    /// <param name="AllowMoreFiles">Allow saved domain file count to be grater than domain count saved in metadata</param>
    /// <param name="Strict">Saved domain file count must be equal to domain count saved in metadata</param>
    /// </summary>
    public enum DomainAmountValidationType : byte {
        AllowMoreFiles,
        Strict
    }
}