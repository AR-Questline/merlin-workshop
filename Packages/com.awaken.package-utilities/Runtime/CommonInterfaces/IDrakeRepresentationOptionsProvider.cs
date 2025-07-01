namespace Awaken.CommonInterfaces {
    public interface IDrakeRepresentationOptionsProvider {
        bool ProvideRepresentationOptions { get; }
        IWithUnityRepresentation.Options GetRepresentationOptions();
    }
}
