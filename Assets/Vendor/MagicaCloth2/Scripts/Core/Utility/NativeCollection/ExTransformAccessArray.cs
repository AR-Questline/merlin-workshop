// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;

namespace MagicaCloth2
{
    /// <summary>
    /// 固定インデックス型の固定インデックスTransformAccessArray管理クラス
    /// 一度確保したインデックスはズレない（ここ重要）
    /// 同じトランスフォームに関しては参照カウンタでまとめる（TransformAccessArrayは重複を許さないため）
    /// </summary>
    public class ExTransformAccessArray : IDisposable
    {
        TransformAccessArray transformArray;

        /// <summary>
        /// ネイティブリストの配列数
        /// ※ジョブでエラーが出ないように事前に確保しておく
        /// </summary>
        int nativeLength;

        /// <summary>
        /// 空インデックススタック
        /// </summary>
        Queue<int> emptyStack;

        /// <summary>
        /// 使用インデックス辞書
        /// </summary>
        Dictionary<int, int> useIndexDict;

        /// <summary>
        /// トランスフォームインデックス辞書
        /// </summary>
        Dictionary<int, int> indexDict;

        /// <summary>
        /// トランスフォーム参照カウンタ辞書
        /// </summary>
        Dictionary<int, int> referenceDict;

        //=========================================================================================
        public ExTransformAccessArray(int capacity, int desiredJobCount = -1)
        {
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// TransformAccessArrayを取得する
        /// </summary>
        /// <returns></returns>
        public TransformAccessArray GetTransformAccessArray()
        {
            return default;
        }

        /// データ追加
        /// 追加したインデックスを返す
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public int Add(Transform element)
        {
            return default;
        }

        /// <summary>
        /// データ削除
        /// 削除されたインデックスは再利用される
        /// </summary>
        /// <param name="index"></param>
        public void Remove(int index)
        {
        }

        public bool Exist(int index)
        {
            return default;
        }

        public bool Exist(Transform element)
        {
            return default;
        }

        /// <summary>
        /// データ使用量
        /// </summary>
        public int Count
        {
            get
            {
                return useIndexDict.Count;
            }
        }

        /// <summary>
        /// データ配列数
        /// </summary>
        public int Length
        {
            get
            {
                return nativeLength;
            }
        }

        public Transform this[int index]
        {
            get
            {
                return transformArray[index];
            }
        }

        public int GetIndex(Transform element)
        {
            return default;
        }

        public void Clear()
        {
        }
    }
}
