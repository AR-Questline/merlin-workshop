using Awaken.Utility.Debugging.MemorySnapshots;
using UnityEngine.Localization;

namespace Awaken.Babel {
    interface IBabelProvider : IMemorySnapshotProvider {
        void Init();
        void Dispose();
        void SwitchLanguage(in LocaleIdentifier locale);
        
        string GetTranslation(uint index);
        bool HasSmartFormatTag(uint index);
        void GetTranslationAndSmartFormatTag(uint idIndex, out string translation, out bool smartFormatTag);
        string GetGesture(uint index);
    }
}
