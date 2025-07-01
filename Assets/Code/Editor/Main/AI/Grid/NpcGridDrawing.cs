using System.Globalization;
using System.Text;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor;
using Awaken.Utility.Maths;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.AI.Grid {
    internal static class NpcGridDrawing {
        static bool s_isNpcGridGizmosToggled;
        
        static readonly Color GridColor = Color.green;
        static readonly Color HeroColor = (Color.red + Color.yellow) * 0.5f;
        static readonly Color NpcColor = Color.red;
        static readonly Color DummyColor = Color.yellow;
        static readonly Color CorpseColor = Color.yellow;
        
        [StaticMarvinButton(state: nameof(s_isNpcGridGizmosToggled))]
        static void ToggleNpcGridGizmos() {
            if (s_isNpcGridGizmosToggled) {
                SceneView.duringSceneGui -= DrawNpcGridGizmos;
                s_isNpcGridGizmosToggled = false;
            } else {
                SceneView.duringSceneGui += DrawNpcGridGizmos;
                s_isNpcGridGizmosToggled = true;
            }
        }

        static void DrawNpcGridGizmos(SceneView sceneView) {
            var grid = World.Services.TryGet<NpcGrid>();
            if (grid == null) {
                return;
            }
            var previousGizmosColor = Handles.color;
            var accessor = new NpcGrid.EDITOR_Accessor(grid);
            var gridCenter = accessor.Center;
            var gridHalfSize = grid.GridHalfSize;
            var chunkSize = grid.ChunkSize;
            for (int x = gridCenter.x - gridHalfSize; x <= gridCenter.x + gridHalfSize; x++) {
                for (int y = gridCenter.y - gridHalfSize; y <= gridCenter.y + gridHalfSize; y++) {
                    Handles.color = GridColor;
                    var cubeCenter = new Vector3((x + 0.5f) * chunkSize, 0, (y + 0.5f) * chunkSize);
                    var cubeSize = chunkSize.UniformVector3();
                    Handles.DrawWireCube(cubeCenter, cubeSize);
                    
                    var chunk = accessor.GetChunkUnchecked(x, y);
                    foreach (var npc in chunk.Npcs) {
                        Handles.color = NpcColor;
                        var coords = npc.Coords;
                        HandlesUtils.DrawSphere(coords, 0.5f);
                        Handles.DrawLine(coords, cubeCenter);
                    }

                    foreach (var corpse in chunk.Corpses) {
                        Handles.color = CorpseColor;
                        var coords = corpse.Coords;
                        HandlesUtils.DrawSphere(coords, 0.5f);
                        Handles.DrawLine(coords, cubeCenter);
                    }

                    foreach (var dummy in chunk.Dummies) {
                        Handles.color = DummyColor;
                        var coords = dummy.Coords;
                        HandlesUtils.DrawSphere(coords, 0.5f);
                        Handles.DrawLine(coords, cubeCenter);
                    }

                    if (chunk.Hero is { } hero) {
                        Handles.color = HeroColor;
                        var coords = hero.Coords;
                        HandlesUtils.DrawSphere(coords, 0.5f);
                        Handles.DrawLine(coords, cubeCenter);
                    }

                    var chunkDataAccess = new NpcChunkData.EDITOR_Accessor();
                    var sb = new StringBuilder();
                    sb.Append("Event Danger: ");
                    sb.AppendLine(chunkDataAccess.DangerEventLifetime(chunk.Data).ToString(CultureInfo.InvariantCulture));
                    sb.Append("Combat Danger: ");
                    sb.AppendLine(chunkDataAccess.DangerCombatLifetime(chunk.Data).ToString(CultureInfo.InvariantCulture));
                    sb.Append("Hero Danger: ");
                    sb.AppendLine(chunkDataAccess.DangerHeroLifetime(chunk.Data).ToString(CultureInfo.InvariantCulture));
                    sb.Append("Has Hero: ");
                    sb.AppendLine(chunkDataAccess.HasHero(chunk.Data).ToString(CultureInfo.InvariantCulture));
                    sb.AppendLine();
                    sb.Append("Local Danger: ");
                    sb.Append(chunkDataAccess.HasLocalDanger(chunk.Data).ToString());
                    sb.Append(" (Fearfuls: ");
                    sb.Append(chunkDataAccess.HasLocalDangerForFearfuls(chunk.Data).ToString());
                    sb.AppendLine(")");
                    sb.Append("Spread Danger: ");
                    sb.Append(chunkDataAccess.HasSpreadDanger(chunk.Data).ToString());
                    sb.Append(" (Fearfuls: ");
                    sb.Append(chunkDataAccess.HasSpreadlDangerForFearfuls(chunk.Data).ToString());
                    sb.AppendLine(")");

                    HandlesUtils.Label(cubeCenter, sb.ToString());
                }
            }
            Handles.color = previousGizmosColor;
        }
    }
}