using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Saving {
    public class CachedDomainsVerificationService : IService, UnityUpdateProvider.IWithUpdateGeneric {
        bool _savingFailed;
        bool _saveIsCorrupted;
        string _savingFailedMessage;

        public void Init() {
            UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
        }

        public void UnityUpdate() {
            if (_savingFailed) {
                if (ScenePreloader.IsLoadingCompleted && World.HasAny<LoadingScreenUI>() == false) {
                    FailAndReturnToTitleScreen();
                }
            }
        }

        public void InformThatSavingCachedDomainFailed(Domain domain, string errorMessage, DomainDataSource domainDataSource) {
            var detailedErrorMessage = domainDataSource switch {
                DomainDataSource.FromGameState => $"Failed to correctly save domain {domain} to cache. Error: {errorMessage}",
                DomainDataSource.FromSaveFile => $"Save file for {domain} was corrupted. Error: {errorMessage}",
                _ => $"Not handled {nameof(domainDataSource)} type {domainDataSource}. Something was wrong with domain {domain}. Error: {errorMessage}"
            };

            InformThatSavingOrLoadingCachedDomainFailed(domain, detailedErrorMessage);
        }
        
        public void InformThatLoadingCachedDomainFailed(Domain domain, string errorMessage) {
            InformThatSavingOrLoadingCachedDomainFailed(domain, $"Failed to load {domain} from cache. Error: {errorMessage}");
        }

        public void DiscardSaveIfCorrupted(SaveSlot saveSlot) {
            if (!_saveIsCorrupted) {
                return;
            }
            _saveIsCorrupted = false;
            Log.Marking?.Warning($"Save {LogUtils.GetDebugName(saveSlot)} failed. Discarding save slot.");
            saveSlot.Discard();
        }

        void InformThatSavingOrLoadingCachedDomainFailed(Domain domain, string errorMessage) {
            if (domain.SaveName == "MetaData") {
                Log.Critical?.Error($"Cached Metadata domain {domain} is invalid. Also, it shouldn't be cached, so there is probably an error in logic. Error: {errorMessage}");
                return;
            }

            Log.Critical?.Error(errorMessage);
            if (_saveIsCorrupted) {
                return; // Already failed and waiting for slot cleanup
            }
            _savingFailed = true;
            _saveIsCorrupted = true;
            _savingFailedMessage = errorMessage;
        }

        void FailAndReturnToTitleScreen() {
            _savingFailed = false;
            var mapScene = World.Services.Get<SceneService>().ActiveSceneBehaviour;
            if (mapScene == null) {
                Log.Critical?.Error($"No active map scene. Cannot exit to title screen with error: {_savingFailedMessage}");
                return;
            }
            mapScene.InitializationCanceled = true;
            Log.Minor?.Info($"Returning to TitleScreen because of domain save error: {_savingFailedMessage}");
            TitleScreen.wasLoadingFailed = LoadingFailed.CachedDomain;
            mapScene.FailAndReturnToTitleScreen(_savingFailedMessage).Forget();
            _savingFailedMessage = string.Empty;
        }
    }
}