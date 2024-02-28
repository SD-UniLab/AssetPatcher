using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UniLab.AssetPatcher {
    [Serializable]
    [CreateAssetMenu(fileName = "Unilab_PatchGroup", menuName = "UniLab/PatchGroup", order = 2)]
    public class PatchGroup : ScriptableObject {
        public List<Patch> ListPatches = new List<Patch>();
    }

    [CustomEditor(typeof(PatchGroup))]
    public class PatchGroupEditor : Editor {
        SerializedProperty ListPatches;

        void OnEnable() {
            ListPatches = serializedObject.FindProperty("ListPatches");
        }

        public override void OnInspectorGUI() {
            var halfSize = GUILayout.MaxWidth((Screen.width-42)/2);
            serializedObject.Update();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test All Patch", halfSize)) {
                for (int i = 0; i < ListPatches.arraySize; i++) {
                    PatchImpl.ApplyPatch((Patch)ListPatches.GetArrayElementAtIndex(i).objectReferenceValue, true);
                }
            }
            if (GUILayout.Button("Apply All Patch", halfSize)) {
                if (EditorUtility.DisplayDialog("Apply Patch?", "This operaion will create backup and overwrite your file.\nContinue?", "Yes", "No")) {
                    for (int i = 0; i < ListPatches.arraySize; i++) {
                        PatchImpl.ApplyPatch((Patch)ListPatches.GetArrayElementAtIndex(i).objectReferenceValue);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(ListPatches);
            serializedObject.ApplyModifiedProperties();
        }
    }
}