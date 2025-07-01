using System;


namespace Awaken.TG.Main.Saving.Cloud {
    public interface ICloudSyncResult {
        ResultType Type { get; }
    }

    public enum ResultType : byte {
        Success = 0,
        Failure = 1,
        Conflict = 2,
    }
    
    public abstract class BaseCloudSyncResult : ICloudSyncResult {
        readonly LocalFileInfo _localFile;
        readonly RemoteStorageFile _cloudFile;
        
        public ResultType Type { get; }
        [UnityEngine.Scripting.Preserve] public virtual Exception Exception => null;
        public DateTime LocalTimeStamp => _localFile.WindowsTimeStamp;
        public DateTime CloudTimeStamp => _cloudFile.timestamp;

        public BaseCloudSyncResult(ResultType result, LocalFileInfo localFile, RemoteStorageFile cloudFile) {
            Type = result;
            this._localFile = localFile;
            this._cloudFile = cloudFile;
        }
        
        public override string ToString() {
            return $"{GetType().Name} {_localFile.steamPath} -- {_localFile.WindowsTimeStamp} -- {_cloudFile.timestamp}";
        }
    }

    public class ConflictBetweenLocalAndCloud : BaseCloudSyncResult {
        Func<ICloudSyncResult> _chooseLocal;
        Func<ICloudSyncResult> _chooseCloud;

        public ConflictBetweenLocalAndCloud(LocalFileInfo localFile, RemoteStorageFile cloudFile, Func<ICloudSyncResult> chooseLocal, Func<ICloudSyncResult> chooseCloud) 
            : base(ResultType.Conflict, localFile, cloudFile) {
            this._chooseLocal = chooseLocal;
            this._chooseCloud = chooseCloud;
        }
        
        public ICloudSyncResult ChooseLocal() {
            return _chooseLocal?.Invoke();
        }
        
        public ICloudSyncResult ChooseCloud() {
            return _chooseCloud?.Invoke();
        }
    }

    public class CloudSyncUploadResult : BaseCloudSyncResult {
        public CloudSyncUploadResult(ResultType type, LocalFileInfo localFile, RemoteStorageFile cloudFile) 
            : base(type, localFile, cloudFile) {
            
        }
    }

    public class CloudSyncDownloadResult : BaseCloudSyncResult {
        readonly Exception _exception;

        public override Exception Exception => _exception;

        public CloudSyncDownloadResult(ResultType type, LocalFileInfo localFile, RemoteStorageFile cloudFile, Exception exception = null) 
            : base(type, localFile, cloudFile) {
            this._exception = exception;
        }
        
        public override string ToString() {
            if (_exception != null) {
                return $"{base.ToString()}\n{_exception}";
            } else {
                return base.ToString();
            }
        }
    }

    public class CloudSyncCloudDeleteResult : BaseCloudSyncResult {
        public CloudSyncCloudDeleteResult(ResultType type, LocalFileInfo originFile, RemoteStorageFile cloudFile) 
            : base(type, originFile, cloudFile) {
            
        }
    }

    public class CloudSyncLocalDeleteResult : BaseCloudSyncResult {
        readonly Exception _exception;
        
        public override Exception Exception => _exception;
        
        public CloudSyncLocalDeleteResult(ResultType type, LocalFileInfo originFile, RemoteStorageFile cloudFile, Exception exception = null) 
            : base(type, originFile, cloudFile) {
            this._exception = exception;
        }
        
        public override string ToString() {
            if (_exception != null) {
                return $"{base.ToString()}\n{_exception}";
            } else {
                return base.ToString();
            }
        }
    }
}