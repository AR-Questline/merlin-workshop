using System;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Utility.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.Terrain {
    public class FootStepsUtils {
        public static void ClearParameters(FMODParameter[] fmodParameters) {
            for (int i = 0; i < fmodParameters.Length; i++) {
                fmodParameters[i] = new FMODParameter(fmodParameters[i].name, 0);
            }
        }
        
        public static bool TryGetTerrainTypeIndex(string paramName, out byte terrainTypeIndex) {
            for (byte i = 0; i < SurfaceType.TerrainTypes.Length; i++) {
                if (SurfaceType.TerrainTypes[i].FModParameterName == paramName) {
                    terrainTypeIndex = i;
                    return true;
                }
            }
            terrainTypeIndex = 0;
            return false;
        }

        public static void ClearAllAndSet(FMODParameter[] fmodParameters, string paramToSetName, float valueToSet) {
            for (int i = 0; i < fmodParameters.Length; i++) {
                var paramName = fmodParameters[i].name;
                var newValue = paramName == paramToSetName ? valueToSet : 0f;
                fmodParameters[i] = new FMODParameter(paramName, newValue);
            }
        }

        public static bool TryInitialize(ref FMODParameter[] fmodParameters, EventReference footStepEventReference, int reserveIndicesAtArrayStartCount = 0, params string[] paramsNamesToSkip) {
            // if (RuntimeManager.TryGetEventDescription(footStepEventReference, out var footStepEventDesc)) {
            //     footStepEventDesc.getParameterDescriptionCount(out int parametersCount);
            //     int terrainTypesCount = 0;
            //     Span<byte> terrainTypeIndices = stackalloc byte[SurfaceType.TerrainTypes.Length];
            //     for (int paramIndex = 0; paramIndex < parametersCount; paramIndex++) {
            //         footStepEventDesc.getParameterDescriptionByIndex(paramIndex, out var parameterDescription);
            //         string paramName = parameterDescription.name;
            //         if (paramsNamesToSkip != null && paramsNamesToSkip.IndexOf(paramName) != -1) {
            //             continue;
            //         }
            //         if (FootStepsUtils.TryGetTerrainTypeIndex(parameterDescription.name, out var terrainTypeIndex)) {
            //             terrainTypeIndices[terrainTypesCount] = terrainTypeIndex;
            //             terrainTypesCount++;
            //         }
            //     }
            //     fmodParameters = new FMODParameter[reserveIndicesAtArrayStartCount + terrainTypesCount];
            //     for (int i = 0; i < terrainTypesCount; i++) {
            //         fmodParameters[reserveIndicesAtArrayStartCount + i] = new FMODParameter(SurfaceType.TerrainTypes[terrainTypeIndices[i]].FModParameterName, 0);
            //     }
            //     return true;
            // }
            return false;
        }
        public static void AssignParameter(FMODParameter[] fmodParameters, SurfaceType surfaceType, float value) {
            var index = Array.IndexOf(SurfaceType.TerrainTypes, surfaceType);
            if (index == -1) {
                Log.Important?.Error($"Trying to use {surfaceType} as terrain footsteps parameter but it's not assign as footsteps surface type");
            } else {
                fmodParameters[index] = new FMODParameter(surfaceType.FModParameterName, value);
            }
        }
    }
}