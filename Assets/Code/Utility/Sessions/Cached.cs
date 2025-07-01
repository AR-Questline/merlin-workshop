using System;
using System.Runtime.CompilerServices; // used in !UNITY_EDITOR
using UnityEngine;

namespace Awaken.Utility.Sessions {
    public struct CacheVersion {
#if UNITY_EDITOR
        long _sessionID;

        public bool NeedUpdate() {
            if (!Application.isPlaying) {
                return true;
            }
            var sessionId = SessionUtils.SessionID;
            var result = _sessionID != sessionId;
            _sessionID = sessionId;
            return result;
        }

        public override string ToString() => _sessionID.ToString();
#else
        bool _updated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool NeedUpdate() {
            var result = !_updated;
            _updated = true;
            return result;
        }
        
        public override string ToString() => string.Empty;
#endif
    }

    public struct Cached<TValue> {
        readonly Func<TValue> _func;
        
        CacheVersion _cacheVersion;
        TValue _value;
        
        public Cached(Func<TValue> func) : this() {
            _func = func;
        }

        public TValue Get() {
            if (_cacheVersion.NeedUpdate()) {
                _value = _func();
            }
            return _value;
        }
        
        public TValue GetNoRefresh() {
            _value ??= _func();
            return _value;
        }
    }
    
    public struct Cached<TOwner, TValue> {
        readonly Func<TOwner, TValue> _func;
        
        CacheVersion _cacheVersion;
        TValue _value;
        
        public Cached(Func<TOwner, TValue> func) : this() {
            _func = func;
        }

        public TValue Get(TOwner owner) {
            if (_cacheVersion.NeedUpdate()) {
                _value = _func(owner);
            }
            return _value;
        }
        
        public TValue GetNoRefresh(TOwner owner) {
            if (_value == null) {
                _value = _func(owner);
            }
            return _value;
        }
    }
    
    
    [UnityEngine.Scripting.Preserve]
    public struct Cached<TOwner, TAdditionalData, TValue> {
        readonly Func<TOwner, TAdditionalData, TValue> _func;
        
        CacheVersion _cacheVersion;
        TValue _value;
        
        public Cached(Func<TOwner, TAdditionalData, TValue> func) : this() {
            _func = func;
        }

        public TValue Get(TOwner owner, TAdditionalData data) {
            if (_cacheVersion.NeedUpdate()) {
                _value = _func(owner, data);
            }
            return _value;
        }
    }
}