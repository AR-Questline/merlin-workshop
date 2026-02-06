using Awaken.PackageUtilities.CommonInterfaces;

namespace Awaken.Babel {
    public interface IBabelIdBaker {
        LocalizationEntryId ConvertToLocalizationEntry(string id);
    }

    public class FakeBabelIdBaker : IBabelIdBaker {
        public LocalizationEntryId ConvertToLocalizationEntry(string id) {
            return default;
        }
    }
}
