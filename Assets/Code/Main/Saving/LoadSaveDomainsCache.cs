using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility;
using Awaken.Utility.Files;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UniversalProfiling;

namespace Awaken.TG.Main.Saving {
    public class LoadSaveDomainsCache {
        static string CachedDomainsDirName {
            get {
#if UNITY_EDITOR
                return "CachedDomains_Editor";
#else
                return "CachedDomains";
#endif
            }
        }
        static readonly UniversalProfilerMarker SaveDomainFileSizeAndStartVerificationMarker = new($"LoadSaveDomainsCache.SaveDomainFileSizeAndStartVerification");

        Dictionary<Domain, uint> _cachedDomainsFilesSizes = new();

        public IEnumerable<Domain> CachedDomains => _cachedDomainsFilesSizes.Keys;

        public LoadSaveDomainsCache() {
            DeleteAndCreateCachedDomainsDirectory();
        }

        public void RemoveFromCache(Domain domain) {
            DeleteCachedDomainFile(domain);
            _cachedDomainsFilesSizes.Remove(domain);
        }

        public FileStream GetCachedDomainFileWriteStream(Domain domain) {
            EnsureCachedDomainsDirectoryExists();
            var cachedDomainFilePath = LoadSaveDomainsCache.GetCachedDomainFilePath(domain);
            return new FileStream(cachedDomainFilePath, FileMode.Create, FileAccess.Write, FileShare.None, LoadSave.BufferSize, false);
        }

        public void SaveDomainFileSizeAndStartVerification(Domain domain, long serializedDomainDataLength, DomainDataSource domainDataSource) {
            using var marker = SaveDomainFileSizeAndStartVerificationMarker.Auto();
            try {
                if (serializedDomainDataLength == 0) {
                    _cachedDomainsFilesSizes.Remove(domain);
                    return;
                }
                _cachedDomainsFilesSizes[domain] = (uint)serializedDomainDataLength;
            } catch (Exception e) {
                Debug.LogException(e);
                World.Services.Get<CachedDomainsVerificationService>().InformThatSavingCachedDomainFailed(domain, e.Message, domainDataSource);
            }
        }

        public bool TryGetCachedUncompressedSaveData(Domain domain, out Stream stream) {
            var cachedDomainFilePath = GetCachedDomainFilePath(domain);
            if (TryGetCachedCompressedDomainStream(domain, cachedDomainFilePath, out _).TryGetValue(out var fileStream)) {
                stream = new DeflateStream(fileStream, CompressionMode.Decompress);
                return true;
            }
            stream = null;
            return false;
            
        }
        
        public bool TryReadCachedDomainAsString(Domain domain, out string cachedDomain) {
            var cachedDomainFilePath = GetCachedDomainFilePath(domain);
            if (TryGetCachedCompressedDomainStream(domain, cachedDomainFilePath, out _).TryGetValue(out var compressedStream)) {
                using var fileStream = compressedStream;
                using var uncompressedStream = new DeflateStream(fileStream, CompressionMode.Decompress);
                using var textReader = new StreamReader(uncompressedStream, LoadSave.Encoding);
                cachedDomain = textReader.ReadToEnd();
                return true;
            }
            cachedDomain = null;
            return false;
        }

        public Optional<FileStream> TryGetCachedCompressedDomainStream(Domain domain, string cachedDomainFilePath, out uint dataLengthInBytes) {
            if (_cachedDomainsFilesSizes.TryGetValue(domain, out dataLengthInBytes) == false) {
                return Optional<FileStream>.None;
            }
            if (dataLengthInBytes == 0) {
                return Optional<FileStream>.None;
            }

            if (File.Exists(cachedDomainFilePath) == false) {
                var errorMessage = $"File {cachedDomainFilePath} does not exist. Maybe it was deleted by user";
                World.Services.Get<CachedDomainsVerificationService>().InformThatLoadingCachedDomainFailed(domain, errorMessage);
                return Optional<FileStream>.None;
            }

            return new FileStream(cachedDomainFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, LoadSave.BufferSize, false);
        }

