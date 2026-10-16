#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.Casino.Cheating.Editor
{
    [CustomEditor(typeof(CheatGroupsLibrary))]
    public class CheatGroupsLibraryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Собрать все CheatGroup из проекта"))
            {
                var guids = AssetDatabase.FindAssets("t:CheatGroup");
                var groups = new List<CheatGroup>(guids.Length);
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    groups.Add(AssetDatabase.LoadAssetAtPath<CheatGroup>(path));
                }

                var library = (CheatGroupsLibrary)target;
                Undo.RecordObject(library, "Collect CheatGroups");
                library.SetGroups(groups);
                EditorUtility.SetDirty(library);
            }
        }
    }
}
#endif