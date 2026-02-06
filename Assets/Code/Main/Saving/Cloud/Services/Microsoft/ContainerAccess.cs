#if UNITY_GAMECORE || MICROSOFT_GAME_CORE

using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Unity.XGamingRuntime;
using HR = Microsoft.Xbox.HR;

namespace Awaken.TG.Main.Saving.Cloud.Services.Microsoft {
    public partial class XboxCloudService {
        static string PathToContainerName(string path) {
            return path.Replace("/", "").Replace("\\", "");
        }
        
        class ContainerAccess {
            readonly XGameSaveProviderHandle _providerHandle;
            List<Container> _containers;

            public ContainerAccess(XGameSaveProviderHandle providerHandle) {
                _providerHandle = providerHandle;
                _containers = new List<Container>();
            }

            public LoadingContainer CreateLoadingContainerHandleByPath(string relativePath) {
                var containerName = PathToContainerName(relativePath);
                CreateLoadingContainerHandle(containerName, out var container);
                return container;
            }

            public SavingContainer CreateSavingContainerHandleByPath(string relativePath) {
                var containerName = PathToContainerName(relativePath);
                CreateSavingContainerHandle(containerName, out var container);
                return container;
            }

            void CreateSavingContainerHandle(string containerName, out SavingContainer container) {
                lock (_containers) {
                    if (_containers.TryGetFirst(c => c.name == containerName, out _)) {
                        throw new ContainerInUseException(containerName);
                    }

                    var hr = SDK.XGameSaveCreateContainer(_providerHandle, containerName, out var containerHandle);
                    HR.ThrowIfHResultFailed(hr);

                    hr = SDK.XGameSaveCreateUpdate(containerHandle, containerName, out XGameSaveUpdateHandle updateHandle);
                    HR.ThrowIfHResultFailed(hr);

                    container = new SavingContainer {
                        handle = containerHandle,
                        name = containerName,
                        updateHandle = updateHandle,
                    };

                    _containers.Add(container);
                }
            }

            void CreateLoadingContainerHandle(string containerName, out LoadingContainer container) {
                lock (_containers) {
                    if (_containers.TryGetFirst(c => c.name == containerName, out _)) {
                        throw new ContainerInUseException(containerName);
                    }

                    var hr = SDK.XGameSaveCreateContainer(_providerHandle, containerName, out var containerHandle);
                    HR.ThrowIfHResultFailed(hr);

                    container = new LoadingContainer {
                        handle = containerHandle,
                        name = containerName,
                    };

                    _containers.Add(container);
                }
            }

            public void ReleaseContainer(Container loadingContainer) {
                lock (_containers) {
                    int index = _containers.IndexOf(loadingContainer);
                    if (index >= 0) {
                        loadingContainer.handle.Close();
                        _containers.RemoveAt(index);
                    }
                }
            }
        }

        abstract class Container : IDisposable {
            public XGameSaveContainerHandle handle;
            public string name;

            public void Dispose() {
                ((XboxCloudService)Get)._containerAccess?.ReleaseContainer(this);
                OnDispose();
            }

            protected virtual void OnDispose() { }
        }

        class LoadingContainer : Container { }

        class SavingContainer : Container {
            public XGameSaveUpdateHandle updateHandle;
            readonly HashSet<string> _savedBlobs = new();

            public int BlobCount {
                get {
                    lock (_savedBlobs) {
                        return _savedBlobs.Count;
                    }
                }
            }

            public void RegisterBlobSaved(string blobKey) {
                if (blobKey.Contains(LoadSystem.UncompressedFileSuffix)) {
                    return;
                }
                
                lock (_savedBlobs) {
                    _savedBlobs.Add(blobKey);
                }
            }

            public bool CheckBlobSaved(string blobKey) {
                lock (_savedBlobs) {
                    return _savedBlobs.Contains(blobKey);
                }
            }

            public string[] GetBlobsNames() {
                lock (_savedBlobs) {
                    return _savedBlobs.ToArray();
                }
            }

            protected override void OnDispose() {
                updateHandle.Dispose();
            }
        }

        class ContainerInUseException : Exception {
            public ContainerInUseException(string containerName) : base($"Save container {containerName} already in use") { }
        }
    }
}
#endif