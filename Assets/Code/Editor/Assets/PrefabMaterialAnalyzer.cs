using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Awaken.TG.Editor.Assets {
    public class PrefabMaterialAnalyzer : EditorWindow {
        const string WindowTitle = "Prefab Material Analyzer";

        const int ItemsPerPage = 20;

        [SerializeField] string selectedFolderPath = "Assets/";
        [SerializeField] bool includeSubfolders = true;
        [SerializeField] bool groupByMaterial = true;
        [SerializeField] int selectedShaderIndex;
        [SerializeField] int currentPage;

        Vector2 _scrollPosition;
        AnalysisResults _results;
        bool _isAnalyzing;
        string[] _shaderNames;
        Shader _selectedShader;
        int _totalPages;
        int _filteredItemCount;

        [MenuItem("TG/Assets/Prefab Material Analyzer")]
        static void ShowWindow() {
            var window = GetWindow<PrefabMaterialAnalyzer>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        void OnGUI() {
            EditorGUILayout.Space(10);

            // === Folder Selection Section
            EditorGUILayout.LabelField("Folder Selection", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            selectedFolderPath = EditorGUILayout.TextField("Folder Path", selectedFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(80))) {
                var path = EditorUtility.OpenFolderPanel("Select Prefab Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path)) {
                    if (path.StartsWith(Application.dataPath)) {
                        selectedFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                    } else {
                        EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder within the Assets directory.", "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
            groupByMaterial = EditorGUILayout.Toggle("Group by Material", groupByMaterial);

            EditorGUILayout.Space(5);

            // === Filter Section
            if (_results != null && _shaderNames != null && _shaderNames.Length > 0) {
                EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                selectedShaderIndex = EditorGUILayout.Popup("Filter by Shader", selectedShaderIndex, _shaderNames);
                if (EditorGUI.EndChangeCheck()) {
                    UpdateShaderFilter();
                }
            }

            EditorGUILayout.Space(10);

            // === Analysis Button
            GUI.enabled = !_isAnalyzing && !string.IsNullOrEmpty(selectedFolderPath);
            if (GUILayout.Button(_isAnalyzing ? "Analyzing..." : "Analyze Prefabs", GUILayout.Height(30))) {
                AnalyzePrefabs();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(10);

            // === Results Section
            if (_results != null) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Analysis Results - {_results.totalPrefabs} Prefabs Scanned, {_results.totalMeshes} Meshes, {_results.totalMaterials} Materials, {_results.totalTextures} Textures", EditorStyles.boldLabel);
                if (GUILayout.Button("Export to CSV", GUILayout.Width(120))) {
                    ExportToCSV();
                }
                if (GUILayout.Button("Export Summary", GUILayout.Width(120))) {
                    ExportSummary();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // === Info message
                if (!groupByMaterial) {
                    var prefabsWithMaterials = 0;
                    var prefabSet = new HashSet<string>();
                    foreach (var materialData in _results.materials.Values) {
                        if (PassesShaderFilter(materialData)) {
                            foreach (var prefabPath in materialData.prefabs) {
                                prefabSet.Add(prefabPath);
                            }
                        }
                    }
                    prefabsWithMaterials = prefabSet.Count;

                    if (prefabsWithMaterials < _results.totalPrefabs) {
                        EditorGUILayout.HelpBox($"Displaying {prefabsWithMaterials} of {_results.totalPrefabs} prefabs. Prefabs without Renderer, SkinnedMeshRenderer, or DrakeMeshRenderer components are not shown.", MessageType.Info);
                    }
                }

                // === Pagination Controls
                if (_totalPages > 1) {
                    EditorGUILayout.BeginHorizontal();
                    var itemType = groupByMaterial ? "materials" : "prefabs";
                    EditorGUILayout.LabelField($"Displaying {_filteredItemCount} {itemType}", GUILayout.Width(180));

                    GUI.enabled = currentPage > 0;
                    if (GUILayout.Button("◄ First", GUILayout.Width(60))) {
                        currentPage = 0;
                        _scrollPosition = Vector2.zero;
                    }
                    if (GUILayout.Button("◄ Prev", GUILayout.Width(60))) {
                        currentPage--;
                        _scrollPosition = Vector2.zero;
                    }
                    GUI.enabled = true;

                    EditorGUILayout.LabelField($"Page {currentPage + 1} / {_totalPages}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(100));

                    GUI.enabled = currentPage < _totalPages - 1;
                    if (GUILayout.Button("Next ►", GUILayout.Width(60))) {
                        currentPage++;
                        _scrollPosition = Vector2.zero;
                    }
                    if (GUILayout.Button("Last ►", GUILayout.Width(60))) {
                        currentPage = _totalPages - 1;
                        _scrollPosition = Vector2.zero;
                    }
                    GUI.enabled = true;

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(5);
                }

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                DrawResults();
                EditorGUILayout.EndScrollView();
            }
        }

        void AnalyzePrefabs() {
            _isAnalyzing = true;
            _results = new AnalysisResults();

            try {
                var searchPattern = includeSubfolders ? "**/*.prefab" : "*.prefab";
                var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { selectedFolderPath });

                var prefabsToAnalyze = new List<string>();
                foreach (var guid in prefabGuids) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!includeSubfolders && Path.GetDirectoryName(path) != selectedFolderPath.TrimEnd('/')) {
                        continue;
                    }
                    prefabsToAnalyze.Add(path);
                }

                var progress = 0;
                foreach (var prefabPath in prefabsToAnalyze) {
                    progress++;
                    if (EditorUtility.DisplayCancelableProgressBar("Analyzing Prefabs", $"Processing {Path.GetFileName(prefabPath)} ({progress}/{prefabsToAnalyze.Count})", (float)progress / prefabsToAnalyze.Count)) {
                        break;
                    }

                    AnalyzePrefab(prefabPath);
                }

                _results.totalPrefabs = prefabsToAnalyze.Count;
                _results.totalMaterials = _results.materials.Count;
                _results.totalMeshes = _results.meshes.Count;

                var uniqueTextures = new HashSet<Texture>();
                foreach (var materialData in _results.materials.Values) {
                    foreach (var textureData in materialData.textures) {
                        if (textureData.texture != null) {
                            uniqueTextures.Add(textureData.texture);
                        }
                    }
                }
                _results.totalTextures = uniqueTextures.Count;

                BuildShaderList();
                CalculatePagination();

                EditorUtility.ClearProgressBar();
            } catch (Exception e) {
                Log.Critical?.Error($"Error analyzing prefabs: {e}");
                EditorUtility.ClearProgressBar();
            } finally {
                _isAnalyzing = false;
            }
        }

        void BuildShaderList() {
            var shaderSet = new HashSet<Shader>();
            foreach (var materialData in _results.materials.Values) {
                if (materialData.material != null && materialData.material.shader != null) {
                    shaderSet.Add(materialData.material.shader);
                }
            }

            var shaderList = new List<string> { "All Shaders" };
            var shaders = new List<Shader> { null };

            foreach (var shader in shaderSet) {
                shaderList.Add(shader.name);
                shaders.Add(shader);
            }

            _shaderNames = shaderList.ToArray();
            _results.shaders = shaders;
            selectedShaderIndex = 0;
            _selectedShader = null;
        }

        void UpdateShaderFilter() {
            if (selectedShaderIndex == 0) {
                _selectedShader = null;
            } else if (selectedShaderIndex > 0 && selectedShaderIndex <= _results.shaders.Count) {
                _selectedShader = _results.shaders[selectedShaderIndex];
            }
            currentPage = 0;
            CalculatePagination();
        }

        void CalculatePagination() {
            _filteredItemCount = 0;

            if (groupByMaterial) {
                foreach (var materialData in _results.materials.Values) {
                    if (PassesShaderFilter(materialData)) {
                        _filteredItemCount++;
                    }
                }
            } else {
                var prefabToMaterials = new Dictionary<string, List<MaterialData>>();
                foreach (var materialData in _results.materials.Values) {
                    if (!PassesShaderFilter(materialData)) {
                        continue;
                    }
                    foreach (var prefabPath in materialData.prefabs) {
                        if (!prefabToMaterials.ContainsKey(prefabPath)) {
                            prefabToMaterials[prefabPath] = new List<MaterialData>();
                        }
                    }
                }
                _filteredItemCount = prefabToMaterials.Count;
            }

            _totalPages = Mathf.Max(1, Mathf.CeilToInt((float)_filteredItemCount / ItemsPerPage));
            currentPage = Mathf.Clamp(currentPage, 0, _totalPages - 1);
        }

        bool PassesShaderFilter(MaterialData materialData) {
            if (_selectedShader == null) {
                return true;
            }
            return materialData.material != null && materialData.material.shader == _selectedShader;
        }

        void AnalyzePrefab(string prefabPath) {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) {
                return;
            }

            // Analyze standard Unity renderers
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers) {
                AnalyzeRenderer(renderer, prefabPath);
            }

            // Analyze DrakeRenderer components
            var drakeRenderers = prefab.GetComponentsInChildren<DrakeMeshRenderer>(true);
            foreach (var drakeRenderer in drakeRenderers) {
                AnalyzeDrakeRenderer(drakeRenderer, prefabPath);
            }
        }

        void AnalyzeRenderer(Renderer renderer, string prefabPath) {
            // Analyze mesh
            Mesh mesh = null;
            if (renderer is MeshRenderer meshRenderer) {
                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null) {
                    mesh = meshFilter.sharedMesh;
                }
            } else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                mesh = skinnedMeshRenderer.sharedMesh;
            }

            if (mesh != null) {
                RecordMesh(mesh, prefabPath);
            }

            // Analyze materials
            var materials = renderer.sharedMaterials;
            foreach (var material in materials) {
                if (material == null) {
                    continue;
                }
                RecordMaterial(material, mesh, prefabPath);
            }
        }

        void AnalyzeDrakeRenderer(DrakeMeshRenderer drakeRenderer, string prefabPath) {
            // Try to load mesh reference
            Mesh mesh = null;
            var meshRef = drakeRenderer.MeshReference;
            if (meshRef != null && meshRef.RuntimeKeyIsValid()) {
                var meshPath = AssetDatabase.GUIDToAssetPath(meshRef.AssetGUID);
                if (!string.IsNullOrEmpty(meshPath)) {
                    mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (mesh != null) {
                        RecordMesh(mesh, prefabPath);
                    }
                }
            }

            // Analyze DrakeRenderer materials
            var materialRefs = drakeRenderer.MaterialReferences;
            if (materialRefs != null) {
                foreach (var materialRef in materialRefs) {
                    if (materialRef == null || !materialRef.RuntimeKeyIsValid()) {
                        continue;
                    }

                    var materialPath = AssetDatabase.GUIDToAssetPath(materialRef.AssetGUID);
                    if (!string.IsNullOrEmpty(materialPath)) {
                        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material != null) {
                            RecordMaterial(material, mesh, prefabPath);
                        }
                    }
                }
            }
        }

        void RecordMesh(Mesh mesh, string prefabPath) {
            if (!_results.meshes.TryGetValue(mesh, out var meshData)) {
                meshData = new MeshData {
                    mesh = mesh,
                    meshPath = AssetDatabase.GetAssetPath(mesh),
                    vertexCount = mesh.vertexCount,
                    triangleCount = mesh.triangles.Length / 3,
                    subMeshCount = mesh.subMeshCount,
                    prefabs = new List<string>()
                };
                _results.meshes[mesh] = meshData;
            }

            if (!meshData.prefabs.Contains(prefabPath)) {
                meshData.prefabs.Add(prefabPath);
            }
        }

        void RecordMaterial(Material material, Mesh mesh, string prefabPath) {
            if (!_results.materials.TryGetValue(material, out var materialData)) {
                materialData = new MaterialData {
                    material = material,
                    textures = new List<TextureData>(),
                    prefabs = new List<string>(),
                    meshes = new List<Mesh>()
                };
                _results.materials[material] = materialData;

                AnalyzeMaterial(material, materialData);
            }

            if (!materialData.prefabs.Contains(prefabPath)) {
                materialData.prefabs.Add(prefabPath);
            }

            if (mesh != null && !materialData.meshes.Contains(mesh)) {
                materialData.meshes.Add(mesh);
            }
        }

        void AnalyzeMaterial(Material material, MaterialData materialData) {
            var shader = material.shader;
            if (shader == null) {
                return;
            }

            var propertyCount = shader.GetPropertyCount();
            for (int i = 0; i < propertyCount; i++) {
                if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture) {
                    var propertyName = shader.GetPropertyName(i);
                    var texture = material.GetTexture(propertyName);

                    if (texture != null) {
                        var textureData = new TextureData {
                            propertyName = propertyName,
                            texture = texture,
                            texturePath = AssetDatabase.GetAssetPath(texture),
                            resolution = $"{texture.width}x{texture.height}",
                            format = GetTextureFormat(texture)
                        };
                        materialData.textures.Add(textureData);
                    }
                }
            }
        }

        string GetTextureFormat(Texture texture) {
            if (texture is Texture2D texture2D) {
                return texture2D.format.ToString();
            }
            return "Unknown";
        }

        void DrawResults() {
            if (groupByMaterial) {
                DrawGroupedByMaterial();
            } else {
                DrawGroupedByPrefab();
            }
        }

        void DrawGroupedByMaterial() {
            var startIndex = currentPage * ItemsPerPage;
            var endIndex = Mathf.Min(startIndex + ItemsPerPage, _filteredItemCount);
            var currentIndex = 0;

            foreach (var materialData in _results.materials.Values) {
                if (!PassesShaderFilter(materialData)) {
                    continue;
                }

                if (currentIndex >= startIndex && currentIndex < endIndex) {
                    DrawMaterialEntry(materialData);
                }

                currentIndex++;
                if (currentIndex >= endIndex) {
                    break;
                }
            }
        }

        void DrawMaterialEntry(MaterialData materialData) {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Material header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField("Material", materialData.material, typeof(Material), false);
            var shaderName = materialData.material?.shader != null ? materialData.material.shader.name : "None";
            EditorGUILayout.LabelField($"Shader: {shaderName}", GUILayout.Width(250));
            EditorGUILayout.LabelField($"Used in {materialData.prefabs.Count} prefabs", GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;

            // Meshes using this material
            if (materialData.meshes.Count > 0) {
                EditorGUILayout.LabelField($"Meshes ({materialData.meshes.Count}):", EditorStyles.miniBoldLabel);
                foreach (var mesh in materialData.meshes) {
                    if (_results.meshes.TryGetValue(mesh, out var meshData)) {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(mesh, typeof(Mesh), false, GUILayout.Width(200));
                        EditorGUILayout.LabelField($"Verts: {meshData.vertexCount}", GUILayout.Width(100));
                        EditorGUILayout.LabelField($"Tris: {meshData.triangleCount}", GUILayout.Width(100));
                        EditorGUILayout.LabelField($"SubMeshes: {meshData.subMeshCount}", GUILayout.Width(100));
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.Space(3);
            }

            // Textures
            if (materialData.textures.Count > 0) {
                EditorGUILayout.LabelField($"Textures ({materialData.textures.Count}):", EditorStyles.miniBoldLabel);

                foreach (var textureData in materialData.textures) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(textureData.propertyName, GUILayout.Width(150));
                    EditorGUILayout.ObjectField(textureData.texture, typeof(Texture), false, GUILayout.Width(200));
                    EditorGUILayout.LabelField(textureData.resolution, GUILayout.Width(100));
                    EditorGUILayout.LabelField(textureData.format, GUILayout.Width(150));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.Space(3);
            }

            // Prefabs using this material
            EditorGUILayout.LabelField($"Prefabs ({materialData.prefabs.Count}):", EditorStyles.miniBoldLabel);
            foreach (var prefabPath in materialData.prefabs) {
                EditorGUILayout.BeginHorizontal();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        void DrawGroupedByPrefab() {
            var prefabToMaterials = new Dictionary<string, List<MaterialData>>();
            var prefabToMeshes = new Dictionary<string, List<MeshData>>();

            foreach (var materialData in _results.materials.Values) {
                if (!PassesShaderFilter(materialData)) {
                    continue;
                }

                foreach (var prefabPath in materialData.prefabs) {
                    if (!prefabToMaterials.TryGetValue(prefabPath, out var materials)) {
                        materials = new List<MaterialData>();
                        prefabToMaterials[prefabPath] = materials;
                    }
                    materials.Add(materialData);
                }
            }

            foreach (var meshData in _results.meshes.Values) {
                foreach (var prefabPath in meshData.prefabs) {
                    if (!prefabToMeshes.TryGetValue(prefabPath, out var meshes)) {
                        meshes = new List<MeshData>();
                        prefabToMeshes[prefabPath] = meshes;
                    }
                    meshes.Add(meshData);
                }
            }

            var startIndex = currentPage * ItemsPerPage;
            var endIndex = Mathf.Min(startIndex + ItemsPerPage, prefabToMaterials.Count);
            var currentIndex = 0;

            foreach (var kvp in prefabToMaterials) {
                if (currentIndex >= startIndex && currentIndex < endIndex) {
                    DrawPrefabEntry(kvp.Key, kvp.Value, prefabToMeshes);
                }

                currentIndex++;
                if (currentIndex >= endIndex) {
                    break;
                }
            }
        }

        void DrawPrefabEntry(string prefabPath, List<MaterialData> materials, Dictionary<string, List<MeshData>> prefabToMeshes) {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Prefab header
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

            EditorGUI.indentLevel++;

            // Meshes
            if (prefabToMeshes.TryGetValue(prefabPath, out var meshes) && meshes.Count > 0) {
                EditorGUILayout.LabelField($"Meshes ({meshes.Count}):", EditorStyles.miniBoldLabel);
                foreach (var meshData in meshes) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(meshData.mesh, typeof(Mesh), false, GUILayout.Width(200));
                    EditorGUILayout.LabelField($"Verts: {meshData.vertexCount}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Tris: {meshData.triangleCount}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"SubMeshes: {meshData.subMeshCount}", GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.Space(3);
            }

            // Materials
            EditorGUILayout.LabelField($"Materials ({materials.Count}):", EditorStyles.miniBoldLabel);

            foreach (var materialData in materials) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(materialData.material, typeof(Material), false);
                var shaderName = materialData.material?.shader != null ? materialData.material.shader.name : "None";
                EditorGUILayout.LabelField($"Shader: {shaderName}", GUILayout.Width(200));
                EditorGUILayout.LabelField($"{materialData.textures.Count} textures", GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                if (materialData.textures.Count > 0) {
                    EditorGUI.indentLevel++;
                    foreach (var textureData in materialData.textures) {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(textureData.propertyName, GUILayout.Width(130));
                        EditorGUILayout.ObjectField(textureData.texture, typeof(Texture), false, GUILayout.Width(180));
                        EditorGUILayout.LabelField(textureData.resolution, GUILayout.Width(90));
                        EditorGUILayout.LabelField(textureData.format, GUILayout.Width(130));
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        void ExportToCSV() {
            var path = EditorUtility.SaveFilePanel("Export Material Analysis", "", "material_analysis.csv", "csv");
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            try {
                var csv = new StringBuilder();
                csv.AppendLine("Prefab,Mesh,Mesh Path,Vertex Count,Triangle Count,SubMesh Count,Material,Material Path,Shader,Texture Property,Texture,Texture Path,Resolution,Format");

                foreach (var materialData in _results.materials.Values) {
                    if (!PassesShaderFilter(materialData)) {
                        continue;
                    }

                    var materialPath = AssetDatabase.GetAssetPath(materialData.material);
                    var shaderName = materialData.material?.shader != null ? materialData.material.shader.name : "None";

                    foreach (var prefabPath in materialData.prefabs) {
                        var prefabName = Path.GetFileNameWithoutExtension(prefabPath);

                        // Get meshes for this prefab and material combination
                        var meshesForPrefab = new List<MeshData>();
                        foreach (var mesh in materialData.meshes) {
                            if (_results.meshes.TryGetValue(mesh, out var meshData) && meshData.prefabs.Contains(prefabPath)) {
                                meshesForPrefab.Add(meshData);
                            }
                        }

                        if (meshesForPrefab.Count == 0) {
                            // No mesh data
                            if (materialData.textures.Count == 0) {
                                csv.AppendLine($"{prefabName},,,,,{materialData.material.name},{materialPath},{shaderName},,,,");
                            } else {
                                foreach (var textureData in materialData.textures) {
                                    csv.AppendLine($"{prefabName},,,,,{materialData.material.name},{materialPath},{shaderName},{textureData.propertyName},{textureData.texture.name},{textureData.texturePath},{textureData.resolution},{textureData.format}");
                                }
                            }
                        } else {
                            foreach (var meshData in meshesForPrefab) {
                                if (materialData.textures.Count == 0) {
                                    csv.AppendLine($"{prefabName},{meshData.mesh.name},{meshData.meshPath},{meshData.vertexCount},{meshData.triangleCount},{meshData.subMeshCount},{materialData.material.name},{materialPath},{shaderName},,,,");
                                } else {
                                    foreach (var textureData in materialData.textures) {
                                        csv.AppendLine($"{prefabName},{meshData.mesh.name},{meshData.meshPath},{meshData.vertexCount},{meshData.triangleCount},{meshData.subMeshCount},{materialData.material.name},{materialPath},{shaderName},{textureData.propertyName},{textureData.texture.name},{textureData.texturePath},{textureData.resolution},{textureData.format}");
                                    }
                                }
                            }
                        }
                    }
                }

                File.WriteAllText(path, csv.ToString());
                Log.Important?.Info($"Exported analysis to: {path}");
                EditorUtility.DisplayDialog("Export Complete", $"Analysis exported to:\n{path}", "OK");
            } catch (Exception e) {
                Log.Critical?.Error($"Error exporting CSV: {e}");
                EditorUtility.DisplayDialog("Export Failed", $"Failed to export: {e.Message}", "OK");
            }
        }

        void ExportSummary() {
            var path = EditorUtility.SaveFilePanel("Export Summary", "", "material_analysis_summary.txt", "txt");
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            try {
                var summary = new StringBuilder();
                summary.AppendLine("=== Prefab Material & Mesh Analysis Summary ===");
                summary.AppendLine($"Folder: {selectedFolderPath}");
                summary.AppendLine($"Include Subfolders: {includeSubfolders}");
                summary.AppendLine($"Analysis Date: {DateTime.Now}");
                summary.AppendLine();
                summary.AppendLine($"Total Prefabs: {_results.totalPrefabs}");
                summary.AppendLine($"Total Unique Meshes: {_results.totalMeshes}");
                summary.AppendLine($"Total Unique Materials: {_results.totalMaterials}");
                summary.AppendLine($"Total Unique Textures: {_results.totalTextures}");
                summary.AppendLine();

                // Calculate totals
                int totalVertices = 0;
                int totalTriangles = 0;
                foreach (var meshData in _results.meshes.Values) {
                    totalVertices += meshData.vertexCount;
                    totalTriangles += meshData.triangleCount;
                }
                summary.AppendLine($"Total Vertices: {totalVertices:N0}");
                summary.AppendLine($"Total Triangles: {totalTriangles:N0}");
                summary.AppendLine();

                summary.AppendLine("=== Meshes ===");
                foreach (var meshData in _results.meshes.Values) {
                    summary.AppendLine();
                    summary.AppendLine($"Mesh: {meshData.mesh.name}");
                    summary.AppendLine($"  Path: {meshData.meshPath}");
                    summary.AppendLine($"  Vertices: {meshData.vertexCount:N0}");
                    summary.AppendLine($"  Triangles: {meshData.triangleCount:N0}");
                    summary.AppendLine($"  SubMeshes: {meshData.subMeshCount}");
                    summary.AppendLine($"  Used in {meshData.prefabs.Count} prefabs");
                }

                summary.AppendLine();
                summary.AppendLine("=== Shaders ===");
                var shaderGroups = new Dictionary<Shader, List<MaterialData>>();
                foreach (var materialData in _results.materials.Values) {
                    if (!PassesShaderFilter(materialData)) {
                        continue;
                    }
                    var shader = materialData.material?.shader;
                    if (shader != null) {
                        if (!shaderGroups.TryGetValue(shader, out var materials)) {
                            materials = new List<MaterialData>();
                            shaderGroups[shader] = materials;
                        }
                        materials.Add(materialData);
                    }
                }

                foreach (var kvp in shaderGroups) {
                    summary.AppendLine();
                    summary.AppendLine($"Shader: {kvp.Key.name}");
                    summary.AppendLine($"  Materials using this shader: {kvp.Value.Count}");
                }

                summary.AppendLine();
                summary.AppendLine("=== Materials ===");

                foreach (var materialData in _results.materials.Values) {
                    if (!PassesShaderFilter(materialData)) {
                        continue;
                    }

                    summary.AppendLine();
                    summary.AppendLine($"Material: {materialData.material.name}");
                    summary.AppendLine($"  Path: {AssetDatabase.GetAssetPath(materialData.material)}");
                    var shaderName = materialData.material?.shader != null ? materialData.material.shader.name : "None";
                    summary.AppendLine($"  Shader: {shaderName}");
                    summary.AppendLine($"  Used in {materialData.prefabs.Count} prefabs");
                    summary.AppendLine($"  Used on {materialData.meshes.Count} meshes");
                    summary.AppendLine($"  Textures: {materialData.textures.Count}");

                    foreach (var textureData in materialData.textures) {
                        summary.AppendLine($"    - {textureData.propertyName}: {textureData.texture.name} ({textureData.resolution}, {textureData.format})");
                    }
                }

                File.WriteAllText(path, summary.ToString());
                Log.Important?.Info($"Exported summary to: {path}");
                EditorUtility.DisplayDialog("Export Complete", $"Summary exported to:\n{path}", "OK");
            } catch (Exception e) {
                Log.Critical?.Error($"Error exporting summary: {e}");
                EditorUtility.DisplayDialog("Export Failed", $"Failed to export: {e.Message}", "OK");
            }
        }

        class AnalysisResults {
            public Dictionary<Material, MaterialData> materials = new();
            public Dictionary<Mesh, MeshData> meshes = new();
            public List<Shader> shaders = new();
            public int totalPrefabs;
            public int totalMeshes;
            public int totalMaterials;
            public int totalTextures;
        }

        class MaterialData {
            public Material material;
            public List<TextureData> textures;
            public List<string> prefabs;
            public List<Mesh> meshes;
        }

        class MeshData {
            public Mesh mesh;
            public string meshPath;
            public int vertexCount;
            public int triangleCount;
            public int subMeshCount;
            public List<string> prefabs;
        }

        class TextureData {
            public string propertyName;
            public Texture texture;
            public string texturePath;
            public string resolution;
            public string format;
        }
    }
}
