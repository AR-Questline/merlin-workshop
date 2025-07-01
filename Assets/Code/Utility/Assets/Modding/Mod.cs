using System.IO;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Awaken.TG.Assets.Modding {
    public struct Mod {
        public string Name { get; }
        public IResourceLocator Locator { get; }

        public Mod(string name, FileInfo file) {
            Name = name;
            Locator = LoadCatalog(file);
        }

        static IResourceLocator LoadCatalog(FileInfo file) {
            return Addressables.LoadContentCatalogAsync(file.FullName).WaitForCompletion();
        }
    }
}