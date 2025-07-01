using System;
using System.Collections.Generic;
using System.IO;
using Awaken.TG.Main.Saving.Utils;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Saving.Cloud.Services {
    public class DebugCloudService : CloudService {
        protected override string UserID {
            get {
#if UNITY_EDITOR
                return "Editor";
#else
                return "Data";
#endif
            }
        }

        protected override bool WorksOnFileSystem => true;

        public override IEnumerable<ICloudSyncResult> InitCloud() {
            AtomicDirectoryWriter.EnsureNoTempDirectories(DataPath);
            return base.InitCloud();
        }
        
        public override void SaveGlobalFile(string relativePath, string fileName, byte[] data, bool synchronized = true) {
            IOUtil.Save(Path.Combine(DataPath, relativePath), fileName, data);
        }

        public override bool TryLoadSingleFile(string relativePath, string fileName, out byte[] data, bool synchronized = true) {
            data = IOUtil.Load(Path.Combine(DataPath, relativePath), fileName);
            return data != null;
        }

        public override void DeleteSaveSlot(string relativePath, bool inBatch = false) {
            IOUtil.DeleteSaveSlot(Path.Combine(DataPath, relativePath));
        }

        public override void DeleteGlobalFile(string relativePath, string fileName, bool synchronized = true) {
            IOUtil.Delete(Path.Combine(DataPath, relativePath), fileName);
        }

        public override void BeginSaveDirectory(string directory, long size) {
            AtomicDirectoryWriter.Begin(Path.Combine(DataPath, directory));
        }

        public override UniTask<bool> EndSaveDirectory(string directory, bool failed) {
            return AtomicDirectoryWriter.End(Path.Combine(DataPath, directory));
        }
    }
}
