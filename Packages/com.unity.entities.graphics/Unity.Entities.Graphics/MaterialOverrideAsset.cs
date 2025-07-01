using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Represents a material property override asset.
/// </summary>
/// <remarks>
/// This enables you to override material properties for an authoring GameObject.
/// </remarks>
[CreateAssetMenu(fileName = "MaterialOverrideAsset", menuName = "Shader/Material Override Asset", order = 1)] //TODO(andrew.theisen): where should this live in the menu?
public class MaterialOverrideAsset : ScriptableObject
{
    /// <summary>
    /// Represents a data container of material override properties.
    /// </summary>
    [Serializable]
    public struct OverrideData
    {
        /// <summary>
        /// The in-shader name of the material property.
        /// </summary>
        public string name;

        /// <summary>
        /// The display name of the material property.
        /// </summary>
        public string displayName;

        /// <summary>
        /// The name of the shader.
        /// </summary>
        public string shaderName;

        /// <summary>
        /// The name of the material.
        /// </summary>
        public string materialName;

        /// <summary>
        /// The type of the shader property.
        /// </summary>
        public ShaderPropertyType type;

        /// <summary>
        /// The override value of the material property.
        /// </summary>
        public Vector4 value;

        /// <summary>
        /// Instance override.
        /// </summary>
        public bool instanceOverride;
    }

    /// <summary>
    /// A list of material property overrides.
    /// </summary>
    public List<OverrideData> overrideList = new List<OverrideData>();

    /// <summary>
    /// The material to apply the overrides to.
    /// </summary>
    public Material material;

    /// <summary>
    /// Gets the material property type from an OverrideData object.
    /// </summary>
    /// <param name="overrideData">The OverrideDat to use.</param>
    /// <returns>Returns the type of the material property.</returns>
    public Type GetTypeFromAttrs(OverrideData overrideData)
    {
        Type overrideType = null;
        bool componentExists = false;
        foreach (var t in TypeManager.GetAllTypes())
        {
            if (t.Type != null)
            {
                //TODO(andrew.theisen): this grabs the first IComponentData that matches these attributes but multiple matches can exist such as URPMaterialPropertyBaseColor
                //                and HDRPMaterialPropertyBaseColor. It actually shouldn't matter which one is used can they can work either shader.
                foreach (var attr in t.Type.GetCustomAttributes(typeof(MaterialPropertyAttribute), false))
                {
                    if (TypeManager.IsSharedComponentType(t.TypeIndex))
                    {
                        continue;
                    }

                    var propAttr = (MaterialPropertyAttribute)attr;
                    //TODO(andrew.theisen): So this won't use exisiting IComponentDatas always. for example:
                    //                HDRPMaterialPropertyEmissiveColor is Float3, but the ShaderPropertyType
                    //                is Color but without alpha. can fix this when we can get the DOTS
                    //                type or byte size of the property
                    if (overrideData.type == ShaderPropertyType.Vector || overrideData.type == ShaderPropertyType.Color)
                    {
                        // propFormat = MaterialPropertyFormat.Float4;
                    }
                    else if (overrideData.type == ShaderPropertyType.Float || overrideData.type == ShaderPropertyType.Range)
                    {
                        // propFormat = MaterialPropertyFormat.Float;
                    }
                    else
                    {
                        break;
                    }

                    if (propAttr.Name == overrideData.name)
                    {
                        overrideType = t.Type;
                        componentExists = true;
                        break;
                    }
                }
            }
            if (componentExists)
            {
                break;
            }
        }
        return overrideType;
    }

    /// <inheritdoc/>
    public void OnValidate()
    {
        foreach (var overrideComponent in FindObjectsByType<MaterialOverride>(FindObjectsSortMode.None))
        {
            if (overrideComponent.overrideAsset == this)
            {
                if (material != null)
                {
                    var newList = new List<OverrideData>();
                    foreach (var overrideData in overrideList)
                    {
                        int index = overrideComponent.overrideList.FindIndex(d => d.name == overrideData.name);
                        if (index != -1)
                        {
                            if (overrideComponent.overrideList[index].instanceOverride)
                            {
                                newList.Add(overrideComponent.overrideList[index]);
                                continue;
                            }
                        }
                        newList.Add(overrideData);
                    }
                    overrideComponent.overrideList = newList;
                    overrideComponent.ApplyMaterialProperties();
                }
                else
                {
                    overrideComponent.overrideList = new List<OverrideData>();
                    overrideComponent.ClearOverrides();
                }
            }
        }
    }
}
