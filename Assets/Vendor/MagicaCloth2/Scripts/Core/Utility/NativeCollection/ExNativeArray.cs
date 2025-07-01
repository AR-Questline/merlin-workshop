// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// 拡張可能なNativeArrayクラス
    /// 領域が不足すると自動で拡張する
    /// データはChankDataにより開始インデックスと長さが管理される
    /// データは削除可能で削除された領域は管理され再利用される
    /// 領域の管理が必要なためExSimpleNativeArrayに比べてやや重いので注意！
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ExNativeArray<T> : IDisposable where T : unmanaged
    {
        NativeArray<T> nativeArray;

        List<DataChunk> emptyChunks = new List<DataChunk>();

        int useCount;

        public void Dispose()
        {
        }

        public bool IsValid => nativeArray.IsCreated;

        /// <summary>
        /// NativeArrayの領域サイズ
        /// 実際に利用されているサイズとは異なるので注意！
        /// </summary>
        public int Length => nativeArray.IsCreated ? nativeArray.Length : 0;

        /// <summary>
        /// 実際に利用されているデータ数（最後のチャンクの最後尾＋１）
        /// </summary>
        public int Count => useCount;

        //=========================================================================================
        public ExNativeArray()
        {
        }

        public ExNativeArray(int emptyLength, bool create = false) : this()
        {
        }

        public ExNativeArray(int emptyLength, T fillData) : this(emptyLength)
        {
        }

        public ExNativeArray(NativeArray<T> dataArray) : this()
        {
        }

        public ExNativeArray(T[] dataArray) : this()
        {
        }

        //=========================================================================================
#if false
        /// <summary>
        /// 使用配列カウントを設定する
        /// 有効数を書き換えすべてのデータを１つのチャンクとして使用中とする
        /// かなり強力な機能なので扱いには注意すること！
        /// </summary>
        /// <param name="count"></param>
        public void SetUseCount(int count)
        {
            useCount = count;
            emptyChunks.Clear();
            if (useCount > Length)
            {
                // 未使用領域を１つの空チャンクとして登録する
                var chunk = new DataChunk(useCount, Length - useCount);
                emptyChunks.Add(chunk);
            }
        }
#endif

        /// <summary>
        /// 指定サイズの領域を追加しそのチャンクを返す
        /// </summary>
        /// <param name="dataLength"></param>
        /// <returns></returns>
        public DataChunk AddRange(int dataLength)
        {
            return default;
        }

        public DataChunk AddRange(int dataLength, T fillData = default(T))
        {
            return default;
        }

        public DataChunk AddRange(T[] array)
        {
            return default;
        }

        public DataChunk AddRange(NativeArray<T> narray, int length = 0)
        {
            return default;
        }

        public DataChunk AddRange(ExNativeArray<T> exarray)
        {
            return default;
        }

        public DataChunk AddRange(ExSimpleNativeArray<T> exarray)
        {
            return default;
        }

        /// <summary>
        /// 型は異なるが型のサイズは同じ配列を追加する。Vector3->float3など。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public unsafe DataChunk AddRange<U>(U[] array) where U : struct
        {
            return default;
        }

        /// <summary>
        /// 型は異なるが型のサイズは同じNativeArrayを追加する。Vector3->float3など。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="udata"></param>
        /// <returns></returns>
        public DataChunk AddRange<U>(NativeArray<U> udata) where U : struct
        {
            return default;
        }

        /// <summary>
        /// 型もサイズも異なる配列を追加する。int[] -> int3[]など。
        /// データはそのままメモリコピーされる。例えばint[]からint3[]へ追加すると次のようになる。
        /// int[]{1, 2, 3, 4, 5, 6} => int3[]{{1, 2, 3}, {4, 5, 6}}
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public unsafe DataChunk AddRangeTypeChange<U>(U[] array) where U : struct
        {
            return default;
        }

        /// <summary>
        /// 型もサイズも異なる配列を部分的にコピーする。Vector4[] -> float3など。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public unsafe DataChunk AddRangeStride<U>(U[] array) where U : struct
        {
            return default;
        }

        public DataChunk Add(T data)
        {
            return default;
        }

        /// <summary>
        /// 指定チャンクのデータ数を拡張し新しいチャンクを返す
        /// 古いチャンクのデータは新しいチャンクにコピーされる
        /// </summary>
        /// <param name="c"></param>
        /// <param name="newDataLength"></param>
        /// <returns></returns>
        public DataChunk Expand(DataChunk c, int newDataLength)
        {
            return default;
        }

        /// <summary>
        /// 指定チャンクのデータ数を拡張し新しいチャンクを返す
        /// 古いチャンクのデータは新しいチャンクにコピーされる
        /// </summary>
        /// <param name="c"></param>
        /// <param name="newDataLength"></param>
        /// <returns></returns>
        public DataChunk ExpandAndFill(DataChunk c, int newDataLength, T fillData = default(T), T clearData = default(T))
        {
            return default;
        }

        public T[] ToArray()
        {
            return default;
        }

        public void CopyTo(T[] array)
        {
        }

        public void CopyTo<U>(U[] array) where U : struct
        {
        }

        public void CopyFrom(NativeArray<T> array)
        {
        }

        public void CopyFrom<U>(NativeArray<U> array) where U : struct
        {
        }

        /// <summary>
        /// 型もサイズも異なる配列にデータをコピーする。
        /// int3 -> int[]など
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        public unsafe void CopyTypeChange<U>(U[] array) where U : struct
        {
        }

        /// <summary>
        /// 型もサイズも異なる配列にデータを断片的にコピーする。
        /// float3 -> Vector4[]など。この場合はVector4にはxyzのみ書き込まれる。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        public unsafe void CopyTypeChangeStride<U>(U[] array) where U : struct
        {
        }

        /// <summary>
        /// すぐに利用できる空領域のみ追加する
        /// </summary>
        /// <param name="dataLength"></param>
        public void AddEmpty(int dataLength)
        {
        }

        public void Remove(DataChunk chunk)
        {
        }

        public void RemoveAndFill(DataChunk chunk, T clearData = default(T))
        {
        }

        public void Fill(T fillData = default(T))
        {
        }

        public void Fill(DataChunk chunk, T fillData = default(T))
        {
        }

        unsafe void FillInternal(int start, int size, T fillData = default(T))
        {
        }

        public void Clear()
        {
        }

        public T this[int index]
        {
            get
            {
                return nativeArray[index];
            }
            set
            {
                nativeArray[index] = value;
            }
        }

        public unsafe ref T GetRef(int index)
        {
            throw new NotImplementedException();
        }

        //public unsafe ref T GetRef(int index)
        //{
        //    var span = new Span<T>(nativeArray.GetUnsafePtr(), nativeArray.Length);
        //    return ref span[index];
        //}

        /// <summary>
        /// Jobで利用する場合はこの関数でNativeArrayに変換して受け渡す
        /// </summary>
        /// <returns></returns>
        public NativeArray<T> GetNativeArray()
        {
            return default;
        }

        /// <summary>
        /// Jobで利用する場合はこの関数でNativeArrayに変換して受け渡す(型変更あり)
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <returns></returns>
        public NativeArray<U> GetNativeArray<U>() where U : struct
        {
            return default;
        }

        //=========================================================================================
        DataChunk GetEmptyChunk(int dataLength)
        {
            return default;
        }

        void AddEmptyChunk(DataChunk chunk)
        {
        }

        public override string ToString()
        {
            return default;
        }

        public string ToSummary()
        {
            return default;
        }
    }
}
