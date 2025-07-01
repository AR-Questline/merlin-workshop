using System;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Collections;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;
using static Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes.SubdividedScene;

namespace Awaken.TG.Editor.Main.Scenes.SubdividedScenes {
    [CustomPropertyDrawer(typeof(SerializedSubscenesData))]
    public class SerializedSubscenesDataDrawer : PropertyDrawer {
        const float NodeFieldHeight = 18f;
        const float SceneFieldHeight = 18f;
        const float SearchFieldHeight = 18f;
        const float ToggleWidth = 20f;
        const float LoadToggleLabelWidth = 40f;
        const string NameNodes = nameof(SerializedSubscenesData.Nodes);
        const string NameScenes = nameof(SerializedSubscenesData.Scenes);
        const string NameFirstChildNodeIndex = nameof(SerializedSubscenesData.NodeData.firstChildNodeIndex);
        const string NameChildNodesCount = nameof(SerializedSubscenesData.NodeData.childNodesCount);
        const string NameFirstChildSceneIndex = nameof(SerializedSubscenesData.NodeData.firstChildSceneIndex);
        const string NameChildScenesCount = nameof(SerializedSubscenesData.NodeData.childScenesCount);
        const string NameName = nameof(SerializedSubscenesData.NodeData.name);
        const string NameNodeId = nameof(SerializedSubscenesData.NodeData.id);
        const string NameSceneId = nameof(SubsceneData.id);
        const string NameNodeStableUniqueId = nameof(SerializedSubscenesData.NodeData.stableUniqueId);
        const string NameNodesUniqueIdCounter = nameof(SerializedSubscenesData.NodesUniqueIdCounter);
        const string NameScenesUniqueIdCounter = nameof(SerializedSubscenesData.ScenesUniqueIdCounter);
        const string NameSceneStableUniqueId = nameof(SubsceneData.stableUniqueId);
        const string NameSceneReference = nameof(SubsceneData.reference);

        const string EditorPrefNameDoNotLoadSubscene = SerializedSubscenesData.EditorPrefNameDoNotLoadScene;
        const string EditorPrefNameDoNotLoadNode = SerializedSubscenesData.EditorPrefNameDoNotLoadNode;
        static readonly Color HoverColor = new(0.3f, 0.3f, 0.3f);
        static readonly OnDemandCache<uint, string> searchByProperty = new(static _ => string.Empty);
        static readonly OnDemandCache<uint, string> editedPropertyPathByProperty = new(static _ => string.Empty);

        public static readonly ARAssetReferenceSettingsAttribute SceneReferenceSettings = new(
            new[] { typeof(SceneAsset) }, true, AddressableGroup.Scenes, labels: new[] { SceneService.ScenesLabel }
        );
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label) {
            DrawGUI(rect, property);
        }

        public static void DrawGUI(Rect rect, SerializedProperty property) {
            SerializedProperty nodes = property.FindPropertyRelative(NameNodes);
            if (nodes.arraySize == 0) {
                return;
            }

            SerializedProperty scenes = property.FindPropertyRelative(NameScenes);
            float yOffset = 0;
            var inputContext = new InputContext(Event.current);
            var propertyContentHash = property.contentHash;
            var search = DrawSearchField(rect, ref yOffset, propertyContentHash);
            DrawNodes(0, rect, ref yOffset, nodes, scenes, in inputContext, search, property, propertyContentHash);
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            var contentHash = property.contentHash;
            var search = searchByProperty[contentHash];
            var nodes = property.FindPropertyRelative(NameNodes);
            var scenes = property.FindPropertyRelative(NameScenes);
            return SearchFieldHeight + GetNodesAndScenesHeight(0, nodes, scenes, search, contentHash);
        }

        static string DrawSearchField(Rect rect, ref float yOffset, uint serializedPropertyContentHash) {
            rect.y += yOffset;
            rect.height = SearchFieldHeight;
            yOffset += SearchFieldHeight;
            var search = searchByProperty[serializedPropertyContentHash];
            search = EditorGUI.TextField(rect, search, EditorStyles.toolbarSearchField);
            searchByProperty[serializedPropertyContentHash] = search;
            return search;
        }
        
        static void DrawNodes(int nodeIndex, Rect rect, ref float yOffset, SerializedProperty nodes,
            SerializedProperty scenes, in InputContext inputContext, string search, SerializedProperty serializedProperty, 
            uint serializedPropertyContentHash) {
            var node = nodes.GetArrayElementAtIndex(nodeIndex);
            var nodeNameValue = node.FindPropertyRelative(NameName).stringValue;
            bool hasSearch = string.IsNullOrEmpty(search) == false;
            bool drawNode = hasSearch == false || IsInSearch(search, nodeNameValue);
            if (drawNode) {
                var nodeRect = new Rect(rect.x, rect.y + yOffset, rect.width,
                    NodeFieldHeight);
                DrawNodeContextMenu(nodeRect, node, in inputContext, serializedProperty);
                DrawNodeField(nodeRect, node, nodeNameValue, serializedPropertyContentHash);
                yOffset += NodeFieldHeight;
            }
            if (node.isExpanded || hasSearch) {
                rect = IndentedRect(rect);
                var firstChildNodeIndex = node.FindPropertyRelative(NameFirstChildNodeIndex).intValue;
                var childNodesCount = node.FindPropertyRelative(NameChildNodesCount).intValue;

                var firstChildSceneIndex = node.FindPropertyRelative(NameFirstChildSceneIndex).intValue;
                int childScenesCount = node.FindPropertyRelative(NameChildScenesCount).intValue;

                DrawScenes(firstChildSceneIndex, childScenesCount, rect, ref yOffset, scenes, 
                    in inputContext, search, serializedProperty, serializedPropertyContentHash);
                for (int i = 0; i < childNodesCount; i++) {
                    DrawNodes(firstChildNodeIndex + i, rect, ref yOffset, nodes, scenes, 
                        in inputContext, search, serializedProperty, serializedPropertyContentHash);
                }
            }
        }
        
