using System;
using System.Collections;
using System.IO;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace Editor.SceneEditor {
    [Serializable]
    public class SceneEditorGuiTools {
        public static void SimpleTitle(string text) {
            var labelStyle = new GUIStyle("label");
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.fontSize = 11;
            EditorGUILayout.LabelField(text, labelStyle);
        }

        public static void SimpleText(string text) {
            var labelStyle = new GUIStyle("label");
            labelStyle.fontStyle = FontStyle.Normal;
            labelStyle.fontSize = 11;
            EditorGUILayout.LabelField(text, labelStyle);
        }

        public static void BeginGroup(int padding = 0) {
            GUILayout.BeginHorizontal();
            GUILayout.Space(padding);
            GUILayout.BeginVertical();
            GUILayout.Space(2f);
        }

        public static void EndGroup(bool endSpace = true) {
            GUILayout.Space(3f);
            GUILayout.EndVertical();
            GUILayout.Space(3f);
            GUILayout.EndHorizontal();

            if (endSpace) {
                GUILayout.Space(10f);
            }
        }

        public static bool ToggleButton(bool state,PreviewObject objectData,  ref Texture2D preview,GameObject prefab=null){
            GUIContent content = new GUIContent();
            if ( preview ==null){
                preview = AssetPreview.GetAssetPreview( prefab);
                content.image = preview;
                content.text = objectData.SceneObjectKey;
                state = GUILayout.Toggle( state,content,new GUIStyle("Button"),GUILayout.Height(80) ); 
            }
            else {
                var style = new GUIStyle("Button");
                style.fontStyle = FontStyle.Normal;
                style.fontSize = 12;
                style.alignment = TextAnchor.MiddleLeft;
                content.image = preview;
                content.text = $"Id:\t{objectData.SceneObjectKey}";
                state = GUILayout.Toggle(state,content,style, GUILayout.Height(80)); 
            }

            return state;
        }

        public static bool Button(string label, Color color, int width, int height = 0, Texture2D icon = null) {
            GUI.backgroundColor = color;

            var guiContent = new GUIContent(label, icon);

            if (height == 0) {
                if (GUILayout.Button(guiContent, GUILayout.Width(width))) {
                    GUI.backgroundColor = Color.white;
                    return true;
                }
            } else {
                if (GUILayout.Button(guiContent, GUILayout.ExpandWidth(true), GUILayout.Height(height))) {
                    GUI.backgroundColor = Color.white;
                    return true;
                }
            }

            GUI.backgroundColor = Color.white;

            return false;
        }
        
        private static IEnumerator Write(string result) {
            File.WriteAllText(Application.dataPath + "/Configuration/Resources/DB_Scenes.json", result);
            yield return null;
        }

        public static void WriteToFile(string result) {
            TMP_EditorCoroutine.StartCoroutine(Write(result));
        }
    }
}
