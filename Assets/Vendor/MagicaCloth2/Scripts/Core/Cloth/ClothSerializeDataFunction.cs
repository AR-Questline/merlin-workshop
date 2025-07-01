// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Serialize data (1)
    /// function part.
    /// </summary>
    public partial class ClothSerializeData : IDataValidate, IValid, ITransform
    {
        /// <summary>
        /// 検証結果
        /// Verification Results.
        /// </summary>
        ResultCode verificationResult;
        public Define.Result VerificationResult => verificationResult.Result;


        public ClothSerializeData()
        {
        }

        /// <summary>
        /// クロスを構築するための最低限の情報が揃っているか検証する
        /// Check if you have the minimum information to construct the cloth.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return default;
        }

        public void DataValidate()
        {
        }

        /// <summary>
        /// エディタメッシュの更新を判定するためのハッシュコード
        /// Hashcode for determining editor mesh updates.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return default;
        }

        /// <summary>
        /// ジョブで参照する構造体に変換して返す
        /// Convert to a structure to be referenced in the job and return.
        /// </summary>
        /// <returns></returns>
        public ClothParameters GetClothParameters()
        {
            return default;
        }

        class TempBuffer
        {
            ClothProcess.ClothType clothType;
            List<Renderer> sourceRenderers;
            PaintMode paintMode;
            List<Texture2D> paintMaps;
            List<Transform> rootBones;
            RenderSetupData.BoneConnectionMode connectionMode;
            float rotationalInterpolation;
            float rootRotation;
            ClothUpdateMode updateMode;
            float animationPoseRatio;
            ReductionSettings reductionSetting;
            CustomSkinningSettings customSkinningSetting;
            NormalAlignmentSettings normalAlignmentSetting;
            ClothNormalAxis normalAxis;
            List<ColliderComponent> colliderList;
            List<Transform> collisionBones;
            MagicaCloth synchronization;
            float stablizationTimeAfterReset;
            float blendWeight;
            CullingSettings.CameraCullingMode cullingMode;
            CullingSettings.CameraCullingMethod cullingMethod;
            List<Renderer> cullingRenderers;
            Transform anchor;
            float anchorInertia;

            internal TempBuffer(ClothSerializeData sdata)
            {
            }

            internal void Push(ClothSerializeData sdata)
            {
            }

            internal void Pop(ClothSerializeData sdata)
            {
            }
        }

        /// <summary>
        /// パラメータをJsonへエクスポートする
        /// Export parameters to Json.
        /// </summary>
        /// <returns></returns>
        public string ExportJson()
        {
            return default;
        }

        /// <summary>
        /// パラメータをJsonからインポートする
        /// Parameterブロックの値型のみがインポートされる
        /// Import parameters from Json.
        /// Only value types of Parameter blocks are imported.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public bool ImportJson(string json)
        {
            return default;
        }

        /// <summary>
        /// 別のシリアライズデータからインポートする
        /// Import from another serialized data.
        /// </summary>
        /// <param name="sdata"></param>
        /// <param name="deepCopy">true = Copy all, false = parameter only</param>
        public void Import(ClothSerializeData sdata, bool deepCopy = false)
        {
        }

        /// <summary>
        /// 別のクロスコンポーネントからインポートする
        /// Import from another cloth component.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="deepCopy"></param>
        public void Import(MagicaCloth src, bool deepCopy = false)
        {
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
        }

        /// <summary>
        /// BoneSpring判定
        /// </summary>
        /// <returns></returns>
        public bool IsBoneSpring() => clothType == ClothProcess.ClothType.BoneSpring;
    }
}
