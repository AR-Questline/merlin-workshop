using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Unity.Mathematics;

namespace Awaken.TG.Main.Saving.Utils {
    public static class IOUtil {
        public static void Save(string path, string fileName, Stream stream) {
            path = AtomicDirectoryWriter.AdjustPath(path);
            string filePath = Path.Combine(path, fileName);
            string outputPath = filePath + ".data";
            string dirtyOutputPath = filePath + "~.data";
            string cleanOutputPath = filePath + "~~.data";

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            File.Delete(dirtyOutputPath);
            File.Delete(cleanOutputPath);

            // writing to file is not atomic operation so when we wrote to destination file and power outage occured, save was corrupted
            // to prevent it we write to temporary file and than use File.Move that is atomic operation
            using (var dirtyStream = new FileStream(dirtyOutputPath, FileMode.Create)) {
                stream.CopyTo(dirtyStream);
                dirtyStream.Flush(true);
            }

            if (!File.Exists(outputPath)) {
                File.Move(dirtyOutputPath, outputPath);
            } else {
                // Move dirty to clean, clean path is always valid (dirty might be corrupted)
                File.Move(dirtyOutputPath, cleanOutputPath);
                File.Delete(outputPath);
                File.Move(cleanOutputPath, outputPath);
            }
        }

        public static void Save(string path, string fileName, byte[] data) {
            const int DefaultBufferSize = 4096;

            path = AtomicDirectoryWriter.AdjustPath(path);
            string filePath = Path.Combine(path, fileName);
            string outputPath = filePath + ".data";
            string dirtyOutputPath = filePath + "~.data";
            string cleanOutputPath = filePath + "~~.data";

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            File.Delete(dirtyOutputPath);
            File.Delete(cleanOutputPath);

            // writing to file is not atomic operation so when we wrote to destination file and power outage occured, save was corrupted
            // to prevent it we write to temporary file and than use File.Move that is atomic operation
            using (var dirtyStream = new FileStream(dirtyOutputPath, FileMode.Create, FileAccess.Write, FileShare.Read, DefaultBufferSize, FileOptions.None)) {
                var bytesToWrite = data.Length;
                var offset = 0;
                while (bytesToWrite > 0) {
                    var currentWriteSize = math.min(DefaultBufferSize, bytesToWrite);
                    dirtyStream.Write(data, offset, currentWriteSize);
                    offset += currentWriteSize;
                    bytesToWrite -= currentWriteSize;
                }
                dirtyStream.Flush(true);
            }
            
            if (!File.Exists(outputPath)) {
                File.Move(dirtyOutputPath, outputPath);
            } else {
                // Move dirty to clean, clean path is always valid (dirty might be corrupted) 
                File.Move(dirtyOutputPath, cleanOutputPath);
                File.Delete(outputPath);
                File.Move(cleanOutputPath, outputPath);
            }
        }

        public static bool Delete(string path, string fileName, bool allowDeleteDirectory = false) {
            bool success = false;
            path = AtomicDirectoryWriter.AdjustPath(path);
            string outputPath = Path.Combine(path, fileName) + ".data";
            if (File.Exists(outputPath)) {
                File.Delete(outputPath);
                success = true;
            }

            if (allowDeleteDirectory && Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any()) {
                Directory.Delete(path);
            }

            return success;
        }

        public static void DeleteSaveSlot(string path) {
            path = AtomicDirectoryWriter.AdjustPath(path);
            if (Directory.Exists(path)) {
                Directory.Delete(path, true);
            }
        }

        public static bool HasSave(string path, string fileName) {
            string filePath = Path.Combine(path, fileName);
            string outputPath = filePath + ".data";
            string cleanOutputPath = filePath + "~~.data";
            return File.Exists(outputPath) || File.Exists(cleanOutputPath);
        }
        
        public static DateTime GetTimeStamp(string path, string fileName) {
            path = AtomicDirectoryWriter.AdjustPath(path);
            string outputPath = Path.Combine(path, fileName) + ".data";
            return File.GetLastWriteTimeUtc(outputPath);
        }

        public static byte[] Load(string path, string fileName) {
            string filePath = Path.Combine(path, fileName);
            string outputPath = filePath + ".data";
            string cleanOutputPath = filePath + "~~.data";
            if (File.Exists(cleanOutputPath)) {
                File.Delete(outputPath);
                File.Move(cleanOutputPath, outputPath);
            }

            return File.Exists(outputPath) ? File.ReadAllBytes(outputPath) : null;
        }

        // === Directories
        public static void DirectoryCopy(string sourcePath, string destinationPath, bool overwriteFiles = false) {
            Directory.CreateDirectory(destinationPath);
            foreach (var filePath in Directory.GetFiles(sourcePath)) {
                File.Copy(filePath, Path.Combine(destinationPath, Path.GetFileName(filePath)), overwriteFiles);
            }

            foreach (var sourceDirectoryChildPath in Directory.GetDirectories(sourcePath)) {
                var destDirectoryChildPath = Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(sourceDirectoryChildPath));
                DirectoryCopy(sourceDirectoryChildPath, destDirectoryChildPath);
            }
        }

        // === ZIP
        
        /// <summary>
        /// Create a ZIP file of the files provided.
        /// </summary>
        /// <param name="zipPath">The full path and name to store the ZIP file at.</param>
        /// <param name="files">The list of files to be added.</param>
        public static void CreateZipFile(string zipPath, IEnumerable<string> files) {
            if (File.Exists(zipPath)) {
                File.Delete(zipPath);
            }
            // Create and open a new ZIP file
            var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            string dirName = Path.GetDirectoryName(zipPath) ?? "";

            foreach (var file in files) {
                string fileRelativePath = Path.GetRelativePath(dirName, file);
                // Add the entry for each file
                zip.CreateEntryFromFile(file, fileRelativePath, CompressionLevel.Optimal);
            }

            // Dispose of the object when we are done
            zip.Dispose();
        }

        public static bool HasSpace(string directory, long freeSpace) {
            return new DriveInfo(new DirectoryInfo(directory).Root.FullName).AvailableFreeSpace > freeSpace;
        }
    }
}