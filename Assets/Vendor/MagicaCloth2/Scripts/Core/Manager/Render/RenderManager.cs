// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 描画の管理とメッシュ更新マネージャ
    /// </summary>
    public class RenderManager : IManager, IValid
    {
        /// <summary>
        /// 描画データをint型ハンドルで管理する
        /// </summary>
        Dictionary<int, RenderData> renderDataDict = new Dictionary<int, RenderData>();

        bool isValid = false;

        //=========================================================================================
        public void Initialize()
        {
        }

        public void EnterdEditMode()
        {
        }

        public void Dispose()
        {
        }

        public bool IsValid()
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 管理するレンダラーの追加（メインスレッドのみ）
        /// </summary>
        /// <param name="ren"></param>
        /// <returns></returns>
        public int AddRenderer(Renderer ren, RenderSetupData referenceSetupData, RenderSetupData.UniqueSerializationData referenceUniqueSetupData)
        {
            return default;
        }

        public bool RemoveRenderer(int handle)
        {
            return default;
        }

        public RenderData GetRendererData(int handle)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 有効化
        /// </summary>
        /// <param name="handle"></param>
        public void StartUse(ClothProcess cprocess, int handle)
        {
        }

        /// <summary>
        /// 無効化
        /// </summary>
        /// <param name="handle"></param>
        public void EndUse(ClothProcess cprocess, int handle)
        {
        }

        //=========================================================================================
        /// <summary>
        /// レンダリング前更新
        /// </summary>
        void PreRenderingUpdate()
        {
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
        }
    }
}
