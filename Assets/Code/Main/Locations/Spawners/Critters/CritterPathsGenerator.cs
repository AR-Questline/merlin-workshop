using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners.Critters {
    public static class CritterPathsGenerator {
        public static Vector3[][] GeneratePathsInEditor(int amountOfPaths, int amountOfPointsPerPath, Vector3 center, float radius, Seeker seeker) {
            var points = GetRandomPoints(amountOfPaths * amountOfPointsPerPath, center, radius).ToArray();

            Vector3[][] combinedPaths = new Vector3[amountOfPaths][];
            var separatePaths = new Path[amountOfPointsPerPath];

            for (int combinedPathIndex = 0; combinedPathIndex < amountOfPaths; combinedPathIndex++) {
                for (int separatePathIndex = 0; separatePathIndex < amountOfPointsPerPath; separatePathIndex++) {
                    int point = combinedPathIndex * amountOfPointsPerPath + separatePathIndex;
                    int nextPoint = combinedPathIndex * amountOfPointsPerPath + (separatePathIndex + 1) % amountOfPointsPerPath;
                    separatePaths[separatePathIndex] = ABPath.Construct(points[point], points[nextPoint]);
                }
                combinedPaths[combinedPathIndex] = GeneratePathsPointsInEditor(separatePaths, seeker);
            }
            return combinedPaths;
        }

        static IEnumerable<Vector3> GetRandomPoints(int amount, Vector3 center, float radius) {
            var sequence = RandomUtil.HaltonSequence2D();
            for (int i = 0; i < amount; i++) {
                var offset = sequence.Current;
                offset = math.remap(0, 1, -1, 1, offset);
                sequence.MoveNext();
                yield return center + new Vector3(offset.x * radius, 0, offset.y * radius);
            }
        }

        static Vector3[] GeneratePathsPointsInEditor(Path[] paths, Seeker seeker) {
            foreach (var path in paths) {
                GeneratePathInEditor(path, seeker);
            }
            return CombinePaths(paths);
        }

        static void GeneratePathInEditor(Path path, Seeker seeker) {
            seeker.StartPath(path).BlockUntilCalculated();
        }

        static Vector3[] CombinePaths(Path[] paths) {
            var combinedPath = new List<Vector3>();
            foreach (var path in paths) {
                var vectorPath = path.vectorPath;
                if (vectorPath.Count > 0) {
                    vectorPath.RemoveAt(vectorPath.Count - 1);
                    combinedPath.AddRange(vectorPath.Select(v => Ground.SnapToGround(v)));
                }
            }
            return combinedPath.ToArray();
        }
    }
}