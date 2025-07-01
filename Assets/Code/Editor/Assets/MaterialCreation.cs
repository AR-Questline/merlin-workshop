using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Awaken.TG.Editor.Utility.Paths;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets {
    public static class MaterialCreation {

        // === Consts
        static readonly Regex TextureNameRegex = new Regex(@"(?<=tex_).+(?=_[A-Z])", RegexOptions.IgnoreCase);
        static readonly Regex MaterialNameRegex = new Regex(@"(?<=mat_).+", RegexOptions.IgnoreCase);
        static readonly Regex MaterialNameWithTexPrefixRegex = new Regex(@"(?<=tex_).+", RegexOptions.IgnoreCase);
        static readonly Regex MaterialNameWithMeshPrefixRegex = new Regex(@"(?<=mesh_).+(?=_LOD\d)", RegexOptions.IgnoreCase);
        static readonly Regex AlbedoRegex = new Regex(@"(?<=_)(Albedo|BaseMap)", RegexOptions.IgnoreCase);
        static readonly Regex MaskMapRegex = new Regex(@"(?<=_)(MS|MAOHS|MaskMap)", RegexOptions.IgnoreCase);
        static readonly Regex EmissionRegex = new Regex(@"(?<=_)(Emission|Emissive)", RegexOptions.IgnoreCase);
        static readonly Regex AORegex = new Regex(@"(?<=_)AO", RegexOptions.IgnoreCase);
        static readonly Regex NormalRegex = new Regex(@"(?<=_)Normal", RegexOptions.IgnoreCase);

        static readonly MaterialProperties BaseColor = new(new [] {
            new MaterialProperty(Shader.PropertyToID("_BaseColorMap"), AlbedoRegex, "There is no albedo/base color map texture", false),
        });

        static readonly MaterialProperties Normal = new(new [] {
            new MaterialProperty(Shader.PropertyToID("_NormalMap"), NormalRegex, "There is no normal texture", true),
        });

        static readonly MaterialProperties MaskMap = new(new [] {
            new MaterialProperty(Shader.PropertyToID("_MaskMap"), MaskMapRegex, "There is no mask map texture", false),
        });

        static readonly MaterialProperties Emission = new(new [] {
            new MaterialProperty(Shader.PropertyToID("_EmissionMap"), EmissionRegex, "There is no emission texture", false),
            new MaterialProperty(Shader.PropertyToID("_EmissiveColorMap"), EmissionRegex, "There is no emission texture", false),
        });

        static readonly MaterialProperties AmbientOcclusion = new(new [] {
            new MaterialProperty(Shader.PropertyToID("_OcclusionMap"), AORegex, "There is no ao map texture", false),
        });

        public static Dictionary<string, HashSet<PathSourcePair<Texture>>> GetTexturesIn(string directory) {
            return PathUtils.GetFiles(Path.Combine(directory, "Textures"))
                .Select(PathUtils.FilesystemToAssetPath)
                .Where(IsTexture)
                .Select(path => new PathSourcePair<Texture>(path, AssetDatabase.LoadAssetAtPath<Texture>(path)))
                .Where(pair => pair.source != null)
                .GroupBy(pair => TextureNameRegex.Match(pair.path).Value)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToDictionary(g => g.Key, g => g.ToHashSet());
        }

        public static Dictionary<string, HashSet<PathSourcePair<Texture>>> GetTexturesInSelected() {
            return Selection.objects
                .OfType<Texture>()
                .Select(static t => new PathSourcePair<Texture>(AssetDatabase.GetAssetPath(t), t))
                .GroupBy(static pair => TextureNameRegex.Match(pair.path).Value)
                .Where(static g => !string.IsNullOrWhiteSpace(g.Key))
                .ToDictionary(static g => g.Key, static g => g.ToHashSet());
        }

        static bool IsTexture(string path) {
            return path.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase)
                   || path.EndsWith(".tga", StringComparison.InvariantCultureIgnoreCase);
        }
        
        public static Material CreateMaterialFromTextures(string materialName, IReadOnlyCollection<PathSourcePair<Texture>> textures) {
             var pathDirectory = PathUtils.ParentDirectory(Path.GetDirectoryName(textures.ElementAt(0).path));
             var fileName = "Mat_" + Path.ChangeExtension(materialName, ".mat");

             pathDirectory = Path.Combine(pathDirectory, "Materials");
             var materialPath = Path.Combine(pathDirectory, fileName);
             var materialAssetsPath = PathUtils.FilesystemToAssetPath(materialPath);

             if (!Directory.Exists(pathDirectory)) {
                 Directory.CreateDirectory(pathDirectory);
             }

             Material material;
             if (!File.Exists(materialPath)) {
                 material = new Material(Shader.Find("HDRP/Lit"));
                 AssetDatabase.CreateAsset(material, materialAssetsPath);
                 Log.Important?.Info($"Created material {fileName}");
             }
             material = AssetDatabase.LoadAssetAtPath<Material>(materialAssetsPath);

             bool updated = false;
             // will have content in situation when material can contains not valid setup
             StringBuilder errorMessageBuilder = new();

             updated = BaseColor.Set(textures, material, errorMessageBuilder) || updated;
             updated = Normal.Set(textures, material, errorMessageBuilder) || updated;
             updated = MaskMap.Set(textures, material, errorMessageBuilder) || updated;
             updated = Emission.Set(textures, material, errorMessageBuilder) || updated;
             updated = AmbientOcclusion.Set(textures, material, errorMessageBuilder) || updated;

             if (updated) {
                 EditorUtility.SetDirty(material);
                 Log.Important?.Info($"Updated material {fileName}");
             }

             if (errorMessageBuilder.Length > 0) {
                 Log.Important?.Info($"<color=orange>Material {fileName} has some suspicious textures setup. Please make sure setup is valid. {errorMessageBuilder}</color>");
             }

             return material;
        }

        public static string MaterialName(string materialName, string fallbackName) {
            var name = TryMatchMaterialName(materialName) ?? TryMatchMaterialName(fallbackName);
            return name ?? string.Empty;
        }

        static string TryMatchMaterialName(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                return null;
            }

            var match = MaterialNameRegex.Match(name);
            if (match.Success) {
                return match.Value;
            }

            match = MaterialNameWithTexPrefixRegex.Match(name);
            if (match.Success) {
                return match.Value;
            }

            match = MaterialNameWithMeshPrefixRegex.Match(name);
            if (match.Success) {
                return match.Value;
            }

            return null;
        }

        public class MaterialProperties {
            public readonly MaterialProperty[] properties;

            public MaterialProperties(MaterialProperty[] properties) {
                this.properties = properties;
            }

            public bool Set(IReadOnlyCollection<PathSourcePair<Texture>> textures, Material material, StringBuilder messageBuilder) {
                for (var i = 0; i < properties.Length; i++) {
                    if (properties[i].Set(textures, material, messageBuilder)) {
                        return true;
                    }
                }
                return false;
            }
        }

        public class MaterialProperty {
            public readonly int shaderId;
            public readonly Regex textureRegex;
            public readonly string errorMessage;
            public readonly bool isNormal;

            public MaterialProperty(int shaderId, Regex textureRegex, string errorMessage, bool isNormal) {
                this.shaderId = shaderId;
                this.textureRegex = textureRegex;
                this.errorMessage = errorMessage;
                this.isNormal = isNormal;
            }

            public bool Set(IReadOnlyCollection<PathSourcePair<Texture>> textures, Material material, StringBuilder messageBuilder) {
                if (!material.HasProperty(shaderId) || material.GetTexture(shaderId) != null) {
                    return false;
                }
                var texture = textures.FirstOrDefault(t => textureRegex.IsMatch(t.path));
                if (!texture?.source) {
                    messageBuilder.AppendLine(errorMessage);
                    return false;
                }
                if (isNormal) {
                    var importer = (TextureImporter) AssetImporter.GetAtPath(texture.path);
                    if (importer.textureType != TextureImporterType.NormalMap) {
                        importer.textureType = TextureImporterType.NormalMap;
                        importer.SaveAndReimport();
                    }
                }
                material.SetTexture(shaderId, texture.source);
                return true;
            }
        }
    }
}