        static void DrawScenes(int startIndex, int count, Rect rect, ref float yOffset,
            SerializedProperty scenes, in InputContext inputContext, string search, SerializedProperty serializedProperty,
            uint serializedPropertyContentHash) {
            int endIndex = startIndex + count;
            rect.y += yOffset;
            for (int i = startIndex; i < endIndex; i++) {
                SerializedProperty scene = scenes.GetArrayElementAtIndex(i);
                if (TryGetSceneValuesIfNeedToDraw(scene, search,
                        out SceneReference sceneReference,
                        out bool isSet, out string sceneName) == false) {
                    continue;
                }
                var sceneFieldHeight = IsCurrentlyEdited(scene.propertyPath, serializedPropertyContentHash) ? SceneFieldHeight * 3 : SceneFieldHeight;
                rect.height = sceneFieldHeight;
                DrawSceneContextMenu(rect, scene, sceneReference, in inputContext, serializedProperty);
                DrawSceneField(rect, scene, sceneName, isSet, sceneReference, serializedPropertyContentHash, serializedProperty);
                rect.y += sceneFieldHeight;
                yOffset += sceneFieldHeight;
            }
        }

        static bool TryGetSceneValuesIfNeedToDraw(SerializedProperty scene, string search,
            out SceneReference sceneReference, out bool isSet, out string sceneName) {
            var friendlyNameValue = scene.FindPropertyRelative(nameof(SubsceneData.nameOverride)).stringValue;
            sceneReference = (SceneReference)scene.FindPropertyRelative(nameof(SubsceneData.reference)).boxedValue;
            isSet = sceneReference.IsSet;
            if (isSet) {
                sceneName = string.IsNullOrEmpty(friendlyNameValue) ? sceneReference.Name : friendlyNameValue;
            } else {
                sceneName = "Missing scene";
            }
            return string.IsNullOrEmpty(search) || IsInSearch(search, sceneName);
        }

        static void DrawNodeField(Rect rect, SerializedProperty node, string nodeNameValue, uint serializedPropertyContentHash) {
            var rects = new PropertyDrawerRects(rect);
            var nodeRect = rects.AllocateWithRest(ToggleWidth + LoadToggleLabelWidth);
            var loadLabelRect = rects.AllocateLeft(LoadToggleLabelWidth);
            var toggleRect = rects.Rect;
            if (IsCurrentlyEdited(node.propertyPath, serializedPropertyContentHash)) {
                var nodeName = node.FindPropertyRelative(NameName);
                EditorGUI.PropertyField(nodeRect, nodeName, GUIContent.none);
            } else {
                node.isExpanded = EditorGUI.Foldout(nodeRect, node.isExpanded,
                    nodeNameValue, true);
            }
            EditorGUI.LabelField(loadLabelRect, "Load:");
            var nodeStableUniqueId = node.FindPropertyRelative(NameNodeStableUniqueId).intValue;
            var isNodeNeededToLoad = IsNodeNeededToLoad(nodeStableUniqueId); 
            var newIsNodeNeededToLoad =  EditorGUI.Toggle(toggleRect, isNodeNeededToLoad);
            if (isNodeNeededToLoad != newIsNodeNeededToLoad) {
                MarkNodeToLoad(nodeStableUniqueId, newIsNodeNeededToLoad);
            }
           
        }
        
        static void DrawSceneField(Rect position, SerializedProperty scene, string sceneName, bool isSet, SceneReference sceneReference, 
             uint serializedPropertyContentHash, SerializedProperty serializedProperty) {
            var rects = new PropertyDrawerRects(position);
            if (IsCurrentlyEdited(scene.propertyPath, serializedPropertyContentHash)) {
                var friendlyName = scene.FindPropertyRelative(nameof(SubsceneData.nameOverride));
                EditorGUI.PropertyField(rects.AllocateTop(SceneFieldHeight), friendlyName);
                ARAssetReferencePropertyDrawer.Draw(
                    rects.AllocateTop(SceneFieldHeight * 2),
                    scene.FindPropertyRelative(NameSceneReference).FindPropertyRelative("reference"),
                    SceneReferenceSettings
                );
            } else {
                if (isSet == false) GUI.enabled = false;
                bool isSceneLoaded = sceneReference.LoadedScene.IsValid();
                var newIsSceneLoaded = EditorGUI.Toggle(rects.AllocateLeft(ToggleWidth), isSceneLoaded);
                
                GUI.enabled = true;
                
                EditorGUI.LabelField(rects.AllocateWithRest(ToggleWidth + LoadToggleLabelWidth), sceneName);
                
                if (isSet == false) GUI.enabled = false;
                
                EditorGUI.LabelField(rects.AllocateLeft(LoadToggleLabelWidth), "Load:");
                
                var sceneStableUniqueId = scene.FindPropertyRelative(NameSceneStableUniqueId).intValue;
                var sceneIndex = SerializedSubscenesData.GetIndexFromId(scene.FindPropertyRelative(NameSceneId).intValue);
                var isSceneNeededToLoad = IsSceneNeededToLoad(sceneStableUniqueId);
                if (isSceneNeededToLoad && isSet == false) {
                    MarkSceneWithNodesToLoad(sceneIndex, false, serializedProperty);
                    isSceneNeededToLoad = false;
                }
                
                var newIsSceneNeededToLoad = EditorGUI.Toggle(rects.Rect, isSceneNeededToLoad);
                
                GUI.enabled = true;
                if (isSceneNeededToLoad != newIsSceneNeededToLoad) {
                    MarkSceneWithNodesToLoad(sceneIndex, newIsSceneNeededToLoad, serializedProperty);
                }
                if (isSceneLoaded != newIsSceneLoaded) {
                    if (newIsSceneLoaded) {
                        LoadScene(sceneReference);
                    } else {
                        UnloadScene(sceneReference, true, true);
                    }
                }
            }
            
        }

