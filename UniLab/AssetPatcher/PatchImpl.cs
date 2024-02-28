using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace UniLab.AssetPatcher {
    public class PatchImpl {
        public static void ApplyPatch(Patch patch, bool isTest = false, bool isDebug = false) {
            List<string> content = getFileContent(patch.getPath());
            Debug.Log($"Patching to {patch.getPath()}");
            var pointer = 0;
            for (int i = 0 ; i < patch.ArrOpCode.Length ; i++) {
                var operation = patch.ArrOpCode[i];
                var patchContent = patch.ArrContents[i];
                switch (operation) {
                    case OpCode.Find:
                        var lineIndex = content.FindIndex(pointer, line => line == patchContent);
                        if (lineIndex == -1) {
                            Debug.LogWarning($"Failed to find line from index {i}.");
                            if (isTest) continue;
                            Debug.LogError("Patch aborted!");
                            return;
                        }
                        pointer = lineIndex;
                        if (isDebug) Debug.Log($"Index {i}: Changed pointer to {pointer}.");
                        break;
                    case OpCode.Skip:
                        var skips = int.Parse(patchContent);
                        if ((pointer + skips) > content.Count) {
                            Debug.LogWarning($"Out of index when skip {skips} lines.");
                            if (isTest) continue;
                            Debug.LogError("Patch aborted!");
                            return;
                        }
                        pointer += skips;
                        if (isDebug) Debug.Log($"Index {i}: Changed pointer to {pointer}.");
                        break;
                    case OpCode.Remove:
                        var count = int.Parse(patchContent);
                        if ((pointer + count) > content.Count) {
                            Debug.LogWarning($"Out of index when remove {count} lines.");
                            if (isTest) continue;
                            Debug.LogError("Patch aborted!");
                            return;
                        }
                        while(count-- > 0) content.RemoveAt(pointer);
                        if (isDebug) Debug.Log($"Index {i}: Removed line from {pointer}.");
                        break;
                    case OpCode.Patch:
                        content[pointer] = patchContent;
                        if (isDebug) Debug.Log($"Index {i}: Patched line from {pointer}.");
                        break;
                    case OpCode.Append:
                        content.Insert(pointer, content[pointer]);
                        pointer++;
                        if (isDebug) Debug.Log($"Index {i}: Changed pointer to {pointer}.");
                        content[pointer] = patchContent;
                        if (isDebug) Debug.Log($"Index {i}: Inserted line to {pointer}.");
                        break;
                    case OpCode.Goto:
                        var line = int.Parse(patchContent);
                        if (line < 0 || line > content.Count) {
                            Debug.LogWarning($"Tried to go invalid line: {line}");
                            if (isTest) continue;
                            Debug.LogError("Patch aborted!");
                            return;
                        }
                        pointer = line;
                        break;
                    case OpCode.MarkAsRemove:
                        Debug.LogWarning("Marked patches not removed, try to remove them unless you need.");
                        break;
                }
            }
            if (isTest) return;
            saveFileContent(patch.getPath(), content);
        }

        public static List<string> getFileContent(string path) {
            return new List<string>(File.ReadAllLines(path));
        }

        public static void saveFileContent(string path, List<string> content) {
            File.Copy(path, path + ".bak", true);
            File.WriteAllLines(path, content);
            AssetDatabase.Refresh();
        }
    }
}