        public Optional<byte[]> TryReadCachedCompressedDomainSynchronous(Domain domain) {
            if (_cachedDomainsFilesSizes.TryGetValue(domain, out var dataLengthInBytes) == false || dataLengthInBytes == 0) {
                return Optional<byte[]>.None;
            }

            var compressedData = new byte[dataLengthInBytes];
            var cachedDomainFilePath = GetCachedDomainFilePath(domain);
            if (File.Exists(cachedDomainFilePath) == false) {
                var errorMessage = $"File {cachedDomainFilePath} does not exist. Maybe it was deleted by user";
                World.Services.Get<CachedDomainsVerificationService>().InformThatLoadingCachedDomainFailed(domain, errorMessage);
                return Optional<byte[]>.None;
            }
            var status = FileRead.ToExistingBufferWithClose(cachedDomainFilePath, 0, compressedData);
            if (status != ReadStatus.Complete) {
                var errorMessage = $"Failed to read cached domain file at path {cachedDomainFilePath}. Status: {status}";
                World.Services.Get<CachedDomainsVerificationService>().InformThatLoadingCachedDomainFailed(domain, errorMessage);
                return Optional<byte[]>.None;
            }
            return compressedData;
        }

        public Optional<byte[]> TryReadCachedCompressedDomainAsync(Domain domain, string cachedDomainFilePath) {
            byte[] compressedData;
            try {
                var readBytes = 0;
                var domainFileStreamOptional = TryGetCachedCompressedDomainStream(domain, cachedDomainFilePath, out var dataLengthInBytes);
                if (!domainFileStreamOptional.HasValue) {
                    // If not successful, we already logged the error if appropriate
                    return Optional<byte[]>.None;
                }
                compressedData = new byte[dataLengthInBytes];
                using (var domainFileStream = domainFileStreamOptional.Value) {
                    readBytes = domainFileStream.Read(compressedData, 0, compressedData.Length);
                }
                if (readBytes != dataLengthInBytes) {
                    var errorMessage = $"Failed to read cached domain file at path {cachedDomainFilePath}, expected {dataLengthInBytes} bytes but read {readBytes} bytes";
                    World.Services.Get<CachedDomainsVerificationService>().InformThatLoadingCachedDomainFailed(domain, errorMessage);
                    return Optional<byte[]>.None;
                }
            } catch (Exception e) {
                var errorMessage = $"Failed to read cached domain file at path {cachedDomainFilePath} with exception: {e}";
                World.Services.Get<CachedDomainsVerificationService>().InformThatLoadingCachedDomainFailed(domain, errorMessage);
                return Optional<byte[]>.None;
            }

            return compressedData;
        }

        public static string GetCachedDomainFilePath(Domain domain) {
            return Path.Combine(GetCachedDomainsDirectoryPath(), domain.FullName + ".data");
        }

        static void DeleteCachedDomainFile(Domain domain) {
            var path = GetCachedDomainFilePath(domain);
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }

        static void DeleteAndCreateCachedDomainsDirectory() {
            var cachedDomainsDir = GetCachedDomainsDirectoryPath();
            if (Directory.Exists(cachedDomainsDir)) {
                Directory.Delete(cachedDomainsDir, true);
            }

            Directory.CreateDirectory(cachedDomainsDir);
        }

        static void EnsureCachedDomainsDirectoryExists() {
            var cachedDomainsDir = GetCachedDomainsDirectoryPath();
            if (Directory.Exists(cachedDomainsDir) == false) {
                Directory.CreateDirectory(cachedDomainsDir);
            }
        }

        static string GetCachedDomainsDirectoryPath() => Path.Combine(Application.persistentDataPath, CachedDomainsDirName);

        public long CalculateDataSize() {
            long dataSize = 0;
            foreach (var domain in CachedDomains) {
                if (domain.IsChildOf(Domain.SaveSlot) == false) {
                    continue;
                }

                if (_cachedDomainsFilesSizes.TryGetValue(domain, out var size)) {
                    dataSize += size;
                }
            }

            dataSize += 50_000; // Reserve some space for metadata
            return dataSize;
        }
    }
}