        static void DrawNodeContextMenu(Rect rect, SerializedProperty node, in InputContext inputContext,
            SerializedProperty serializedProperty) {
            if (rect.Contains(inputContext.mousePosition) == false) {
                return;
            }

            EditorGUI.DrawRect(rect, HoverColor);
            if (inputContext.contextClick == false) {
                return;
            }
            if (IsCurrentlyEdited(node.propertyPath, serializedProperty.contentHash)) {
                StopEditing(serializedProperty);
                return;
            }
            var menu = new GenericMenu();
            var nodeIndex = SerializedSubscenesData.GetIndexFromId(node.FindPropertyRelative(NameNodeId).intValue);
            menu.AddItem(new GUIContent("Load all"), false, () => LoadAllChildScenes(nodeIndex, serializedProperty));
            menu.AddItem(new GUIContent("Unload all"), false, () => {
                UnloadAllChildScenes(nodeIndex, true, serializedProperty);
                CleanupUnity();
            });
            menu.AddItem(new GUIContent("Unload all no save"), false, () => UnloadAllChildScenes(nodeIndex, false, serializedProperty));
            menu.AddItem(new GUIContent("Mark loadable below"), false, () => MarkNodeToLoad(nodeIndex, true, serializedProperty));
            menu.AddItem(new GUIContent("Un-mark loadable below"), false, () => MarkNodeToLoad(nodeIndex, false, serializedProperty));

            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Add Folder"), false, () => {
                int newNodeIndex = AddChildNode(nodeIndex, serializedProperty);
                StartEditing(serializedProperty.FindPropertyRelative(NameNodes).GetArrayElementAtIndex(newNodeIndex), serializedProperty);
            });
            menu.AddItem(new GUIContent("Add Scene"), false, () => OpenAddSceneWindow(serializedProperty, nodeIndex));
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Edit"), false, () => StartEditing(node, serializedProperty));
            menu.AddItem(new GUIContent("Expand all"), false, () => SetChildNodesExpanded(nodeIndex, true, serializedProperty));
            menu.AddItem(new GUIContent("Collapse all"), false, () => SetChildNodesExpanded(nodeIndex, false, serializedProperty));
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Move Up"), false, () => MoveNodeUp(nodeIndex, serializedProperty));
            menu.AddItem(new GUIContent("Move down"), false, () => MoveNodeDown(nodeIndex, serializedProperty));
            menu.AddItem(new GUIContent("Remove Folder"), false, () => RemoveNode(nodeIndex, serializedProperty));
            menu.ShowAsContext();
            inputContext.evt.Use();
        }

        static void StartEditing(SerializedProperty editedProperty, SerializedProperty serializedProperty) {
            editedPropertyPathByProperty[serializedProperty.contentHash] = editedProperty.propertyPath;
        }
        static void StopEditing(SerializedProperty serializedProperty) {
            editedPropertyPathByProperty[serializedProperty.contentHash] = string.Empty;
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        static void DrawSceneContextMenu(Rect rect, SerializedProperty scene, SceneReference sceneReference, in InputContext inputContext,
            SerializedProperty serializedProperty) {
            if (rect.Contains(inputContext.mousePosition) == false) {
                return;
            }

            EditorGUI.DrawRect(rect, HoverColor);
            if (inputContext.contextClick == false) {
                return;
            }

            if (IsCurrentlyEdited(scene.propertyPath, serializedProperty.contentHash)) {
                StopEditing(serializedProperty);
                return;
            }
            bool isSceneLoaded = sceneReference.LoadedScene.IsValid();
            bool isSet = sceneReference.IsSet;
            var menu = new GenericMenu();

            var sceneIndex = SerializedSubscenesData.GetIndexFromId(scene.FindPropertyRelative(NameSceneId).intValue);
            if (isSet) {
                if (isSceneLoaded) {
                    menu.AddItem(new GUIContent("Unload"), false, () => UnloadScene(scene, true, true));
                } else {
                    menu.AddItem(new GUIContent("Load"), false, () => LoadScene(scene));
                }
                if (sceneReference.TryGetSceneAssetGUID(out var guid)) {
                    menu.AddItem(new GUIContent("Ping"), false, () => EditorGUIUtility.PingObject(AddressableHelper.FindFirstEntry<SceneAsset>(guid)));
                }
                menu.AddItem(new GUIContent("Mark load this"), false, () => { MarkSceneWithNodesToLoad(sceneIndex, true, serializedProperty); });
                menu.AddItem(new GUIContent("Mark load just this"), false, () => {
                    MarkNodeToLoad(0,false, serializedProperty);
                    MarkSceneWithNodesToLoad(sceneIndex, true, serializedProperty);
                });
                menu.AddItem(new GUIContent("Mark unload this"), false, () => MarkSceneWithNodesToLoad(sceneIndex, false, serializedProperty));
                menu.AddSeparator("");
            }
            menu.AddItem(new GUIContent("Edit"), false, () => StartEditing(scene, serializedProperty));
            menu.AddItem(new GUIContent("Move up"), false, () => MoveSceneUp(sceneIndex, serializedProperty));
            menu.AddItem(new GUIContent("Move down"), false, () => MoveSceneDown(sceneIndex, serializedProperty));
            menu.AddItem(new GUIContent("Remove"), false, () => RemoveScene(sceneIndex, serializedProperty));

            menu.ShowAsContext();
            inputContext.evt.Use();
        }

        static bool IsInSearch(string search, string name) {
            return name.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }
        
        static float GetNodesAndScenesHeight(int nodeIndex, SerializedProperty nodes, SerializedProperty scenes, string search, 
            uint serializedPropertyContentHash) {
            bool hasSearch = string.IsNullOrEmpty(search) == false;
            var node = nodes.GetArrayElementAtIndex(nodeIndex);
            string nodeNameValue = node.FindPropertyRelative(NameName).stringValue;
            bool drawNode = hasSearch == false || IsInSearch(search, nodeNameValue);
            float height = 0;
            if (drawNode) {
                height += NodeFieldHeight;
            }
            if (node.isExpanded || hasSearch) {
                var childNodesCount = node.FindPropertyRelative(NameChildNodesCount).intValue;
                var firstChildNodeIndex = node.FindPropertyRelative(NameFirstChildNodeIndex).intValue;
                for (int i = 0; i < childNodesCount; i++) {
                    height += GetNodesAndScenesHeight(firstChildNodeIndex + i, nodes, scenes, search, serializedPropertyContentHash);
                }

                int childScenesCount = node.FindPropertyRelative(NameChildScenesCount).intValue;
                int firstChildSceneIndex = node.FindPropertyRelative(NameFirstChildSceneIndex).intValue;
                for (int i = 0; i < childScenesCount; i++) {
                    var scene = scenes.GetArrayElementAtIndex(firstChildSceneIndex + i);
                    height += GetSceneHeight(scene, search, serializedPropertyContentHash);
                }
            }

            return height;
        }

        static float GetSceneHeight(SerializedProperty scene, string search, uint serializedPropertyContentHash) {
            if (TryGetSceneValuesIfNeedToDraw(scene, search,
                    out SceneReference _, out bool _, out string _) == false) {
                return 0;
            }
            return IsCurrentlyEdited(scene.propertyPath, serializedPropertyContentHash) ? (SceneFieldHeight * 3) : SceneFieldHeight;
        }

        /// ---  NodesAndScenesHierarchyChanging
        public static void AddScene(short nodeIndex, string friendlyName, string scenePath, SerializedProperty serializedProperty) {
            var node = serializedProperty.FindPropertyRelative(NameNodes).GetArrayElementAtIndex(nodeIndex);
            var scenes = serializedProperty.FindPropertyRelative(NameScenes);
            var nodeChildScenesCount = node.FindPropertyRelative(NameChildScenesCount);
            var nodeFirstChildSceneIndex = node.FindPropertyRelative(NameFirstChildSceneIndex);
            var nodeFirstChildSceneIndexValue = (short)nodeFirstChildSceneIndex.intValue;
            var scenesCount = (short)scenes.arraySize;
            if (nodeChildScenesCount.intValue == 0) {
                nodeFirstChildSceneIndexValue = scenesCount;
            }

            var insertIndex = (short)(nodeFirstChildSceneIndexValue + (short)nodeChildScenesCount.intValue);
            if (insertIndex >= scenes.arraySize) {
                scenes.arraySize = insertIndex + 1;
            } else {
                scenes.InsertArrayElementAtIndex(insertIndex);
            }

            nodeFirstChildSceneIndex.intValue = nodeFirstChildSceneIndexValue;
            nodeChildScenesCount.intValue += 1;
            scenesCount += 1;
            var insertedScene = scenes.GetArrayElementAtIndex(insertIndex);
            var uniqueIdCounter = serializedProperty.FindPropertyRelative(NameScenesUniqueIdCounter);
            int newUniqueId = uniqueIdCounter.intValue + 1;
            uniqueIdCounter.intValue = newUniqueId;
            insertedScene.FindPropertyRelative(NameSceneStableUniqueId).intValue = newUniqueId;
            insertedScene.FindPropertyRelative(nameof(SubsceneData.nameOverride)).stringValue = friendlyName;
            insertedScene.FindPropertyRelative(NameNodeId).intValue = SerializedSubscenesData.GetId(insertIndex, nodeIndex);
            SetSceneRef(insertedScene, scenePath, serializedProperty);

            var nodes = serializedProperty.FindPropertyRelative(NameNodes);
            int nodesCount = nodes.arraySize;
            for (int i = 0; i < nodesCount; i++) {
                if (i == nodeIndex) {
                    continue;
                }

                var notShiftedNode = nodes.GetArrayElementAtIndex(i);
                var notShiftedNodeChildScenesCount =
                    notShiftedNode.FindPropertyRelative(NameChildScenesCount).intValue;
                if (notShiftedNodeChildScenesCount == 0) {
                    continue;
                }

                var notShiftedNodeFirstChildSceneIndex = notShiftedNode.FindPropertyRelative(NameFirstChildSceneIndex);
                if (notShiftedNodeFirstChildSceneIndex.intValue < insertIndex) {
                    continue;
                }

                notShiftedNodeFirstChildSceneIndex.intValue += 1;
            }

            for (int i = insertIndex + 1; i < scenesCount; i++) {
                var shiftedScene = scenes.GetArrayElementAtIndex(i);
                var shiftedSceneId = shiftedScene.FindPropertyRelative(NameSceneId);
                var shiftedScenePrevIdValue = shiftedSceneId.intValue;
                (short shiftedScenePrevIndex, short shiftedSceneParentNodeIndex) =
                    SerializedSubscenesData.DeconstructId(shiftedScenePrevIdValue);
                var shiftedSceneNewIndex = (short)(shiftedScenePrevIndex + 1);
                shiftedSceneId.intValue = SerializedSubscenesData.GetId(shiftedSceneNewIndex, shiftedSceneParentNodeIndex);
            }
            node.isExpanded = true;
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }
        
        static void MoveSceneUp(short sceneIndex, SerializedProperty serializedProperty) {
            MoveScene(sceneIndex, true, serializedProperty);
        }
        
        static void MoveSceneDown(short sceneIndex, SerializedProperty serializedProperty) {
            MoveScene(sceneIndex, false, serializedProperty);
        }

        static void MoveScene(short sceneIndex, bool up, SerializedProperty serializedProperty) {
            var nodes = serializedProperty.FindPropertyRelative(NameNodes);
            var scenes = serializedProperty.FindPropertyRelative(NameScenes);
            var scene = scenes.GetArrayElementAtIndex(sceneIndex);
            var sceneId = scene.FindPropertyRelative(NameSceneId);
            var parentNodeIndex = SerializedSubscenesData.GetParentNodeIndex((short)sceneId.intValue);
            var parentNode = nodes.GetArrayElementAtIndex(parentNodeIndex);
            var parentNodeFirstChildSceneIndex = (short)parentNode.FindPropertyRelative(NameFirstChildSceneIndex).intValue;
            if (up && parentNodeFirstChildSceneIndex == sceneIndex) {
                return;
            }
            var parentNodeChildScenesCount = parentNode.FindPropertyRelative(NameChildScenesCount).intValue;
            if (!up && parentNodeFirstChildSceneIndex + parentNodeChildScenesCount - 1 == sceneIndex) {
                return;
            }

            var offset = up ? -1 : 1;
            var adjacentSceneIndex = (sceneIndex + offset);
            var adjacentScene = scenes.GetArrayElementAtIndex(adjacentSceneIndex);
            var adjacentSceneID  = adjacentScene.FindPropertyRelative(NameSceneId);
            sceneId.intValue = SerializedSubscenesData.GetId((short)adjacentSceneIndex, parentNodeIndex);
            adjacentSceneID.intValue = SerializedSubscenesData.GetId(sceneIndex, parentNodeIndex);
            scenes.MoveArrayElement(sceneIndex, adjacentSceneIndex);
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }
        
        static void MoveNodeUp(short nodeIndex, SerializedProperty serializedProperty) {
            MoveNode(nodeIndex, true, serializedProperty);
        }
        
        static void MoveNodeDown(short nodeIndex, SerializedProperty serializedProperty) {
            MoveNode(nodeIndex, false, serializedProperty);
        }

        static void MoveNode(short nodeIndex, bool up, SerializedProperty serializedProperty) {
            if (nodeIndex == 0) {
                return;
            }
            var nodes = serializedProperty.FindPropertyRelative(NameNodes);
            var node = nodes.GetArrayElementAtIndex(nodeIndex);
            var nodeId = node.FindPropertyRelative(NameNodeId);
            var parentNodeIndex = SerializedSubscenesData.GetParentNodeIndex(nodeId.intValue);
            var parentNode = nodes.GetArrayElementAtIndex(parentNodeIndex);
            var parentNodeFirstChildIndexValue = parentNode.FindPropertyRelative(NameFirstChildNodeIndex).intValue;
            if (up && parentNodeFirstChildIndexValue == nodeIndex) {
                return;
            }
            var parentNodeChildNodesCountValue = parentNode.FindPropertyRelative(NameChildNodesCount).intValue;
            if (!up && parentNodeFirstChildIndexValue + parentNodeChildNodesCountValue - 1 == nodeIndex) {
                return;
            }

            var offset = up ? -1 : 1;
            var adjacentNodeIndex = (nodeIndex + offset);
            var adjacentNode = nodes.GetArrayElementAtIndex(adjacentNodeIndex);
            var adjacentNodeId = adjacentNode.FindPropertyRelative(NameNodeId);
            nodeId.intValue = SerializedSubscenesData.GetId((short)adjacentNodeIndex, parentNodeIndex);
            adjacentNodeId.intValue = SerializedSubscenesData.GetId(nodeIndex, parentNodeIndex);
            nodes.MoveArrayElement(nodeIndex, adjacentNodeIndex);
            UpdateNodesSwappedParentNodesIndices(nodeIndex, adjacentNodeIndex, serializedProperty);
            UpdateScenesSwappedParentNodesIndices(nodeIndex, adjacentNodeIndex, serializedProperty);
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        static int AddChildNode(short nodeIndex, SerializedProperty serializedProperty) {
            var nodes = serializedProperty.FindPropertyRelative(NameNodes);
            var node = nodes.GetArrayElementAtIndex(nodeIndex);
            var nodeChildNodesCount = node.FindPropertyRelative(NameChildNodesCount);
            var nodeFirstChildIndex = node.FindPropertyRelative(NameFirstChildNodeIndex);
            var nodeFirstChildIndexValue = (short)nodeFirstChildIndex.intValue;
            var nodesCount = (short)nodes.arraySize;
            if (nodeChildNodesCount.intValue == 0) {
                nodeFirstChildIndexValue = nodesCount;
            }

            var insertIndex = (short)(nodeFirstChildIndexValue + (short)nodeChildNodesCount.intValue);
            if (nodeIndex == nodesCount) {
                nodes.arraySize += 1;
            } else {
                nodes.InsertArrayElementAtIndex(insertIndex);
            }
            nodesCount += 1;

            nodeFirstChildIndex.intValue = nodeFirstChildIndexValue;
            nodeChildNodesCount.intValue += 1;

            var insertedNode = nodes.GetArrayElementAtIndex(insertIndex);
            insertedNode.FindPropertyRelative(NameNodeId).intValue =
                SerializedSubscenesData.GetId(insertIndex, nodeIndex);
            var uniqueIdCounter = serializedProperty.FindPropertyRelative(NameNodesUniqueIdCounter);
            int newUniqueId = uniqueIdCounter.intValue + 1;
            uniqueIdCounter.intValue = newUniqueId;
            insertedNode.FindPropertyRelative(NameNodeStableUniqueId).intValue = newUniqueId;
            insertedNode.FindPropertyRelative(NameName).stringValue = "new folder " + newUniqueId;
            insertedNode.FindPropertyRelative(NameChildScenesCount).intValue = 0;
            insertedNode.FindPropertyRelative(NameFirstChildSceneIndex).intValue = 0;
            insertedNode.FindPropertyRelative(NameChildNodesCount).intValue = 0;
            insertedNode.FindPropertyRelative(NameFirstChildNodeIndex).intValue = 0;
            for (int i = 0; i < nodesCount; i++) {
                if (i == nodeIndex || i == insertIndex) {
                    continue;
                }

                var notShiftedNode = nodes.GetArrayElementAtIndex(i);
                var notShiftedNodeChildNodesCount = notShiftedNode.FindPropertyRelative(NameChildNodesCount).intValue;
                if (notShiftedNodeChildNodesCount == 0) {
                    continue;
                }

                var notShiftedNodeFirstChildNodeIndex = notShiftedNode.FindPropertyRelative(NameFirstChildNodeIndex);
                if (notShiftedNodeFirstChildNodeIndex.intValue < insertIndex) {
                    continue;
                }

                notShiftedNodeFirstChildNodeIndex.intValue += 1;
            }

            for (int i = insertIndex + 1; i < nodesCount; i++) {
                var shiftedNode = nodes.GetArrayElementAtIndex(i);
                var shiftedNodeId = shiftedNode.FindPropertyRelative(NameNodeId);
                var shiftedNodePrevIdValue = shiftedNodeId.intValue;
                (short shiftedNodePrevIndex, short shiftedNodeParentNodePrevIndex) =
                    SerializedSubscenesData.DeconstructId(shiftedNodePrevIdValue);
                var shiftedNodeNewIndex = (short)(shiftedNodePrevIndex + 1);
                var shiftedNodeNewParentIndex = shiftedNodeParentNodePrevIndex < insertIndex
                    ? shiftedNodeParentNodePrevIndex
                    : (short)(shiftedNodeParentNodePrevIndex + 1);
                shiftedNodeId.intValue =
                    SerializedSubscenesData.GetId(shiftedNodeNewIndex, shiftedNodeNewParentIndex);
            }

            var scenes = serializedProperty.FindPropertyRelative(NameScenes);
            int scenesCount = scenes.arraySize;
            for (int i = 0; i < scenesCount; i++) {
                var scene = scenes.GetArrayElementAtIndex(i);
                var sceneId = scene.FindPropertyRelative(NameSceneId);
                var (sceneIndex, sceneParentNodePrevIndex) = SerializedSubscenesData.DeconstructId(sceneId.intValue);
                var newSceneParentNodeNewIndex = sceneParentNodePrevIndex < insertIndex
                    ? sceneParentNodePrevIndex
                    : (short)(sceneParentNodePrevIndex + 1);
                sceneId.intValue = SerializedSubscenesData.GetId(sceneIndex, newSceneParentNodeNewIndex);
            }

            node.isExpanded = true;
            insertedNode.isExpanded = true;
            serializedProperty.serializedObject.ApplyModifiedProperties();
            return insertIndex;
        }

        static void OpenAddSceneWindow(SerializedProperty serializedProperty, short parentNodeIndex) {
            StopEditing(serializedProperty);
            var component = serializedProperty.serializedObject.targetObject as Component;
            if (component != null) {
                var scene = component.gameObject.scene;
                serializedProperty.isExpanded = true;
                SubdividedSceneCreator.Show(serializedProperty, scene, parentNodeIndex);
            }
        }

        static void SetSceneRef(SerializedProperty sceneProperty, string scenePath, SerializedProperty serializedProperty) {
            var assetReferenceProperty = sceneProperty.FindPropertyRelative(nameof(SubsceneData.reference)).FindPropertyRelative("reference");
            var assetReference = assetReferenceProperty.boxedValue as ARAssetReference;
            ARAssetReferencePropertyDrawer.AssignAsset(
                assetReferenceProperty, assetReference,
                AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath),
                SceneReferenceSettings
            );
            assetReferenceProperty.boxedValue = assetReference;
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        static void RemoveScene(short sceneToRemoveIndex, SerializedProperty serializedProperty) {
            var scenes = serializedProperty.FindPropertyRelative(NameScenes);
            var sceneToRemove = scenes.GetArrayElementAtIndex(sceneToRemoveIndex);
            var removedSceneParentNodeIndex =
                SerializedSubscenesData.GetParentNodeIndex(sceneToRemove.FindPropertyRelative(NameSceneId).intValue);
            scenes.DeleteArrayElementAtIndex(sceneToRemoveIndex);
            var nodes = serializedProperty.FindPropertyRelative(NameNodes);
            var removedSceneParentNode = nodes.GetArrayElementAtIndex(removedSceneParentNodeIndex);
            removedSceneParentNode.FindPropertyRelative(NameChildScenesCount).intValue -= 1;

            int nodesCount = nodes.arraySize;
            for (int i = 0; i < nodesCount; i++) {
                if (i == removedSceneParentNodeIndex) {
                    continue;
                }

                var node = nodes.GetArrayElementAtIndex(i);
                var nodeChildScenesCount = node.FindPropertyRelative(NameChildScenesCount);
                var nodeChildScenesCountValue = nodeChildScenesCount.intValue;
                if (nodeChildScenesCountValue == 0) {
                    continue;
                }

                var nodeFirstChildSceneIndex = node.FindPropertyRelative(NameFirstChildSceneIndex);
                var nodeFirstChildSceneIndexValue = nodeFirstChildSceneIndex.intValue;
                if (nodeFirstChildSceneIndexValue <= sceneToRemoveIndex) {
                    continue;
                }

                nodeFirstChildSceneIndex.intValue -= 1;
            }

            var scenesCount = scenes.arraySize;
            for (int i = sceneToRemoveIndex; i < scenesCount; i++) {
                var shiftedScene = scenes.GetArrayElementAtIndex(i);
                var shiftedSceneId = shiftedScene.FindPropertyRelative(NameSceneId);
                var shiftedScenePrevIdValue = shiftedSceneId.intValue;
                (short shiftedScenePrevIndex, short shiftedSceneParentNodeIndex) =
                    SerializedSubscenesData.DeconstructId(shiftedScenePrevIdValue);
                var shiftedSceneNewIndex = (short)(shiftedScenePrevIndex - 1);
                shiftedSceneId.intValue = SerializedSubscenesData.GetId(shiftedSceneNewIndex, shiftedSceneParentNodeIndex);
            }

            serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        static void RemoveNode(short nodeToRemoveIndex, SerializedProperty serializedProperty) {
            if (nodeToRemoveIndex == 0) {
                return;
            }

            var nodes = serializedProperty.FindPropertyRelative(NameNodes);
            var nodeToRemove = nodes.GetArrayElementAtIndex(nodeToRemoveIndex);
            var nodeToRemoveChildScenesCount = nodeToRemove.FindPropertyRelative(NameChildScenesCount);
            var nodeToRemoveChildScenesCountValue = nodeToRemoveChildScenesCount.intValue;
            if (nodeToRemoveChildScenesCountValue > 0) {
                var firstChildSceneIndexValue = nodeToRemove.FindPropertyRelative(NameFirstChildSceneIndex).intValue;
                for (int i = nodeToRemoveChildScenesCountValue - 1; i >= 0; i--) {
                    RemoveScene((short)(firstChildSceneIndexValue + i), serializedProperty);
                }
            }

            nodeToRemoveChildScenesCount.intValue = 0;
            var nodeToRemoveChildNodesCount = nodeToRemove.FindPropertyRelative(NameChildNodesCount);
            var nodeToRemoveChildNodesCountValue = nodeToRemoveChildNodesCount.intValue;
            if (nodeToRemoveChildNodesCountValue > 0) {
                var firstChildNodeIndexValue = nodeToRemove.FindPropertyRelative(NameFirstChildNodeIndex).intValue;
                for (int i = 0; i < nodeToRemoveChildNodesCountValue; i++) {
                    RemoveNode((short)(firstChildNodeIndexValue + i), serializedProperty);
                }
            }

            nodeToRemoveChildNodesCount.intValue = 0;
            var removedNodeParentNodeIndex =
                SerializedSubscenesData.GetParentNodeIndex(nodeToRemove.FindPropertyRelative(NameNodeId)
                    .intValue);
            nodes.DeleteArrayElementAtIndex(nodeToRemoveIndex);
            var removedNodeParentNode = nodes.GetArrayElementAtIndex(removedNodeParentNodeIndex);
            removedNodeParentNode.FindPropertyRelative(NameChildNodesCount).intValue -= 1;

            int nodesCount = nodes.arraySize;
            for (int i = 0; i < nodesCount; i++) {
                if (i == removedNodeParentNodeIndex) {
                    continue;
                }

                var notShiftedNode = nodes.GetArrayElementAtIndex(i);
                var notShiftedNodeChildNodesCount = notShiftedNode.FindPropertyRelative(NameChildNodesCount).intValue;
                if (notShiftedNodeChildNodesCount == 0) {
                    continue;
                }

                var notShiftedNodeFirstChildNodeIndex = notShiftedNode.FindPropertyRelative(NameFirstChildNodeIndex);
                if (notShiftedNodeFirstChildNodeIndex.intValue <= nodeToRemoveIndex) {
                    continue;
                }

                notShiftedNodeFirstChildNodeIndex.intValue -= 1;
            }

            for (int i = nodeToRemoveIndex; i < nodesCount; i++) {
                var shiftedNode = nodes.GetArrayElementAtIndex(i);
                var shiftedNodeId = shiftedNode.FindPropertyRelative(NameNodeId);
                var shiftedNodePrevIdValue = shiftedNodeId.intValue;
                (short shiftedNodePrevIndex, short shiftedNodeParentNodePrevIndex) =
                    SerializedSubscenesData.DeconstructId(shiftedNodePrevIdValue);
                var shiftedNodeNewIndex = (short)(shiftedNodePrevIndex - 1);
                var shiftedNodeNewParentIndex = shiftedNodeParentNodePrevIndex < nodeToRemoveIndex
                    ? shiftedNodeParentNodePrevIndex
                    : (short)(shiftedNodeParentNodePrevIndex - 1);
                shiftedNodeId.intValue =
                    SerializedSubscenesData.GetId(shiftedNodeNewIndex, shiftedNodeNewParentIndex);
            }

            var scenes = serializedProperty.FindPropertyRelative(NameScenes);
            int scenesCount = scenes.arraySize;
            for (int i = 0; i < scenesCount; i++) {
                var scene = scenes.GetArrayElementAtIndex(i);
                var sceneId = scene.FindPropertyRelative(NameSceneId);
                var (sceneIndex, sceneParentNodePrevIndex) = SerializedSubscenesData.DeconstructId(sceneId.intValue);
                var newSceneParentNodeNewIndex = sceneParentNodePrevIndex < nodeToRemoveIndex
                    ? sceneParentNodePrevIndex
                    : (short)(sceneParentNodePrevIndex - 1);
                sceneId.intValue = SerializedSubscenesData.GetId(sceneIndex, newSceneParentNodeNewIndex);
            }

            serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        static void MarkSceneWithNodesToLoad(short sceneIndex, bool load, SerializedProperty serializedProperty) {
            var nodes = serializedProperty.FindPropertyRelative(NameNodes);
            var scenes = serializedProperty.FindPropertyRelative(NameScenes); 
            var scene = scenes.GetArrayElementAtIndex(sceneIndex);
            var sceneStableUniqueId = scene.FindPropertyRelative(NameSceneStableUniqueId).intValue;
            MarkSceneToLoad(sceneStableUniqueId, load);

            if (load) {
                var sceneIdValue = scene.FindPropertyRelative(NameSceneId).intValue;
                var parentNodeIndex = SerializedSubscenesData.GetParentNodeIndex(sceneIdValue);
                var parentNode = nodes.GetArrayElementAtIndex(parentNodeIndex);
                var parentNodeStableUniqueIdValue = parentNode.FindPropertyRelative(NameNodeStableUniqueId).intValue;
                MarkNodeToLoad(parentNodeStableUniqueIdValue, true);
                MakeParentNodesLoadable(parentNodeIndex, nodes);
            }
        }
        
        static void MarkNodeToLoad(short nodeIndex, bool load, SerializedProperty serializedProperty) {
            var nodes = serializedProperty.FindPropertyRelative(NameNodes);
            var node = nodes.GetArrayElementAtIndex(nodeIndex);
            var nodeChildScenesCountValue = node.FindPropertyRelative(NameChildScenesCount).intValue;
            if (nodeChildScenesCountValue > 0) {
                var scenes = serializedProperty.FindPropertyRelative(NameScenes); 
                var firstChildSceneIndexValue = node.FindPropertyRelative(NameFirstChildSceneIndex).intValue;
                for (int i = 0; i < nodeChildScenesCountValue; i++) {
                    var sceneIndex = (firstChildSceneIndexValue + i);
                    var scene = scenes.GetArrayElementAtIndex(sceneIndex);
                    var sceneStableUniqueId = scene.FindPropertyRelative(NameSceneStableUniqueId).intValue;
                    MarkSceneToLoad(sceneStableUniqueId, load);
                }
            }
            var nodeChildNodesCount = node.FindPropertyRelative(NameChildNodesCount);
            var nodeChildNodesCountValue = nodeChildNodesCount.intValue;
            if (nodeChildNodesCountValue > 0) {
                var firstChildNodeIndexValue = node.FindPropertyRelative(NameFirstChildNodeIndex).intValue;
                for (int i = 0; i < nodeChildNodesCountValue; i++) {
                    MarkNodeToLoad((short)(firstChildNodeIndexValue + i), load, serializedProperty);
                }
            }

            var nodeStableUniqueId = node.FindPropertyRelative(NameNodeStableUniqueId).intValue;
            MarkNodeToLoad(nodeStableUniqueId, load);
            if (load) {
                MakeParentNodesLoadable(nodeIndex, nodes);
            }
        }

        static void MakeParentNodesLoadable(short nodeIndex, SerializedProperty nodes) {
            var node = nodes.GetArrayElementAtIndex(nodeIndex);
            var nodeId = node.FindPropertyRelative(NameNodeId);
            var parentNodeIndex = SerializedSubscenesData.GetParentNodeIndex(nodeId.intValue);
            if (parentNodeIndex < 0) {
                return;
            }

            var parentNodeStableUniqueIdValue = nodes.GetArrayElementAtIndex(parentNodeIndex)
                .FindPropertyRelative(NameNodeStableUniqueId).intValue;
            MarkNodeToLoad(parentNodeStableUniqueIdValue, true);
            MakeParentNodesLoadable(parentNodeIndex, nodes);
        }


        /// ---  EditorPrefs

        static void MarkSceneToLoad(int sceneStableUniqueId, bool load) {
            EditorPrefs.SetBool(EditorPrefNameDoNotLoadSubscene + sceneStableUniqueId, !load);
        }
        
        static void MarkNodeToLoad(int nodeStableUniqueId, bool load) {
            EditorPrefs.SetBool(EditorPrefNameDoNotLoadNode + nodeStableUniqueId, !load);
        }

        static bool IsSceneNeededToLoad(int sceneStableUniqueId) {
            return EditorPrefs.GetBool(EditorPrefNameDoNotLoadSubscene + sceneStableUniqueId) == false;
        }
        
        static bool IsNodeNeededToLoad(int nodeStableUniqueId) {
            return EditorPrefs.GetBool(EditorPrefNameDoNotLoadNode + nodeStableUniqueId) == false;
        }
        

        /// ---  ScenesLoading
        
        static void LoadAllChildScenes(int nodeIndex, SerializedProperty serializedProperty) {
            var nodes = serializedProperty.FindPropertyRelative(NameNodes);
            var node = nodes.GetArrayElementAtIndex(nodeIndex);
            var nodeChildNodesCountValue = node.FindPropertyRelative(NameChildNodesCount).intValue;
            var nodeFirstChildNodeIndexValue = node.FindPropertyRelative(NameFirstChildNodeIndex).intValue;
            for (int i = 0; i < nodeChildNodesCountValue; i++) {
                LoadAllChildScenes(nodeFirstChildNodeIndexValue + i, serializedProperty);
            }

            var nodeChildScenesCount = node.FindPropertyRelative(NameChildScenesCount).intValue;
            if (nodeChildScenesCount == 0) {
                return;
            }

            var scenes = serializedProperty.FindPropertyRelative(NameScenes);
            var nodeFirstChildSceneIndexValue = node.FindPropertyRelative(NameFirstChildSceneIndex).intValue;
            for (int i = 0; i < nodeChildScenesCount; i++) {
                var scene = scenes.GetArrayElementAtIndex(nodeFirstChildSceneIndexValue + i);
                LoadScene(scene);
            }
        }
        
        static void UnloadAllChildScenes(int nodeIndex, bool withSave, SerializedProperty serializedProperty) {
            var nodes = serializedProperty.FindPropertyRelative(NameNodes);
            var node = nodes.GetArrayElementAtIndex(nodeIndex);
            var nodeChildNodesCountValue = node.FindPropertyRelative(NameChildNodesCount).intValue;
            var nodeFirstChildNodeIndexValue = node.FindPropertyRelative(NameFirstChildNodeIndex).intValue;
            for (int i = 0; i < nodeChildNodesCountValue; i++) {
                UnloadAllChildScenes(nodeFirstChildNodeIndexValue + i, withSave, serializedProperty);
            }

            var nodeChildScenesCount = node.FindPropertyRelative(NameChildScenesCount).intValue;
            if (nodeChildScenesCount == 0) {
                return;
            }

            var scenes = serializedProperty.FindPropertyRelative(NameScenes);
            var nodeFirstChildSceneIndexValue = node.FindPropertyRelative(NameFirstChildSceneIndex).intValue;
            for (int i = 0; i < nodeChildScenesCount; i++) {
                var scene = scenes.GetArrayElementAtIndex(nodeFirstChildSceneIndexValue + i);
                UnloadScene(scene, withSave, false);
            }
        }
        
        static void UnloadScene(SerializedProperty scene, bool withSave, bool cleanupUnity) {
            var sceneReference = (SceneReference)scene.FindPropertyRelative(NameSceneReference).boxedValue;
            UnloadScene(sceneReference, withSave, cleanupUnity);
        }   
        
        static void UnloadScene(SceneReference sceneReference, bool withSave, bool cleanupUnity) {
            new SceneReference.EditorAccess(sceneReference).UnloadScene(withSave);
            if (cleanupUnity) {
                CleanupUnity();
            }
        }
        
        static void LoadScene(SerializedProperty scene) {
            var sceneReference = (SceneReference)scene.FindPropertyRelative(NameSceneReference).boxedValue;
            LoadScene(sceneReference);
        }
        
        static void LoadScene(SceneReference sceneReference) {
            new SceneReference.EditorAccess(sceneReference).LoadScene();
        }
        
        // --- Utility
        
        static void SetChildNodesExpanded(int nodeIndex, bool expand, SerializedProperty serializedProperty) {
            var nodes = serializedProperty.FindPropertyRelative(NameNodes);
            var node = nodes.GetArrayElementAtIndex(nodeIndex);
            node.isExpanded = expand;
            var nodeChildNodesCountValue = node.FindPropertyRelative(NameChildNodesCount).intValue;
            var nodeFirstChildNodeIndexValue = node.FindPropertyRelative(NameFirstChildNodeIndex).intValue;
            for (int i = 0; i < nodeChildNodesCountValue; i++) {
                SetChildNodesExpanded(nodeFirstChildNodeIndexValue + i, expand, serializedProperty);
            }
        }

        static void UpdateScenesSwappedParentNodesIndices(short swappedNodeIndex,
            int adjacentSwappedNodeIndex, SerializedProperty serializedProperty) {
            var scenes = serializedProperty.FindPropertyRelative(NameScenes);
            int scenesCount = scenes.arraySize;
            for (int i = 0; i < scenesCount; i++) {
                var scene = scenes.GetArrayElementAtIndex(i);
                var sceneId = scene.FindPropertyRelative(NameSceneId);
                var (sceneIndex, sceneParentNodePrevIndex) = SerializedSubscenesData.DeconstructId(sceneId.intValue);
                var newSceneParentNodeNewIndex = sceneParentNodePrevIndex;
                if (sceneParentNodePrevIndex == swappedNodeIndex) {
                    newSceneParentNodeNewIndex = (short)adjacentSwappedNodeIndex;
                } else if (sceneParentNodePrevIndex == adjacentSwappedNodeIndex) {
                    newSceneParentNodeNewIndex = swappedNodeIndex;
                }

                sceneId.intValue = SerializedSubscenesData.GetId(sceneIndex, newSceneParentNodeNewIndex);
            }
        }
        
        static void UpdateNodesSwappedParentNodesIndices(short swappedNodeIndex,
            int adjacentSwappedNodeIndex, SerializedProperty serializedProperty) {
            var nodes = serializedProperty.FindPropertyRelative(NameNodes);
            int nodesCount = nodes.arraySize;
            for (int i = 0; i < nodesCount; i++) {
                var node = nodes.GetArrayElementAtIndex(i);
                var nodeId = node.FindPropertyRelative(NameNodeId);
                var (nodeIndex, nodeParentNodePrevIndex) = SerializedSubscenesData.DeconstructId(nodeId.intValue);
                var newNodeParentNodeNewIndex = nodeParentNodePrevIndex;
                if (nodeParentNodePrevIndex == swappedNodeIndex) {
                    newNodeParentNodeNewIndex = (short)adjacentSwappedNodeIndex;
                } else if (nodeParentNodePrevIndex == adjacentSwappedNodeIndex) {
                    newNodeParentNodeNewIndex = swappedNodeIndex;
                }
                nodeId.intValue = SerializedSubscenesData.GetId(nodeIndex, newNodeParentNodeNewIndex);
            }
        }
        
        static bool IsCurrentlyEdited(string propertyPath, uint serializedPropertyContentHash) {
            return editedPropertyPathByProperty[serializedPropertyContentHash] == propertyPath;
        }
        
        static void CleanupUnity() {
            EditorUtility.UnloadUnusedAssetsImmediate();
            GC.Collect();
        }
        
        static Rect IndentedRect(Rect rect) {
            const float indentStep = 20f;
            rect.x += indentStep;
            rect.width -= indentStep;
            return rect;
        }

        readonly struct InputContext {
            public readonly Event evt;
            public readonly Vector2 mousePosition;
            public readonly bool contextClick;

            public InputContext(Event evt) {
                this.evt = evt;
                mousePosition = evt.mousePosition;
                contextClick = evt.type == EventType.MouseDown && evt.button == 1;
            }
        }
    }
}