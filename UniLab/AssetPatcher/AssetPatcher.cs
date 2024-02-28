using UnityEngine;
using UnityEditor;

namespace UniLab.AssetPatcher {
    public class AssetPatcher : EditorWindow {
        [MenuItem("UniLab/AssetPatcher")]
        static void Init() {
            AssetPatcher window = (AssetPatcher)EditorWindow.GetWindow(typeof(AssetPatcher));
            window.Show();
        }

        void OnGUI() {
            EditorGUILayout.ObjectField("", null, typeof(PatchGroup), true);
        }
    }
}