#if UNITY_PS5
using Awaken.Utility.Debugging;
using Unity.Mathematics;
using Unity.SaveData.PS5.Convert;
using Unity.SaveData.PS5.Core;
using Unity.SaveData.PS5.Dialog;
using Unity.SaveData.PS5.Mount;
using Unity.SaveData.PS5.Search;
using UnityEngine;

namespace Awaken.TG.Main.Saving.Cloud.Services.Sony {
    public partial class SonyCloudService {
        static void InsufficientStorageSpacePopup() {
            var request = new Dialogs.OpenDialogRequest {
                UserId = Sony.SonyCloudService.PrimaryUserId,
                IgnoreCallback = true,
                Async = false,
                DispType = Dialogs.DialogType.Save,
                Mode = Dialogs.DialogMode.SystemMsg,
                Option = new Dialogs.OptionParam {
                    Flag = Dialogs.OptionFlag.Default,
                    Back = Dialogs.OptionBack.Disable
                },
                SystemMessage = new Dialogs.SystemMessageParam {
                    SysMsgType = Dialogs.SystemMessageType.NoSpace
                },
            };

            var response = new Dialogs.OpenDialogResponse();
            Dialogs.OpenDialog(request, response);
            if (response.IsErrorCode) {
                Log.Critical?.Error($"SaveData [InsufficientStorageSpacePopup] error: {response.ReturnCode}");
            }
        }

        /// <summary>
        /// Calculates how many blocks save slot requires for mount.
        /// </summary>
        /// <param name="dataLength">Number of bytes we are trying to save to a slot</param>
        /// <param name="dir"><see cref="DirName" value="DirName"/> of a Save Slot</param>
        /// <returns></returns>
        static ulong GetSaveBlocks(long dataLength, string dir) {
            return 256; //TODO: remove when fixed
            ulong blocks;
            var searchRequest = new Searching.DirNameSearchRequest {
                UserId = PrimaryUserId,
                IgnoreCallback = true,
                Async = false,
                DirName = new DirName { Data = dir },
                IncludeBlockInfo = true,
            };
            var searchResponse = new Searching.DirNameSearchResponse();
            Searching.DirNameSearch(searchRequest, searchResponse);
            ulong dataBlocks = (ulong)Mathf.CeilToInt(dataLength * 1.1f / Mounting.MountRequest.BLOCK_SIZE);
            if (searchResponse is { IsErrorCode: false, HasInfo: true, SaveDataItems: { Length: > 0 } }) {
                var info = searchResponse.SaveDataItems[0].Info;
                if (info.Blocks > dataBlocks) {
                    blocks = info.Blocks;
                } else {
                    blocks = dataBlocks + MetadataAreaBlocks;
                    Clamp(ref blocks);
                    ConvertMountPoint(searchResponse.SaveDataItems[0].DirName.Data, blocks);
                }
            } else {
                blocks = math.max(dataBlocks + MetadataAreaBlocks, Mounting.MountRequest.BLOCKS_MIN);
                Clamp(ref blocks);
            }

            return blocks;

            static void Clamp(ref ulong blocks) {
                blocks = math.clamp(blocks, Mounting.MountRequest.BLOCKS_MIN, Mounting.MountRequest.BLOCKS_MAX);
            }
        }
        
        /// <summary>
        /// Changes save slot number of blocks and/or it's name
        /// </summary>
        /// <param name="dir"><see cref="DirName" value="DirName"/> of a Save Slot</param>
        /// <param name="blocks">Number of blocks to change to</param>
        /// <param name="destinationDir">If null name won't be changed. Name must be a valid <see cref="DirName" value="DirName"/> and contain only characters allowed by SaveData system</param>
        static void ConvertMountPoint(string dir, ulong blocks, string destinationDir = null) {
            bool retry;
            do {
                retry = false;
                var convertRequest = new Conversion.ConvertRequest {
                    UserId = PrimaryUserId,
                    IgnoreCallback = true,
                    Async = false,
                    SourceDirName = new DirName { Data = dir },
                    DestinationDirName = new DirName { Data = destinationDir },
                    DestinationBlocks = blocks
                };
                var convertResponse = new Conversion.ConvertResponse();
                Conversion.Convert(convertRequest, convertResponse);
                if (convertResponse.IsErrorCode) {
                    if (convertResponse.Exception == null) {
                        Log.Critical?.Error($"SaveData conversion error: {convertResponse.ReturnCode}");
                    } else {
                        Log.Critical?.Error($"SaveData conversion error: {convertResponse.ReturnCode}", null, LogOption.NoStacktrace);
                        Debug.LogException(convertResponse.Exception);
                    }
                    
                    if (convertResponse.ReturnCode is ReturnCodes.DATA_ERROR_NO_SPACE_FS) {
                        InsufficientStorageSpacePopup();
                        retry = true;
                        continue;
                    }
                }
            } while (retry);
        }
    }
}
#endif