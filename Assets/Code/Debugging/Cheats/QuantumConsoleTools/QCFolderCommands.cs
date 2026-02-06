using System;
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
            public int folderCount = 0;
            public readonly List<string> commands = new();
            
            public void AddCommands(IEnumerable<string> commands) {
                this.commands.AddRange(commands);
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
                s_sb.Append(folder.Value.commands.Count);
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
            foreach (var folder in folderData
                                   .OrderBy(f => {
                                        if (f.Key.StartsWith('\'')) return -1;
                                        if (!f.Key.StartsWith(".")) return 0;
                                        return 1;
                                   })
                                   .ThenBy(f => f.Key)) {
                if (odd) {
                    s_sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(ARColor.LightGrey)}>");
                }
                s_sb.Append("     ");
                s_sb.AppendFormat("{0,-35}", folder.Key);
                
                s_sb.Append("(");
                s_sb.Append(folder.Value.commands.Count);
                s_sb.Append(")");
                
                if (folder.Value.folderCount > 0) {
                    s_sb.Append(" [");
                    s_sb.Append(folder.Value.folderCount);
                    s_sb.Append("]");
                }

                string enteredFolder = null;
                foreach (string command in folder.Value.commands) {
                    s_sb.AppendLine();
                    
                    int lastSeparator = command.LastIndexOf(FolderSeparator);
                    if (lastSeparator > 0) {
                        if (enteredFolder == null || !command.StartsWith(enteredFolder)) {
                            enteredFolder = command[..lastSeparator];
                            s_sb.Append("        ");
                            s_sb.Append("<b>");
                            s_sb.Append(enteredFolder);
                            s_sb.Append("</b>");
                            s_sb.AppendLine();
                        }
                        s_sb.Append("   ");
                    } else {
                        enteredFolder = null;
                    }
                    
                    s_sb.Append("      - ");
                    s_sb.Append(command.AsSpan(lastSeparator + 1));
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
            //                                      .Where(IsInsideTargetFolder)
            //                                      .Select(c => {
            //                                          int lastFolderSeparator = c.LastIndexOf(FolderSeparator);
            //                                          string folder = lastFolderSeparator > 0 ? c[..lastFolderSeparator] : "";
            //                                          
            //                                          int folderLength = folder.Length;
            //                                          int toRemove = folderLength > 0 
            //                                                             ? folderLength + 1 
            //                                                             : 0; // +1 for the separator
            //                                          return (folder, command: c.Remove(0, toRemove));
            //                                      }).GroupBy(command => command.folder);
            //                                      
            //
            // foreach (var group in grouped) {
            //     // The root folder inside the ParentFolder we are displaying
            //     string folder;
            //
            //     // Is inside ParentFolder but in a subfolder
            //     int subFolderCount = group.Key.Count(static s => s == FolderSeparator);
            //     
            //     if (group.Key == parentFolder) {
            //         // Inside the folder we are browsing
            //         folder = "'Commands here: ";
            //     } else if (parentFolder.IsNullOrWhitespace()) {
            //         folder = group.Key;
            //     } else {
            //         int indexToCut = group.Key.IndexOf(parentFolder, StringComparison.Ordinal) + parentFolder.Length + 1;
            //         folder = group.Key[indexToCut..];
            //     }
            //
            //     if (subFolderCount > 0) {
            //         // We want all commands to be shown in their root folder
            //         folder = folder[..folder.IndexOf(FolderSeparator)];
            //     }
            //
            //     // save data
            //     if (!folderData.TryGetValue(folder, out var data)) {
            //         folderData[folder] = data = new FolderData();
            //     }
            //
            //     if (subFolderCount == 0) {
            //         data.AddCommands(group.Select(pair => pair.command));
            //         continue;
            //     }
            //     data.folderCount++;
            //     
            //     int prefixToRemove = folder.Length + 1; // +1 for the separator
            //     data.AddCommands(group.Select(pair => pair.folder[prefixToRemove..] + FolderSeparator + pair.command));
            // }
            // return;
            //
            // bool IsInsideTargetFolder(string command) {
            //     if (parentFolder.IsNullOrWhitespace()) {
            //         return true;
            //     }
            //     int lastIndexOf = command.LastIndexOf(FolderSeparator);
            //     if (lastIndexOf < 0) {
            //         // No folder, just a command
            //         return false;
            //     }
            //     var split = command[..lastIndexOf];
            //
            //     return split.StartsWith(parentFolder);
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