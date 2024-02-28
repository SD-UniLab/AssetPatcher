using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace UniLab.AssetPatcher {
    public enum OpCode {
        Find = 'F',
        Skip = 'S',
        Remove = 'R',
        Patch = 'P',
        Append = 'A',
        Goto = 'G',
        MarkAsRemove = 'X'
    }

    public enum ContentType {
        None = 0,
        Integer = 1,
        String = 2,
    }

    [Serializable]
    [CreateAssetMenu(fileName = "Unilab_Patch", menuName = "UniLab/Patch", order = 1)]
    public class Patch : ScriptableObject {
        public string PatchTarget;
        public OpCode[] ArrOpCode;
        public string[] ArrContents;

        public string getPath() {
            var tryGuid = AssetDatabase.GUIDToAssetPath(PatchTarget);
            if (String.IsNullOrEmpty(tryGuid)) return PatchTarget;
            return tryGuid;
        }

        public static ContentType getAllowedType(OpCode operation) {
            switch (operation) {
                case OpCode.Find:
                case OpCode.Patch:
                case OpCode.Append:
                case OpCode.MarkAsRemove:
                    return ContentType.String;
                case OpCode.Skip:
                case OpCode.Goto:
                case OpCode.Remove:
                    return ContentType.Integer;
                default:
                    return ContentType.None;
            }
        }

        public static bool isAllowed(OpCode operation, string content) {
            var allowed = getAllowedType(operation);
            switch (allowed) {
                case ContentType.None:
                    return String.IsNullOrEmpty(content);
                case ContentType.Integer:
                    return int.TryParse(content, out _);
                case ContentType.String:
                    return true;
                default:
                    return false;
            }
        }
    }

    [CustomEditor(typeof(Patch))]
    public class PatchEditor : Editor {
        SerializedProperty PatchTarget;
        SerializedProperty ArrOpCode;
        SerializedProperty ArrContents;

        private Vector2 scrollPos = Vector2.zero;

        void OnEnable() {
            PatchTarget = serializedObject.FindProperty("PatchTarget");
            ArrOpCode = serializedObject.FindProperty("ArrOpCode");
            ArrContents = serializedObject.FindProperty("ArrContents");
        }

        public override void OnInspectorGUI() {
            var halfSize = GUILayout.MaxWidth((Screen.width-42)/2);
            serializedObject.Update();
            EditorGUILayout.PropertyField(PatchTarget);
            GUILayout.Label("Commands");
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+", halfSize)) {
                    var size = ArrOpCode.arraySize;
                    ArrOpCode.InsertArrayElementAtIndex(size);
                    if (size > 0) {
                        ArrOpCode.GetArrayElementAtIndex(size).intValue = ArrOpCode.GetArrayElementAtIndex(size-1).intValue;
                    } else {
                        ArrOpCode.GetArrayElementAtIndex(size).intValue = 'F';
                    }
                    ArrContents.InsertArrayElementAtIndex(size);
                    ArrContents.GetArrayElementAtIndex(size).stringValue = "";
                }
                if (GUILayout.Button("-", halfSize)) {
                    var size = ArrOpCode.arraySize;
                    var i = 0;
                    while(i < size) {
                        if ((OpCode)ArrOpCode.GetArrayElementAtIndex(i).intValue == OpCode.MarkAsRemove) {
                            ArrOpCode.DeleteArrayElementAtIndex(i);
                            ArrContents.DeleteArrayElementAtIndex(i);
                            size--;
                        } else {
                            i++;
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Check Contents", halfSize))  {
                    var invalidCount = 0;
                    for (int i=0; i<ArrOpCode.arraySize ; i++) {
                        var operation = (OpCode)ArrOpCode.GetArrayElementAtIndex(i).intValue;
                        var contents = ArrContents.GetArrayElementAtIndex(i).stringValue;
                        if (Patch.isAllowed(operation, contents)) continue;
                        invalidCount++;
                        Debug.LogWarning($"Index {i} has invalid content. Operation: {operation}, Expected Type: {Patch.getAllowedType(operation)}");
                    }
                    if (invalidCount > 0) {
                        Debug.LogWarning($"Found {invalidCount} invalid contents.");
                    } else {
                        Debug.Log("Invalid contents not found.");
                    }
                    if (File.Exists(((Patch)serializedObject.targetObject).getPath())) {
                        Debug.Log("Target found.");
                    } else {
                        Debug.LogWarning("Target not found.");
                    }
                    
                }
                if (GUILayout.Button("Test Patch", halfSize)) {
                    PatchImpl.ApplyPatch((Patch)serializedObject.targetObject, true);
                }
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Apply Patch", GUILayout.MaxWidth(Screen.width - 40))) {
                if (EditorUtility.DisplayDialog("Apply Patch?", "This operaion will create backup and overwrite your file.\nContinue?", "Yes", "No")) {
                    PatchImpl.ApplyPatch((Patch)serializedObject.targetObject);
                }
            }
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true, GUIStyle.none, GUI.skin.GetStyle("verticalScrollbar"), EditorStyles.helpBox,
                GUILayout.MaxHeight(Screen.height - 220), GUILayout.MaxWidth(Screen.width - 40));
            {
                for (int i=0; i<ArrOpCode.arraySize ; i++) {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(i.ToString(), GUILayout.MaxWidth(30));
                    var operation = (OpCode)EditorGUILayout.EnumPopup((OpCode)ArrOpCode.GetArrayElementAtIndex(i).intValue, GUILayout.MaxWidth(120));
                    ArrOpCode.GetArrayElementAtIndex(i).intValue = (int)operation;
                    var contents = EditorGUILayout.TextField(ArrContents.GetArrayElementAtIndex(i).stringValue);
                    ArrContents.GetArrayElementAtIndex(i).stringValue = contents;
                    if (GUILayout.Button("▲", GUILayout.MaxWidth(22))) {
                        if (i > 0) {
                            ArrOpCode.GetArrayElementAtIndex(i).intValue = ArrOpCode.GetArrayElementAtIndex(i-1).intValue;
                            ArrOpCode.GetArrayElementAtIndex(i-1).intValue = (int)operation;
                            ArrContents.GetArrayElementAtIndex(i).stringValue = ArrContents.GetArrayElementAtIndex(i-1).stringValue;
                            ArrContents.GetArrayElementAtIndex(i-1).stringValue = contents;
                        }
                    }
                    if (GUILayout.Button("▼", GUILayout.MaxWidth(22))) {
                        if (i < ArrOpCode.arraySize - 1) {
                            ArrOpCode.GetArrayElementAtIndex(i).intValue = ArrOpCode.GetArrayElementAtIndex(i+1).intValue;
                            ArrOpCode.GetArrayElementAtIndex(i+1).intValue = (int)operation;
                            ArrContents.GetArrayElementAtIndex(i).stringValue = ArrContents.GetArrayElementAtIndex(i+1).stringValue;
                            ArrContents.GetArrayElementAtIndex(i+1).stringValue = contents;
                        }
                    }
                    GUILayout.Space(20);
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
            serializedObject.ApplyModifiedProperties();
        }
    }
}