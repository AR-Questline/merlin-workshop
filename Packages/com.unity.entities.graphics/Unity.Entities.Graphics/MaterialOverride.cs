using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

interface IHelper
{
    void AddComponentData(EntityManager dstManager, Entity entity, IComponentData iComponentData);
}

class Helper<T> : IHelper where T : unmanaged, IComponentData
{
    public void AddComponentData(EntityManager dstManager, Entity entity, IComponentData iComponentData)
    {
        dstManager.AddComponentData(entity, (T)iComponentData);
    }
}

/// <summary>
/// Represents a material override authoring component.
/// </summary>
[DisallowMultipleComponent]
[ExecuteInEditMode]
public class MaterialOverride : MonoBehaviour
{
    /// <summary>
    /// The material asset to override.
    /// </summary>
    public MaterialOverrideAsset overrideAsset;

    /// <summary>
    /// The list of overridden material properties.
    /// </summary>
    public List<MaterialOverrideAsset.OverrideData> overrideList = new List<MaterialOverrideAsset.OverrideData>();

    /// <summary>
    /// Applies the material properties to the renderer.
    /// </summary>
    public void ApplyMaterialProperties()
    {
        if (overrideAsset != null)
        {
            if (overrideAsset.material != null)
            {
                //TODO(andrew.theisen): needs support for multiple renderers
                var renderer = GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(null);
                    var propertyBlock = new MaterialPropertyBlock();
                    foreach (var overrideData in overrideList)
                    {
                        if (overrideData.type == ShaderPropertyType.Color)
                        {
                            propertyBlock.SetColor(overrideData.name, overrideData.value);
                        }
                        else if (overrideData.type == ShaderPropertyType.Vector)
                        {
                            propertyBlock.SetVector(overrideData.name, overrideData.value);
                        }
                        else if (overrideData.type == ShaderPropertyType.Float || overrideData.type == ShaderPropertyType.Range)
                        {
                            propertyBlock.SetFloat(overrideData.name, overrideData.value.x);
                        }
                    }

                    renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }
    }

    /// <inheritdoc/>
    public void OnValidate()
    {
        if (overrideAsset != null)
        {
            var newList = new List<MaterialOverrideAsset.OverrideData>();
            foreach (var overrideData in overrideAsset.overrideList)
            {
                int index = overrideList.FindIndex(d => d.name == overrideData.name);
                if (index != -1)
                {
                    if (overrideList[index].instanceOverride)
                    {
                        newList.Add(overrideList[index]);
                        continue;
                    }
                }
                newList.Add(overrideData);
            }
            overrideList = newList;
            ApplyMaterialProperties();
        }
        else
        {
            overrideList = new List<MaterialOverrideAsset.OverrideData>();
            ClearOverrides();
        }
    }

    /// <summary>
    /// Resets the renderer.
    /// </summary>
    public void ClearOverrides()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.SetPropertyBlock(null);
        }
    }

    /// <summary>
    /// Calls ClearOverrides when the behaviour becomes disabled.
    /// </summary>
    public void OnDisable()
    {
        ClearOverrides();
    }
}

class MaterialOverrideBaker : Baker<MaterialOverride>
{
    public override unsafe void Bake(MaterialOverride authoring)
    {
        if (authoring.overrideAsset != null && authoring.overrideAsset.material != null)
        {
            foreach (var overrideData in authoring.overrideList)
            {
                Type overrideType = authoring.overrideAsset.GetTypeFromAttrs(overrideData);
                if (overrideType != null)
                {
                    var overrideTypeIndex = TypeManager.GetTypeIndex(overrideType);
                    var typeInfo = TypeManager.GetTypeInfo(overrideTypeIndex);
                    var entity = GetEntity(authoring, TransformUsageFlags.Renderable);
                    int dataSize = 0;
                    var componentData = UnsafeUtility.Malloc(typeInfo.TypeSize, typeInfo.AlignmentInBytes, Allocator.Temp);

                    if (overrideData.type == ShaderPropertyType.Vector || overrideData.type == ShaderPropertyType.Color)
                    {
                        var data = new float4(overrideData.value.x, overrideData.value.y, overrideData.value.z, overrideData.value.w);
                        dataSize = sizeof(float4);

                        Assert.AreEqual(dataSize, typeInfo.TypeSize, "Material Override components must contain only the exact field it is overriding.");
                        UnsafeUtility.MemCpy(componentData, &data, dataSize);
                    }
                    else if (overrideData.type == ShaderPropertyType.Float || overrideData.type == ShaderPropertyType.Range)
                    {
                        float data = overrideData.value.x;
                        dataSize = sizeof(float);

                        Assert.AreEqual(dataSize, typeInfo.TypeSize, "Material Override components must contain only the exact field it is overriding.");
                        UnsafeUtility.MemCpy(componentData, &data, dataSize);
                    }

                    UnsafeAddComponent(entity, overrideTypeIndex, typeInfo.TypeSize, componentData);
                    UnsafeUtility.Free(componentData, Allocator.Temp);
                }
            }
        }
    }
}
