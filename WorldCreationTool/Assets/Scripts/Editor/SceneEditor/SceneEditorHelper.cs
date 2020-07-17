using System.Collections.Generic;
using Editor.SceneEditor.DataTypes;
using UnityEditor;
using UnityEngine;

namespace Editor.SceneEditor {
    public static class SceneEditorHelper {
        public static readonly Texture2D[] ToolbarIcons = new Texture2D[5];
        private static Texture2D _deleteCursor;
        private static Texture2D _undoIcon;
        
        public static void InitIcons(){
            if (ToolbarIcons[0]==null){
                ToolbarIcons[0] = (Texture2D)Resources.Load("paint");
            }
            if (ToolbarIcons[1]==null){
                ToolbarIcons[1] = (Texture2D)Resources.Load("select");
            }
            if (ToolbarIcons[2]==null){
                ToolbarIcons[2] = (Texture2D)Resources.Load("scale");
            }
            if (ToolbarIcons[3]==null){
                ToolbarIcons[3] = (Texture2D)Resources.Load("settings");
            }
            if (ToolbarIcons[4]==null){
                ToolbarIcons[4] = (Texture2D)Resources.Load("info");
            }

            if (_deleteCursor == null){
                _deleteCursor  = (Texture2D)Resources.Load("eraser");
            }

            if (_undoIcon == null) {
                _undoIcon = (Texture2D)Resources.Load("undo");
            }
        }

        public static Texture2D GetUndoIcon() {
            return _undoIcon;
        }

        public static void DrawBrush(SceneEditorPreference preference, SceneObjectBrush brush, Vector3 pos, float radius, Vector3 normal){
            radius = radius<preference.minBrushDrawSize?preference.minBrushDrawSize:radius;
            var gridPos = new Vector3(Mathf.FloorToInt(pos.x) + 0.5f, pos.y, Mathf.FloorToInt(pos.z) + 0.5f);

            if (preference.showBrushSize){
                var corners = new Vector3[preference.brushDetail+1];
                var step = 360f/preference.brushDetail;
                var rot = Quaternion.FromToRotation( Vector3.up,normal);
                

                for (var i=0; i<=corners.Length-1; i++){
                    corners[i] = new Vector3( Mathf.Sin(step*i*Mathf.Deg2Rad), 0, Mathf.Cos(step*i*Mathf.Deg2Rad) ) * radius  + gridPos;
                    var dir = corners[i] - gridPos;
                    dir = rot * dir;
                    corners[i] = dir + gridPos;

                    RaycastHit hit;
                    if (Physics.Raycast(corners[i]+ normal.normalized, -normal,out hit,brush.size/2, brush.pickableLayer)){
                        corners[i] = hit.point;
                    }
                }

                Handles.color = preference.brushColor;
                Handles.DrawAAPolyLine(3, corners);
            }

            if (preference.showCentralDot){
                Handles.color = new Color(preference.brushColor.r,preference.brushColor.g,preference.brushColor.b,0.3f);
                Handles.DrawSolidDisc( gridPos,normal,preference.dotSize);
            }

            if (preference.showNormal){
                Handles.color = preference.normalColor;
                if (brush.align2Surface){
                    Handles.ArrowHandleCap( 0,gridPos,Quaternion.LookRotation( normal,Vector3.up),2, EventType.Repaint);
                }
                else{
                    Handles.ArrowHandleCap( 0,gridPos,Quaternion.LookRotation( Vector3.up,Vector3.up),2, EventType.Repaint);
                }
            }
        }

        public static void DrawDescriptionsOnSceneObjects(List<SceneObjectDataContainer> data) {
            foreach (var sceneObject in data) {
                if (sceneObject.ObjTransform != null) {
                    var label = new GUIStyle("label");
                    label.alignment = TextAnchor.MiddleCenter;
                    label.fontSize = 11;
                    label.fontStyle = FontStyle.Bold;
                    label.normal.textColor = Color.white;
                    var pos = sceneObject.ObjTransform.position + new Vector3(sceneObject.Size.X / 2f, 1f, sceneObject.Size.Y / 2f);
                    Handles.Label(pos, "Id: " + sceneObject.Id.ToString(), label);
                }
            }
        }

        public static GameObject Instantiate(GameObject prefab, Point pos, float offsetY = 0) {
            var go = Object.Instantiate(prefab, new Vector3(pos.X, offsetY, pos.Y), Quaternion.identity);
            go.AddComponent<SceneObjectEditor>();
            go.hideFlags = HideFlags.NotEditable;
            
            return go;
        }
        
        public static GameObject Instantiate(GameObject prefab, Area area, float offsetY = 0) {
            var go = Instantiate(prefab, new Point(area.X, area.Y), offsetY);
            go.transform.localScale = new Vector3(area.Width, 1f, area.Height);
            
            return go;
        }

        public static GameObject InstantiatePrefabLink(GameObject prefab, Point pos, float offsetY = 0) {
            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            go.transform.position = new Vector3(pos.X, offsetY, pos.Y);
            go.AddComponent<SceneObjectEditor>();
            go.hideFlags = HideFlags.NotEditable;
            
            return go;
        }
        
        public static GameObject InstantiatePrefabLink(GameObject prefab, Area area, float offsetY = 0) {
            var go = InstantiatePrefabLink(prefab, new Point(area.X, area.Y), offsetY);
            go.transform.localScale = new Vector3(area.Width, 1f, area.Height);
            
            return go;
        }

        public static void SetOffsetSceneObject(GameObject so, string key) {
            Point size = new Point(0, 0);
            var isFound = false;
            foreach (var soData in DataLoader.GetAllObject()) {
                if (soData.Id == key) {
                    size = new Point(soData.Size.x, soData.Size.y);
                    isFound = true;
                    break;
                }
            }

            if (!isFound) {
                Debug.LogError("Not found Scene Object with Key: " + key);
            } else {
                //TODO: set object offset;
            }
        }
    }
}