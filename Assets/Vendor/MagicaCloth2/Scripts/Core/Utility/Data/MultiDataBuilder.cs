// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Collections;

namespace MagicaCloth2
{
    /// <summary>
    /// T型のデータリストを構築し要素ごとにそのスタートインデックスとデータカウンタを生成する
    /// 出力はT型のデータ配列と、要素ごとのスタートインデックスとカウンタが１つのuintにパックされた配列の２つ
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MultiDataBuilder<T> : IDisposable where T : unmanaged
    {
        int indexCount;

        public NativeParallelMultiHashMap<int, T> Map;

        //=========================================================================================
        public MultiDataBuilder(int indexCount, int dataCapacity)
        {
        }

        public void Dispose()
        {
        }

        public int Count() => Map.Count();

        public int GetDataCount(int index)
        {
            return default;
        }

        public void Add(int key, T data)
        {
        }

        //public int AddAndReturnIndex(int key, T data)
        //{
        //    int cnt = Map.CountValuesForKey(key);
        //    Map.Add(key, data);
        //    return cnt;
        //}

        public int CountValuesForKey(int key)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 内部HashMapのデータをT型配列と要素ごとのスタートインデックスとカウンタ配列の２つに分離して返す
        /// 出力はT型のデータ配列と、要素ごとのスタートインデックス(20bit)とカウンタ(12bit)を１つのuintにパックした配列となる
        /// </summary>
        /// <returns></returns>
        public (T[], uint[]) ToArray()
        {
            return default;
        }

        public uint[] ToIndexArray()
        {
            return default;
        }

        /// <summary>
        /// 内部HashMapのデータをT型配列と要素ごとのスタートインデックス+カウンタの２つのNativeArrayに分離して返す
        /// 出力はT型のデータ配列と、要素ごとのスタートインデックス(20bit)とカウンタ(12bit)を１つのuintにパックした配列となる
        /// </summary>
        /// <param name="indexArray"></param>
        /// <param name="dataArray"></param>
        public void ToNativeArray(out NativeArray<uint> indexArray, out NativeArray<T> dataArray)
        {
            indexArray = default(NativeArray<uint>);
            dataArray = default(NativeArray<T>);
        }
    }
}
