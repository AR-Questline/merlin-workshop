using System;
using System.Collections.Generic;


namespace Awaken.TG.Main.Saving.Cloud {
    public interface ICloudSyncResult {
        ResultType Type { get; }
    }

    public interface ICloudSyncConflict : ICloudSyncResult {
        DateTime LocalTimeStamp { get; }
        DateTime CloudTimeStamp { get; }
        IEnumerable<ICloudSyncResult> ChooseCloud();
        IEnumerable<ICloudSyncResult> ChooseLocal();
    }

    public enum ResultType : byte {
        Success = 0,
        Failure = 1,
        Conflict = 2,
    }
    
    public abstract class BaseCloudSyncResult : ICloudSyncResult {
        protected readonly string steamFilePath;
        protected readonly DateTime localTimestamp;
        protected readonly DateTime cloudTimestamp; 
        
        public ResultType Type { get; }
        [UnityEngine.Scripting.Preserve] public virtual Exception Exception => null;
        public DateTime LocalTimeStamp => localTimestamp;
        public DateTime CloudTimeStamp => cloudTimestamp;

        protected BaseCloudSyncResult(ResultType result, string steamFilePath, DateTime localTimestamp, DateTime cloudTimestamp) {
            Type = result;
            this.steamFilePath = steamFilePath;
            this.localTimestamp = localTimestamp;
            this.cloudTimestamp = cloudTimestamp;
        }
        
        public override string ToString() {
            return $"{GetType().Name} ({Type}) {steamFilePath} -- {localTimestamp} -- {CloudTimeStamp}";
        }
    }

    public class ConflictBetweenLocalAndCloud : BaseCloudSyncResult, ICloudSyncConflict {
        Func<ICloudSyncResult> _chooseLocal;
        Func<ICloudSyncResult> _chooseCloud;

        public ConflictBetweenLocalAndCloud(string steamFilePath, DateTime localTimestamp, DateTime cloudFileTimestamp, Func<ICloudSyncResult> chooseLocal, Func<ICloudSyncResult> chooseCloud) 
            : base(ResultType.Conflict, steamFilePath, localTimestamp, cloudFileTimestamp) {
            this._chooseLocal = chooseLocal;
            this._chooseCloud = chooseCloud;
        }

        IEnumerable<ICloudSyncResult> ICloudSyncConflict.ChooseLocal() {
            var result = _chooseLocal?.Invoke();
            yield return result;
        }

        IEnumerable<ICloudSyncResult> ICloudSyncConflict.ChooseCloud() {
            var result = _chooseCloud?.Invoke();
            yield return result;
        }
    }
    
    public class SaveSlotConflictBetweenLocalAndCloud : BaseCloudSyncResult, ICloudSyncConflict {
        public delegate List<ICloudSyncResult> SlotConflictResultFunc();

        SlotConflictResultFunc _chooseLocal;
        SlotConflictResultFunc _chooseCloud;

        public SaveSlotConflictBetweenLocalAndCloud(string slotPath, DateTime localTimestamp, DateTime cloudFileTimestamp, SlotConflictResultFunc chooseLocal, SlotConflictResultFunc chooseCloud) 
            : base(ResultType.Conflict, slotPath, localTimestamp, cloudFileTimestamp) {
            this._chooseLocal = chooseLocal;
            this._chooseCloud = chooseCloud;
        }
        
        public IEnumerable<ICloudSyncResult> ChooseLocal() {
            return _chooseLocal?.Invoke();
        }
        
        public IEnumerable<ICloudSyncResult> ChooseCloud() {
            return _chooseCloud?.Invoke();
        }
    }

    public class CloudSyncUploadResult : BaseCloudSyncResult {
        public CloudSyncUploadResult(ResultType type, string steamFilePath, DateTime localTimestamp, DateTime cloudFileTimestamp) 
            : base(type, steamFilePath, localTimestamp, cloudFileTimestamp) {
            
        }
    }

    public class CloudSyncDownloadResult : BaseCloudSyncResult {
        readonly Exception _exception;

        public override Exception Exception => _exception;

        public CloudSyncDownloadResult(ResultType type, string steamFilePath, DateTime localTimestamp, DateTime cloudFileTimestamp, Exception exception = null) 
            : base(type, steamFilePath, localTimestamp, cloudFileTimestamp) {
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
        public CloudSyncCloudDeleteResult(ResultType type, string steamFilePath, DateTime localTimestamp, DateTime cloudFileTimestamp) 
            : base(type, steamFilePath, localTimestamp, cloudFileTimestamp) {
            
        }
    }

    public class CloudSyncLocalDeleteResult : BaseCloudSyncResult {
        readonly Exception _exception;
        
        public override Exception Exception => _exception;
        
        public CloudSyncLocalDeleteResult(ResultType type, string steamFilePath, DateTime localTimestamp, DateTime cloudFileTimestamp, Exception exception = null) 
            : base(type, steamFilePath, localTimestamp, cloudFileTimestamp) {
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