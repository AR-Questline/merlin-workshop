using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Awaken.TG.Main.UI.TitleScreen.FileVerification {
    public static unsafe class FileHasher {
        public struct Result {
            public int index;
            public string filename;
            public bool fileMissing;
            public uint4 hash;
            
            public Result WithHash(uint4 hash) {
                this.hash = hash;
                return this;
            }
        }

        class Hasher {
            readonly byte* _buffer;
            readonly int _bufferSize;

            xxHash3.StreamingState _hasher;
            
            string _filename;
            int _index;
            
            ReadHandle _readInfo;
            FileInfoResult _info;
            ReadHandle _readFile;
            ReadCommand _command;

            long _offset;
            int _size;
            uint4 _hash;
            
            public State CurrentState { get; private set; }
            
            public Result Result => new() {
                filename = _filename,
                index = _index,
                hash = _hash,
                fileMissing = CurrentState == State.FileMissing
            };
            
            public Hasher(byte* buffer, int bufferSize) {
                _buffer = buffer;
                _bufferSize = bufferSize;
                _hasher = new xxHash3.StreamingState(false);
            }

            public void StartReading(string filename, int index) {
                _filename = filename;
                _index = index;
                _readInfo = AsyncReadManager.GetFileInfo(_filename, (FileInfoResult*)UnsafeUtility.AddressOf(ref _info));
                CurrentState = State.ReadingInfo;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update() {
                if (CurrentState == State.ReadingInfo) {
                    var readInfoStatus = _readInfo.Status;
                    if (readInfoStatus is ReadStatus.Failed or ReadStatus.Canceled or ReadStatus.Truncated) {
                        CurrentState = State.Failed;
                        return;
                    }
                    if (readInfoStatus != ReadStatus.Complete) {
                        return;
                    }
                    _readInfo.Dispose();
                    _readInfo = default;
                    
                    if (_info.FileState == FileState.Absent) {
                        CurrentState = State.FileMissing;
                        return;
                    }
                    
                    if (_info.FileSize == 0) {
                        _hash = uint4.zero;
                        CurrentState = State.Completed;
                        return;
                    }
                    StartReadingNextPart();
                }

                if (CurrentState == State.ReadingFile) {
                    var readFileStatus = _readFile.Status;
                    if (readFileStatus is ReadStatus.Failed or ReadStatus.Canceled or ReadStatus.Truncated) {
                        CurrentState = State.Failed;
                        return;
                    }
                    if (readFileStatus != ReadStatus.Complete) {
                        return;
                    }

                    _readFile.Dispose();
                    _readFile = default;
                    _hasher.Update(_buffer, _size);
                    _offset += _size;

                    if (_offset >= _info.FileSize) {
                        _hash = _hasher.DigestHash128();
                        CurrentState = State.Completed;
                        return;
                    }

                    StartReadingNextPart();
                }
            }

            public void Clear() {
                _hasher.Reset(false);
                _filename = null;
                _index = 0;
                _readInfo = default;
                _info = default;
                _readFile = default;
                _command = default;
                CurrentState = State.None;
                _offset = 0;
                _size = 0;
                _hash = uint4.zero;
            }

            public void Dispose() {
                if (_readInfo.IsValid()) {
                    _readInfo.Cancel();
                    while (_readInfo.Status == ReadStatus.InProgress) {
                        Thread.Sleep(1);
                    }
                    _readInfo.Dispose();
                }
                if (_readFile.IsValid()) {
                    _readFile.Cancel();
                    while (_readFile.Status == ReadStatus.InProgress) {
                        Thread.Sleep(1);
                    }
                    _readFile.Dispose();
                }
            }

            void StartReadingNextPart() {
                _size = (int)math.min(_bufferSize, _info.FileSize - _offset);
                _command.Offset = _offset;
                _command.Size = _size;
                _command.Buffer = _buffer;
                _readFile = AsyncReadManager.Read(_filename, (ReadCommand*)UnsafeUtility.AddressOf(ref _command), 1);
                CurrentState = State.ReadingFile;
            }

            public enum State : byte {
                None,
                ReadingInfo,
                ReadingFile,
                Completed,
                Failed,
                FileMissing
            }
        }
        
        struct BatchedHasher : IDisposable {
            readonly byte* _bytes;
            Hasher[] _hashers;
            
            public BatchedHasher(int batchCount, int batchSize) {
                _bytes = (byte*)UnsafeUtility.Malloc(batchCount * batchSize, UnsafeUtility.AlignOf<byte>(), Allocator.Persistent);
                _hashers = new Hasher[batchCount];
                for (int i = 0; i < batchCount; i++) {
                    _hashers[i] = new Hasher(_bytes + i * batchSize, batchSize);
                }
            }

            public int Length => _hashers.Length;
            public Hasher this[int index] => _hashers[index];

            public void Dispose() {
                for (int i = 0; i < _hashers.Length; i++) {
                    _hashers[i].Dispose();
                }
                UnsafeUtility.Free(_bytes, Allocator.Persistent);
            }
        }
        
        public static IEnumerable<Result> Hash(IEnumerable<string> filenames, int batchCount = 64, int batchSize = 512 * 1024) {
            using var hashers = new BatchedHasher(batchCount, batchSize);
            using var iterator = filenames.GetEnumerator();
            int fileIndex = 0;
            
            bool processed;
            do {
                processed = false;
                for (int i = 0; i < hashers.Length; i++) {
                    var hasher = hashers[i];
                    if (hasher.CurrentState == Hasher.State.None) {
                        if (iterator.MoveNext()) {
                            hasher.StartReading(iterator.Current, fileIndex++);
                        } else {
                            continue;
                        }
                    }
                    hasher.Update();
                    if (hasher.CurrentState == Hasher.State.Completed) {
                        yield return hasher.Result;
                        hasher.Clear();
                    } else if (hasher.CurrentState is Hasher.State.Failed or Hasher.State.FileMissing) {
                        yield return hasher.Result.WithHash(uint4.zero);
                        hasher.Clear();
                    }
                    processed = true;
                }
            } while (processed);
        }
    }
}