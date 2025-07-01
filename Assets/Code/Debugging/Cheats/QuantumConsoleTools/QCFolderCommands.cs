using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using QFSW.QC;
using UnityEngine;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCFolderCommands {
        const char FolderSeparator = '.';
        static readonly StringBuilder s_sb = new();
        
        class FolderData {
            public int CommandCount { get; private set; }
            public int folderCount = 0;
            public readonly List<string> commands = new();
            
            public void AddCommands(IEnumerable<string> commands) {
                this.commands.AddRange(commands);
                CommandCount = this.commands.Count;
            }
        }

        [Command("list", "Lists all folders inside the given folder")][UnityEngine.Scripting.Preserve]
        static void FolderTree([FolderCommands] string parentFolder = "") {
            s_sb.AppendLine($"Listing folders inside '{(parentFolder.IsNullOrWhitespace() ? "root" : parentFolder)}'".ColoredText(ARColor.EditorBlue));
            s_sb.AppendLine("() = command count, [] = subfolder count");
            s_sb.AppendLine("========================================".ColoredText(ARColor.EditorBlue));
            
            Dictionary<string, FolderData> folderData = new();

            GatherFolderData(parentFolder, folderData);

            bool odd = true;
            foreach (var folder in folderData.OrderBy(f => f.Key)) {
                if (odd) {
                    s_sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(ARColor.LightGrey)}>");
                }
                s_sb.Append("   - ");
                s_sb.AppendFormat("{0,-25}", folder.Key);
                
                s_sb.Append("(");
                s_sb.Append(folder.Value.CommandCount);
                s_sb.Append(")");
                
                if (folder.Value.folderCount > 0) {
                    s_sb.Append(" [");
                    s_sb.Append(folder.Value.folderCount);
                    s_sb.Append("]");
                }
                if (odd) {
                    s_sb.Append("</color>");
                }
                s_sb.AppendLine();
                odd = !odd;
            }

            QuantumConsole.Instance.LogToConsoleAsync(s_sb.ToString());
            s_sb.Clear();
        }
        
        [Command("commands", "Lists all folders and commands inside the given folder")][UnityEngine.Scripting.Preserve]
        static void CommandTree([FolderCommands] string parentFolder = "") {
            s_sb.AppendLine($"Listing folders and commands inside '{(parentFolder.IsNullOrWhitespace() ? "root" : parentFolder)}'".ColoredText(ARColor.EditorBlue));
            s_sb.AppendLine("() = command count, [] = subfolder count");
            s_sb.AppendLine("========================================".ColoredText(ARColor.EditorBlue));
            
            Dictionary<string, FolderData> folderData = new();

            GatherFolderData(parentFolder, folderData);

            bool odd = true;
            foreach (var folder in folderData.OrderBy(f => f.Key)) {
                if (odd) {
                    s_sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(ARColor.LightGrey)}>");
                }
                s_sb.Append("   - ");
                s_sb.AppendFormat("{0,-25}", folder.Key);
                
                s_sb.Append("(");
                s_sb.Append(folder.Value.CommandCount);
                s_sb.Append(")");
                
                if (folder.Value.folderCount > 0) {
                    s_sb.Append(" [");
                    s_sb.Append(folder.Value.folderCount);
                    s_sb.Append("]");
                }

                foreach (string command in folder.Value.commands) {
                    int lastFolderSeparator = command.LastIndexOf(FolderSeparator);
                    var commandOnly = lastFolderSeparator > 0 ? command[lastFolderSeparator..] : command;
                    s_sb.AppendLine();
                    s_sb.Append("      - ");
                    s_sb.Append(commandOnly);
                }
                
                if (odd) {
                    s_sb.Append("</color>");
                }
                s_sb.AppendLine();
                odd = !odd;
            }

            QuantumConsole.Instance.LogToConsoleAsync(s_sb.ToString());
            s_sb.Clear();
        }

        static void GatherFolderData(string parentFolder, Dictionary<string, FolderData> folderData) {
            // Gather all commands and group them by folder
            // var grouped = QuantumConsoleProcessor.UniqueUserCommandNames()
            //                                      .Where(command => command.Contains(parentFolder))
            //                                      .GroupBy(command => {
            //                                          // Group by folder
            //                                          int lastFolderSeparator = command.LastIndexOf(FolderSeparator);
            //                                          return lastFolderSeparator > 0 ? command[..lastFolderSeparator] : "";
            //                                      });
            //
            // foreach (var group in grouped) {
            //     // The root folder inside the ParentFolder we are displaying
            //     string folder;
            //     // Is inside ParentFolder but in a subfolder
            //     bool isSubFolder = false;
            //
            //     if (group.Key == parentFolder) {
            //         // Inside the folder we are browsing
            //         folder = "'Commands here: ";
            //     } else if (parentFolder.IsNullOrWhitespace()) {
            //         // Display all root folders
            //         string[] split = group.Key.Split(FolderSeparator);
            //         folder = split[0];
            //         // Child folder inside of folder
            //         isSubFolder = split.Length > 1;
            //     } else {
            //         // Ignore folder structure above parentFolder
            //         string[] strings = group.Key.Split(parentFolder + FolderSeparator);
            //         string[] split = strings[1].Split(FolderSeparator);
            //         // Folder inside of parentFolder
            //         folder = split[0];
            //         // Child folder inside of folder
            //         isSubFolder = split.Length > 1;
            //     }
            //
            //     // save data
            //     if (!folderData.TryGetValue(folder, out var data)) {
            //         folderData[folder] = data = new FolderData();
            //     }
            //
            //     data.AddCommands(group);
            //     if (isSubFolder) {
            //         data.folderCount++;
            //     }
            // }
        }

        public static IEnumerable<string> AllFolders() {
            return Enumerable.Empty<string>();
            // return QuantumConsoleProcessor.UniqueUserCommandNames()
            //                               .Select(c => c.Split(FolderSeparator))
            //                               .Select(f => f[..^1])
            //                               .SelectMany(f => f)
            //                               .Distinct();
        }
    }
}