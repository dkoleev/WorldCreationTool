using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.SceneEditor.Commands;
using Editor.SceneEditor.DataTypes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Editor.SceneEditor {
    public class SceneEditorWindow : EditorWindow {
        private enum EditMode {
            NotSelect = -1,
            SceneObjects = 0,
            Expansions = 1,
            ZonesAndLockedTiles = 2,
            Settings = 3,
            Info = 4
        }

        private enum ZoneLockedTailsMode {
            LockedTails,
            Zones
        }

        private readonly string _sceneObjectsPath = "SceneObjects";
        private readonly string _scenePath = "Scenes";
        private readonly string _expansionAndTailPath = "Scenes";
        private readonly string _sceneObjectsDataPath = "DB_SceneObjects";
        private readonly string _sceneDataPath = "DB_Scenes";

        private GameObject _currentScene;
        private ScenesRegistry _data;
        private readonly List<PreviewObject> _previewSceneObjects = new List<PreviewObject>();
        private PreviewObject _previewExpansion = new PreviewObject();
        private PreviewObject _previewLockedTail = new PreviewObject();
        private Dictionary<string, PreviewObject> _previewZones = new Dictionary<string, PreviewObject>();

        [SerializeField]
        private SceneEditorPreference preference;

        [SerializeField]
        private SceneObjectBrush brush = new SceneObjectBrush();

        private SceneObjectBrush.AlignVector selectionAlignVector;
        private EditMode _currentMode = EditMode.NotSelect;
        private ZoneLockedTailsMode _currentZoneLockedTailsMode = ZoneLockedTailsMode.LockedTails;
        private Transform LastTransform;
        private bool isShift = false;
        private bool isAlt = false;
        private bool isCtrl = false;
        private bool _showZones;
        private bool _showLockedTiles;
        private bool _showSceneObjects = true;
        private bool _showExpansions = true;

        private readonly List<SceneObjectDataContainer> _currentSceneObjects = new List<SceneObjectDataContainer>();
        private readonly List<SceneObjectDataContainer> _newSceneObjects = new List<SceneObjectDataContainer>();

        private readonly List<SceneObjectDataContainer> _sceneObjectsDataForDelete =
            new List<SceneObjectDataContainer>();

        private readonly List<SceneObjectDataContainer> _currentExpansions = new List<SceneObjectDataContainer>();
        private readonly List<SceneObjectDataContainer> _newExpansions = new List<SceneObjectDataContainer>();
        private readonly List<SceneObjectDataContainer> _expansionsDataForDelete = new List<SceneObjectDataContainer>();
        private Vector2Int _currentExpansionSize = new Vector2Int(4, 4);

        private readonly List<SceneObjectDataContainer> _currentLockedTails = new List<SceneObjectDataContainer>();
        private readonly List<SceneObjectDataContainer> _newLockedTails = new List<SceneObjectDataContainer>();
        private readonly List<SceneObjectDataContainer> _lockedTailsForDelete = new List<SceneObjectDataContainer>();

        private readonly Dictionary<GameObject, SceneObjectDataContainer> _currentZones =
            new Dictionary<GameObject, SceneObjectDataContainer>();

        private bool _haveChanges;
        private Vector2 scrollView = Vector2.zero;
        private Vector3 _dragOffset;
        private SceneObjectType _typeSo;
        private KeyCode _prevKey;
        private SceneObjectDataContainer _currentMovingObject;

        private int _selectedSceneObjectsFlags = -1;
        private int _selectedSceneObjectsOnSceneFlags = -1;
        private List<string> _sceneObjectTypesShownOnScene = new List<string>();
        private readonly string[] _selectedTypesOfSceneObjects = Enum.GetNames(typeof(SceneObjectType));
        private string _findSceneObjectText;

        private readonly List<Command> _oldCommandsSceneObjects = new List<Command>();
        private readonly List<Command> _oldCommandsExpansions = new List<Command>();
        private readonly List<Command> _oldCommandsLockedTails = new List<Command>();
        private readonly List<Command> _oldCommandsZones = new List<Command>();
        private Command _undoCommandSceneObjects;
        private Command _undoCommandExpansions;
        private Command _undoCommandLockedTails;
        private Command _undoCommandZones;

        private bool _autoSave;

        [MenuItem("GameGarden/Scene Editor")]
        public static void ShowWindow() {
            var window = (SceneEditorWindow) GetWindow(typeof(SceneEditorWindow));
            window.titleContent = new GUIContent("Scene Editor");
            window.Show();
        }

        private void OnEnable() {
            SceneEditorHelper.InitIcons();
            titleContent = new GUIContent("Scene Editor");
            minSize = new Vector2(325, 100);
            preference = new SceneEditorPreference();
            preference.LoadPreference();
            _autoSave = preference.autoSave;

            _undoCommandSceneObjects = new UndoCommand(_oldCommandsSceneObjects);
            _undoCommandExpansions = new UndoCommand(_oldCommandsExpansions);
            _undoCommandLockedTails = new UndoCommand(_oldCommandsLockedTails);
            _undoCommandZones = new UndoCommand(_oldCommandsZones);

            PrepareStartWorkspace();
            SceneView.duringSceneGui += SceneGui;
        }

        private void OnDisable() {
            ClearScene();
            SceneView.duringSceneGui -= SceneGui;
        }

        private void PrepareStartWorkspace() {
            if (_currentScene == null) {
                _newSceneObjects.Clear();
                _currentSceneObjects.Clear();
                _newExpansions.Clear();
                _currentExpansions.Clear();
                _newLockedTails.Clear();
                _currentLockedTails.Clear();
                _currentZones.Clear();

                LoadData();

                LoadExpansionPrefab();
                LoadLockedTailPrefabPrefab();
                LoadZonePrefabPrefab();
                LoadSceneObjects();
                LoadScene();
                
                FixDuplicateIds();
            }
        }

        #region Load

        private void LoadData() {
            LoadSceneObjectsData();
            LoadSceneData();
        }

        private void LoadSceneObjectsData() {
            DB.Load();
        }

        private void LoadSceneData() {
            var asset = Resources.Load<TextAsset>(_sceneDataPath);
            _data = JsonConvert.DeserializeObject<ScenesRegistry>(asset.text, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.Auto,
                Converters = new List<JsonConverter> {
                    new StringEnumConverter()
                }
            });
        }

        private void LoadSceneObjects() {
            foreach (var item in DB.instance.GameData.GetSceneObjects()) {
                var prefab = Resources.Load<GameObject>(_sceneObjectsPath + "/" + item.Prefab);
                if (prefab == null) {
                    Debug.LogError($"Can't find prefab for {item.Id}");
                } else {
                    CreateSceneObjectPreview(prefab, item.Id, item, null, EditMode.SceneObjects);
                }
            }
        }

        private void LoadExpansionPrefab() {
            var prefab = Resources.Load<GameObject>(_expansionAndTailPath + "/ExpansionFrame");
            var data = new SceneExpansionData {
                Area = new Area {Height = _currentExpansionSize.y, Width = _currentExpansionSize.x, X = 0, Y = 0}
            };
            CreateSceneObjectPreview(prefab, null, null, data, EditMode.Expansions);
		}

		private void LoadLockedTailPrefabPrefab() {
            var prefab = Resources.Load<GameObject>(_expansionAndTailPath + "/LockedTailFrame");
            CreateSceneObjectPreview(prefab, null, null, null, EditMode.ZonesAndLockedTiles,
                ZoneLockedTailsMode.LockedTails);
        }

        private void LoadZonePrefabPrefab() {
            foreach (var zoneData in _data.Scenes[_data.DefaultScene].Zones) {
                var prefab = Resources.Load<GameObject>(_expansionAndTailPath + "/" + zoneData.Key + "ZoneFrame");
                CreateSceneObjectPreview(prefab, null, null, null, EditMode.ZonesAndLockedTiles,
                    ZoneLockedTailsMode.Zones, zoneData.Key);
            }
        }

        private void LoadScene() {
            var prefab = Resources.Load<GameObject>(_scenePath + "/" + _data.DefaultScene);

            _currentScene = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            _currentScene.hideFlags = HideFlags.NotEditable;

            foreach (var startObject in _data.Scenes[_data.DefaultScene].StartObjects) {
                var soPrefab = _previewSceneObjects.FindAll(x => x.SceneObjectData.Id == startObject.SceneObject)
                    .FirstOrDefault();
                //var soPrefab = Resources.Load<GameObject>(_sceneObjectsPath + "/" + startObject.SceneObject);
                var go = SceneEditorHelper.InstantiatePrefabLink(soPrefab.prefab, startObject.Position);
                SceneEditorHelper.SetOffsetSceneObject(go, startObject.SceneObject);
                var data = DB.instance.GameData.GetSceneObject(startObject.SceneObject);
                _currentSceneObjects.Add(new SceneObjectDataContainer(startObject.Id, startObject.SceneObject,
                    startObject.Position, data.Size.ToPoint(), go.transform, soPrefab));
                
                SceneObjectView.AddCollider(go.transform.Find("Offset"),  data.Size.ToPoint());
            }
        }

		#endregion

		#region Save

		private void SaveScene() {
            SaveSceneObjects();
            SaveExpansions();
            SaveLockedTails();
            SaveZones();

            var result = JsonConvert.SerializeObject(_data, Formatting.Indented,
                new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.Auto
                });

            File.WriteAllText(Application.dataPath + "/Configuration/Resources/DB_Scenes.json", result);
            AssetDatabase.Refresh();
        }

        private IEnumerator Write(string result) {
            File.WriteAllText(Application.dataPath + "/Configuration/Resources/DB_Scenes.json", result);
            yield return null;
        }

        private int GetHighestId() {
            var highestId = int.MinValue;
            foreach (var so in _currentSceneObjects) {
                if (so.Id > highestId) {
                    highestId = so.Id;
                }
            }

            foreach (var so in _newSceneObjects) {
                if (so.Id > highestId) {
                    highestId = so.Id;
                }
            }

            if (highestId < 0) {
                highestId = 1000;
            }
            return highestId;
        }
        
        private void SaveSceneObjects() {
            var lastSoId = GetHighestId();

            foreach (var sceneObject in _currentSceneObjects) {
                if (sceneObject.IsModified()) {
                    var dataSo = _data.Scenes[_data.DefaultScene].StartObjects.Find(x => x.Id == sceneObject.Id);
                    dataSo.Position = sceneObject.Position;
                    sceneObject.ApplyChanges();
                }
            }

            foreach (var objectData in _sceneObjectsDataForDelete) {
                _data.Scenes[_data.DefaultScene].StartObjects.RemoveAll(x => x.Id == objectData.Id);
            }

            _sceneObjectsDataForDelete.Clear();

            foreach (var scatteredObject in _newSceneObjects) {
                var soData = new StartSceneObject {
                    Id = ++lastSoId,
                    SceneObject = scatteredObject.Key,
                    Position = scatteredObject.Position
                };

                _data.Scenes[_data.DefaultScene].StartObjects.Add(soData);

                scatteredObject.Id = soData.Id;
                _currentSceneObjects.Add(scatteredObject);
            }

            //_data.Scenes[_data.DefaultScene].LastSceneObjectId = lastSoId;
            _newSceneObjects.Clear();

            _oldCommandsSceneObjects.Clear();
        }

        private void SaveExpansions() {
            var lastId = _data.Scenes[_data.DefaultScene].LastExpansionId;

            foreach (var sceneObject in _currentExpansions) {
                if (sceneObject.IsModified()) {
                    var dataSo = _data.Scenes[_data.DefaultScene].Expansions[sceneObject.Key];
                    dataSo.Area = new Area {
                        Width = sceneObject.Size.X,
                        Height = sceneObject.Size.Y,
                        X = sceneObject.Position.X,
                        Y = sceneObject.Position.Y
                    };
                    sceneObject.ApplyChanges();
                }
            }

            foreach (var objectData in _expansionsDataForDelete) {
                _data.Scenes[_data.DefaultScene].Expansions.Remove(objectData.Key);
            }

            _expansionsDataForDelete.Clear();

            foreach (var expansion in _newExpansions) {
                var expData = new SceneExpansionData {
                    Id = ++lastId,
                    Area = new Area {
                        Width = expansion.Size.X,
                        Height = expansion.Size.Y,
                        X = expansion.Position.X,
                        Y = expansion.Position.Y
                    },
                };

                _data.Scenes[_data.DefaultScene].Expansions.Add(expData.Id.ToString(), expData);

                expansion.Id = expData.Id;
                expansion.Key = expData.Id.ToString();
                _currentExpansions.Add(expansion);
            }

            _data.Scenes[_data.DefaultScene].LastExpansionId = lastId;
            _newExpansions.Clear();

            _oldCommandsExpansions.Clear();
        }

		public Dictionary<Transform, int> GetCurrentExpansionsIds()
		{
			Dictionary<Transform, int> ids = new Dictionary<Transform, int>();

			foreach(SceneObjectDataContainer e in _currentExpansions)
			{
				ids.Add(e.ObjTransform, e.Id);
			}
			return ids;
		}

        private void SaveLockedTails() {
            var data = _data.Scenes[_data.DefaultScene].LockedTails;

            foreach (var objectData in _lockedTailsForDelete) {
                data.RemoveAll(x => x.X == objectData.Position.X && x.Y == objectData.Position.Y);
            }

            _lockedTailsForDelete.Clear();

            foreach (var newLockedTail in _newLockedTails) {
                if (!data.Contains(newLockedTail.Position)) {
                    data.Add(newLockedTail.Position);
                    _currentLockedTails.Add(newLockedTail);
                }
            }

            _newLockedTails.Clear();

            _oldCommandsLockedTails.Clear();
        }

        private void SaveZones() {
            foreach (var zone in _currentZones) {
                if (zone.Value.IsModified()) {
                    var area = new Area {
                        X = zone.Value.Position.X,
                        Y = zone.Value.Position.Y,
                        Width = zone.Value.Size.X,
                        Height = zone.Value.Size.Y
                    };
                    _data.Scenes[_data.DefaultScene].Zones[zone.Value.Key].Area = area;

                    zone.Value.ApplyChanges();
                }
            }

            _oldCommandsZones.Clear();
        }

        #endregion

        #region Clear

        private void ClearScene() {
            ClearSceneObjects();
            ClearExpansions();
            ClearLockedTails();
            ClearZones();

            if (_currentScene != null) {
                DestroyImmediate(_currentScene);
            }

            var allGarbage = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var garbage in allGarbage) {
                var so = garbage.GetComponent<SceneObjectEditor>();
                if (so != null) {
                    DestroyImmediate(garbage);
                }
            }
        }

        private void ClearSceneObjects() {
            foreach (var newSceneObject in _newSceneObjects) {
                DestroyImmediate(newSceneObject.ObjTransform.gameObject);
            }

            foreach (var sceneObject in _currentSceneObjects) {
                DestroyImmediate(sceneObject.ObjTransform.gameObject);
            }

            _currentSceneObjects.Clear();
            _newSceneObjects.Clear();
            _sceneObjectsDataForDelete.Clear();
        }

        private void ClearExpansions() {
            foreach (var newExpansion in _newExpansions) {
                DestroyImmediate(newExpansion.ObjTransform.gameObject);
            }

            foreach (var expansion in _currentExpansions) {
                DestroyImmediate(expansion.ObjTransform.gameObject);
            }

            _newExpansions.Clear();
            _currentExpansions.Clear();
            _expansionsDataForDelete.Clear();
        }

        private void ClearLockedTails() {
            foreach (var tail in _newLockedTails) {
                DestroyImmediate(tail.ObjTransform.gameObject);
            }

            foreach (var tail in _currentLockedTails) {
                DestroyImmediate(tail.ObjTransform.gameObject);
            }

            _newLockedTails.Clear();
            _currentLockedTails.Clear();
            _lockedTailsForDelete.Clear();
        }

        private void ClearZones() {
            foreach (var zone in _currentZones) {
                DestroyImmediate(zone.Key);
            }

            _currentZones.Clear();
        }

        private void DiscardChanges() {
            switch (_currentMode) {
                case EditMode.SceneObjects:
                    DiscardChanges(_newSceneObjects, _currentSceneObjects, _sceneObjectsDataForDelete, false);
                    _oldCommandsSceneObjects.Clear();
                    break;
                case EditMode.Expansions:
                    DiscardChanges(_newExpansions, _currentExpansions, _expansionsDataForDelete, true);
                    _oldCommandsExpansions.Clear();
                    break;
                case EditMode.ZonesAndLockedTiles:
                    if (_currentZoneLockedTailsMode == ZoneLockedTailsMode.LockedTails) {
                        DiscardChanges(_newLockedTails, _currentLockedTails, _lockedTailsForDelete, false);
                        _oldCommandsLockedTails.Clear();
                    } else if (_currentZoneLockedTailsMode == ZoneLockedTailsMode.Zones) {
                        foreach (var zone in _currentZones) {
                            zone.Value.DiscardChanges(true);
                        }

                        _oldCommandsZones.Clear();
                    }

                    break;
            }
        }

        private void UndoLastChange() {
            switch (_currentMode) {
                case EditMode.SceneObjects:
                    _undoCommandSceneObjects.Execute();
                    break;
                case EditMode.Expansions:
                    _undoCommandExpansions.Execute();
                    break;
                case EditMode.ZonesAndLockedTiles:
                    if (_currentZoneLockedTailsMode == ZoneLockedTailsMode.LockedTails) {
                        _undoCommandLockedTails.Execute();
                    } else if (_currentZoneLockedTailsMode == ZoneLockedTailsMode.Zones) {
                        _undoCommandZones.Execute();
                    }

                    break;
            }
        }

        private void DiscardChanges(List<SceneObjectDataContainer> newObjects,
            List<SceneObjectDataContainer> currentObjects,
            List<SceneObjectDataContainer> objectsForDelete,
            bool discardSize) {
            foreach (var newObject in newObjects) {
                DestroyImmediate(newObject.ObjTransform.gameObject);
            }

            newObjects.Clear();

            foreach (var so in objectsForDelete) {
                currentObjects.Add(so);
                so.ObjTransform.gameObject.SetActive(true);
            }

            objectsForDelete.Clear();

            foreach (var sceneObject in currentObjects) {
                sceneObject.DiscardChanges();
            }
        }

        #endregion

        #region Draw

        private bool isScatteredObjetWaitToDelete;

        private void OnGUI() {
            EditorGUILayout.Space();

            _currentMode = (EditMode) GUILayout.SelectionGrid((int) _currentMode, SceneEditorHelper.ToolbarIcons, 5);

            if (_currentMode == EditMode.SceneObjects
                || _currentMode == EditMode.Expansions
                || _currentMode == EditMode.ZonesAndLockedTiles) {
                EditorGUILayout.Space();
                DrawCheckBoxes();
                EditorGUILayout.Space();
            }

            switch (_currentMode) {
                case EditMode.NotSelect:
                    EditorGUILayout.HelpBox("No tool selected \nPlease select a tool", MessageType.None);
                    scrollView = EditorGUILayout.BeginScrollView(scrollView);
                    EditorGUILayout.EndScrollView();
                    break;
                case EditMode.SceneObjects:
                    DrawCreateSceneObjectsMode();
                    break;
                case EditMode.Expansions:
                    scrollView = EditorGUILayout.BeginScrollView(scrollView);
                    DrawCreateExpansionsMode();
                    EditorGUILayout.EndScrollView();
                    break;
                case EditMode.ZonesAndLockedTiles:
                    scrollView = EditorGUILayout.BeginScrollView(scrollView);
                    DrawCreateZonesMode();
                    EditorGUILayout.EndScrollView();
                    break;
                case EditMode.Settings:
                    scrollView = EditorGUILayout.BeginScrollView(scrollView);
                    DrawSettingsMode();
                    EditorGUILayout.EndScrollView();
                    break;
                case EditMode.Info:
                    scrollView = EditorGUILayout.BeginScrollView(scrollView);
                    DrawInfoMode();
                    EditorGUILayout.EndScrollView();
                    break;
            }

            if (_currentMode != EditMode.Info) {
                DrawSaveStatusInfo();
                if (SceneEditorGuiTools.Button("Save Scene", _haveChanges ? Color.green : Color.gray, 200, 40)) {
                    SaveScene();
                }

                EditorGUILayout.Space();
            }

            UpdateSceneObjectsVisual();
            UpdateExpansionsVisual();
            UpdateZonesVisual();
            UpdateLockedTailsVisual();

            ShortCut();
        }

        void FixDuplicateIds() {
            HashSet<int> takenIds = new HashSet<int>();
            var nextId = GetHighestId();
            foreach (var obj in _data.Scenes[_data.DefaultScene].StartObjects) {
                var wasId = obj.Id;
                while (takenIds.Contains(obj.Id)) {
                    obj.Id = nextId++;
                }

                if (wasId != obj.Id) {
                    var onScene = _currentSceneObjects.Find(x => x.Id == wasId);
                    onScene.Id = obj.Id;
                }
                takenIds.Add(obj.Id);
            }
        }

        private void DrawDiscardChangesGui() {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            var color = Color.gray;
            var oldColor = color;
            switch (_currentMode) {
                case EditMode.SceneObjects:
                    color = _oldCommandsSceneObjects.Count > 0 ? Color.white : Color.gray;
                    break;
                case EditMode.Expansions:
                    color = _oldCommandsExpansions.Count > 0 ? Color.white : Color.gray;
                    break;
                case EditMode.ZonesAndLockedTiles:
                    if (_currentZoneLockedTailsMode == ZoneLockedTailsMode.LockedTails) {
                        color = _oldCommandsLockedTails.Count > 0 ? Color.white : Color.gray;
                    } else if (_currentZoneLockedTailsMode == ZoneLockedTailsMode.Zones) {
                        color = _oldCommandsZones.Count > 0 ? Color.white : Color.gray;
                    }

                    break;
            }

            if (SceneEditorGuiTools.Button("", color, 30, 0, SceneEditorHelper.GetUndoIcon())) {
                UndoLastChange();
            }

            if (SceneEditorGuiTools.Button(isScatteredObjetWaitToDelete ? "Cancel" : "Discard all changes", Color.white,
                50, 20)) {
                isScatteredObjetWaitToDelete = !isScatteredObjetWaitToDelete;
            }

            if (isScatteredObjetWaitToDelete) {
                if (SceneEditorGuiTools.Button("Discard", Color.red, 100, 20)) {
                    DiscardChanges();
                    isScatteredObjetWaitToDelete = false;
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (color != oldColor) {
                Repaint();
            }
        }

        private void DrawCheckBoxes() {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Show:", GUILayout.Width(40));
            if (_currentMode == EditMode.SceneObjects) {
                EditorGUI.BeginDisabledGroup(true);
                _showSceneObjects = EditorGUILayout.ToggleLeft("Scene Objects", true, GUILayout.Width(100));
                EditorGUI.EndDisabledGroup();
            } else {
                _showSceneObjects =
                    EditorGUILayout.ToggleLeft("Scene Objects", _showSceneObjects, GUILayout.Width(100));
            }

            if (_currentMode == EditMode.Expansions) {
                EditorGUI.BeginDisabledGroup(true);
                _showExpansions = EditorGUILayout.ToggleLeft("Expansion", true, GUILayout.Width(80));
                EditorGUI.EndDisabledGroup();
            } else {
                _showExpansions = EditorGUILayout.ToggleLeft("Expansion", _showExpansions, GUILayout.Width(80));
            }

            if (_currentMode == EditMode.ZonesAndLockedTiles) {
                switch (_currentZoneLockedTailsMode) {
                    case ZoneLockedTailsMode.LockedTails:
                        EditorGUI.BeginDisabledGroup(true);
                        _showLockedTiles = EditorGUILayout.ToggleLeft("Locked Tiles", true, GUILayout.Width(100));
                        EditorGUI.EndDisabledGroup();
                        _showZones = EditorGUILayout.ToggleLeft("Zones", _showZones, GUILayout.Width(60));
                        break;
                    case ZoneLockedTailsMode.Zones:
                        _showLockedTiles =
                            EditorGUILayout.ToggleLeft("Locked Tiles", _showLockedTiles, GUILayout.Width(100));
                        EditorGUI.BeginDisabledGroup(true);
                        _showZones = EditorGUILayout.ToggleLeft("Zones", true, GUILayout.Width(60));
                        EditorGUI.EndDisabledGroup();
                        break;
                }
            } else {
                _showLockedTiles = EditorGUILayout.ToggleLeft("Locked Tiles", _showLockedTiles, GUILayout.Width(100));
                _showZones = EditorGUILayout.ToggleLeft("Zones", _showZones, GUILayout.Width(60));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCreateSceneObjectsMode() {
            EditorGUILayout.Space();
            SceneEditorGuiTools.SimpleTitle("Scene Objects editor");

            DrawDiscardChangesGui();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            _selectedSceneObjectsFlags = EditorGUILayout.MaskField("Shown Types", _selectedSceneObjectsFlags, _selectedTypesOfSceneObjects);
            var selectedOptions = new List<string>();
            for (int i = 0; i < _selectedTypesOfSceneObjects.Length; i++) {
                if ((_selectedSceneObjectsFlags & (1 << i)) == (1 << i))
                    selectedOptions.Add(_selectedTypesOfSceneObjects[i]);
            }

            EditorGUILayout.Space();
            
            _selectedSceneObjectsOnSceneFlags = EditorGUILayout.MaskField("Shown Types on Scene", _selectedSceneObjectsOnSceneFlags, _selectedTypesOfSceneObjects);
            _sceneObjectTypesShownOnScene.Clear();
            for (int i = 0; i < _selectedTypesOfSceneObjects.Length; i++) {
                if ((_selectedSceneObjectsOnSceneFlags & (1 << i)) == (1 << i))
                    _sceneObjectTypesShownOnScene.Add(_selectedTypesOfSceneObjects[i]);
            }

            EditorGUILayout.Space();

            _findSceneObjectText = EditorGUILayout.TextField("Find:", _findSceneObjectText);
            EditorGUILayout.Space();

            scrollView = EditorGUILayout.BeginScrollView(scrollView);
            SceneEditorGuiTools.BeginGroup();
            DrawPrefabInspector(selectedOptions);
            SceneEditorGuiTools.EndGroup();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            if (SceneEditorGuiTools.Button("Reload Data", Color.white, 200, 30)) {
                AssetDatabase.Refresh();
                LoadSceneObjectsData();
                _previewSceneObjects.Clear();
                LoadSceneObjects();
                AssetDatabase.Refresh();
            }
            EditorGUILayout.Space();
        }

        private void DrawCreateExpansionsMode() {
            EditorGUILayout.Space();
            SceneEditorGuiTools.SimpleTitle("Expansions editor");

            DrawDiscardChangesGui();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            SceneEditorGuiTools.SimpleText("Expansion size: ");
            _currentExpansionSize = EditorGUILayout.Vector2IntField("", _currentExpansionSize, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }

		private void DrawCreateZonesMode() {
            EditorGUILayout.Space();
            SceneEditorGuiTools.SimpleTitle("Zones and locked tiles editor");

            DrawDiscardChangesGui();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            _currentZoneLockedTailsMode =
                (ZoneLockedTailsMode) EditorGUILayout.EnumPopup("Mode: ", _currentZoneLockedTailsMode);

            switch (_currentZoneLockedTailsMode) {
                case ZoneLockedTailsMode.LockedTails:

                    break;
                case ZoneLockedTailsMode.Zones:
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical();
                    foreach (var currentZone in _currentZones) {
                        EditorGUILayout.BeginHorizontal();
                        SceneEditorGuiTools.SimpleText(currentZone.Value.Key + " zone size: ");
                        currentZone.Value.SetSize(EditorGUILayout.Vector2IntField("", currentZone.Value.SizeV2I,
                            GUILayout.Width(100)));
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndVertical();
                    break;
            }
        }

        private void DrawSettingsMode() {
            EditorGUILayout.Space();
            _autoSave = EditorGUILayout.ToggleLeft("Auto Save", _autoSave, GUILayout.Width(100));
            if (preference != null) {
                if (preference.autoSave != _autoSave) {
                    preference.autoSave = _autoSave;
                    preference.SavePreference();
                }
            }
        }

        private void DrawInfoMode() {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mouse Left Click\t\t: Place Object");
            EditorGUILayout.LabelField("Shift + Mouse Left Drag\t: Move Object");
            EditorGUILayout.LabelField("Shift + Mouse Right Click\t: Delete Object");
            EditorGUILayout.Space();
            SceneEditorGuiTools.SimpleTitle("Shortcuts");
            EditorGUILayout.LabelField("Ctrl+Z\t: Undo Last Action");
            EditorGUILayout.LabelField("Tab\t: Change edit mode");
            EditorGUILayout.LabelField("Shift+Tab\t: Select Zones or Locked tiles");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("I\t: Show Info");

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            SceneEditorGuiTools.SimpleTitle("Don't forget click \"Save scene\" button to apply your changes!");
        }

        private void DrawSaveStatusInfo() {
            if (_newSceneObjects.Count > 0
                || _sceneObjectsDataForDelete.Count > 0
                || _newExpansions.Count > 0
                || _expansionsDataForDelete.Count > 0
                || _newLockedTails.Count > 0
                || _lockedTailsForDelete.Count > 0
                || HasModifiedSceneObjects()
            ) {
                if (!_haveChanges) {
                    _haveChanges = !_haveChanges;
                    Repaint();
                }

                EditorGUILayout.HelpBox("Have not saved changes!", MessageType.Warning);
            } else {
                if (_haveChanges) {
                    _haveChanges = !_haveChanges;
                    Repaint();
                }
            }
        }

        private bool HasModifiedSceneObjects() {
            foreach (var value in _currentSceneObjects)
                if (value.IsModified())
                    return true;

            foreach (var value in _currentExpansions)
                if (value.IsModified())
                    return true;

            foreach (var zone in _currentZones.Values) {
                if (zone.IsModified())
                    return true;
            }

            return false;
        }

        private void SceneGui(SceneView sceneView) {
            DrawSaveStatusInfo();
            ShortCut();

            if (_currentMode == EditMode.ZonesAndLockedTiles &&
                _currentZoneLockedTailsMode == ZoneLockedTailsMode.Zones) {
                preference.showNormal = false;
            } else {
                preference.showNormal = !isShift;
            }

            if (_currentMode == EditMode.Expansions) {
                SceneEditorHelper.DrawDescriptionsOnSceneObjects(_currentExpansions.ToList());
            }

            if (Event.current.type == EventType.MouseDrag && Event.current.button == 0 && Event.current.control) {
                var delta = Event.current.delta;
                if (LastTransform) {
                    LastTransform.localScale -= Vector3.one * (delta.y / 100f);
                    LastTransform.Rotate(Vector3.up * delta.x);
                }
            }

            if (Event.current.alt && Event.current.type != EventType.ScrollWheel ||
                (Event.current.control && _currentMode != EditMode.Expansions)) {
                return;
            }

            var mousePos = Event.current.mousePosition;
            mousePos.y = Screen.height - mousePos.y - 40;

            var camEditor = SceneView.lastActiveSceneView.camera;
            if (camEditor == null) return;
            var ray = camEditor.ScreenPointToRay(mousePos);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, brush.pickableLayer)) {
                if (_currentMode == EditMode.SceneObjects
                    || _currentMode == EditMode.Expansions
                    || _currentMode == EditMode.ZonesAndLockedTiles) {
                    var rect = new Rect(0, 0, Screen.width, Screen.height);
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.Arrow);

                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                    UnityEditor.Tools.current = Tool.None;

                    if (Event.current.type == EventType.MouseUp) {
                        _currentMovingObject = null;
                    }

                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
                        var screenPoint = camEditor.WorldToScreenPoint(hit.transform.position);
                        var cameraWorldPos =
                            camEditor.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, screenPoint.z));
                        _dragOffset = hit.transform.root.position - cameraWorldPos;

                        if (!isShift) {
                            DoCreate(hit.point, hit.normal);
                        }
                    }

                    if (Event.current.type == EventType.MouseDrag && Event.current.button == 0) {
                        if (isShift) {
                            DoMove(hit.point, hit.normal);
                        } else {
                            DoCreate(hit.point, hit.normal);
                        }
                    }

                    if ((Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown) &&
                        Event.current.button == 1) {
                        if (isShift) {
                            Event.current.Use();
                            DoDelete(hit.transform);
                        }
                    }
                }

                if (_currentMode == EditMode.SceneObjects || _currentMode == EditMode.ZonesAndLockedTiles ||
                    _currentMode == EditMode.Expansions) {
                    SceneEditorHelper.DrawBrush(preference, brush, hit.point, brush.size / 2, hit.normal);
                }
            }

            sceneView.Repaint();
        }

        private void UpdateLockedTailsVisual() {
            var isLockedTilesCategory = _currentMode == EditMode.ZonesAndLockedTiles &&
                                        _currentZoneLockedTailsMode == ZoneLockedTailsMode.LockedTails;
            var active = isLockedTilesCategory || _showLockedTiles;

            foreach (var tail in _currentLockedTails) {
                if (tail.ObjTransform.gameObject.activeSelf != active) {
                    tail.ObjTransform.gameObject.SetActive(active);
                }
            }

            foreach (var tail in _newLockedTails) {
                if (tail.ObjTransform.gameObject.activeSelf != active) {
                    tail.ObjTransform.gameObject.SetActive(active);
                }
            }
        }

        private void UpdateZonesVisual() {
            var isZoneCategory = _currentMode == EditMode.ZonesAndLockedTiles &&
                                 _currentZoneLockedTailsMode == ZoneLockedTailsMode.Zones;
            var isActive = isZoneCategory || _showZones;

            foreach (var zone in _currentZones) {
                if (zone.Key.activeSelf != isActive) {
                    zone.Key.SetActive(isActive);
                }
            }
        }

        private void UpdateExpansionsVisual() {
            var active = _currentMode == EditMode.Expansions || _showExpansions;

            foreach (var expansion in _currentExpansions) {
                if (expansion.ObjTransform.gameObject.activeSelf != active) {
                    expansion.ObjTransform.gameObject.SetActive(active);
                }
            }

            foreach (var expansion in _newExpansions) {
                if (expansion.ObjTransform.gameObject.activeSelf != active) {
                    expansion.ObjTransform.gameObject.SetActive(active);
                }
            }
        }

        private void UpdateSceneObjectsVisual() {
            var active = _currentMode == EditMode.SceneObjects || _showSceneObjects;

            foreach (var so in _currentSceneObjects) {
                var soActive = _sceneObjectTypesShownOnScene.Contains(so.PreviewObject.SceneObjectData.Type.ToString());
                if (active) {
                    if (so.ObjTransform.gameObject.activeSelf != soActive) {
                        so.ObjTransform.gameObject.SetActive(soActive);
                    }
                } else if(so.ObjTransform.gameObject.activeSelf) {
                    so.ObjTransform.gameObject.SetActive(false);
                }
            }

            foreach (var so in _newSceneObjects) {
                var soActive = _sceneObjectTypesShownOnScene.Contains(so.PreviewObject.SceneObjectData.Type.ToString());
                if (active) {
                    if (so.ObjTransform.gameObject.activeSelf != soActive) {
                        so.ObjTransform.gameObject.SetActive(soActive);
                    }

                } else if(so.ObjTransform.gameObject.activeSelf) {
                    so.ObjTransform.gameObject.SetActive(false);
                }
            }
        }

        private void DrawPrefabInspector(List<string> selectedTypes) {
            EditorGUILayout.Space();
            for (int i = 0; i < _previewSceneObjects.Count; i++) {
                if (_previewSceneObjects[i].prefab != null) {
                    if (selectedTypes.Contains(_previewSceneObjects[i].SceneObjectData.Type.ToString())) {
                        if (!string.IsNullOrEmpty(_findSceneObjectText)) {
                            var locale = LocalizationEngine.GetTerm(_previewSceneObjects[i].SceneObjectData.Name);
                            if (_previewSceneObjects[i].SceneObjectKey.ToLower().Contains(_findSceneObjectText.ToLower())
                                || locale.ToLower().Contains(_findSceneObjectText.ToLower())) {
                                DrawPrefabChild(_previewSceneObjects[i]);
                            }
                        } else {
                            DrawPrefabChild(_previewSceneObjects[i]);
                        }
                    }
                } else {
                    _previewSceneObjects.Remove(_previewSceneObjects[i]);
                }
            }
        }

        private void CreateSceneObjectPreview(Object dragObject,
            string soKey,
            SceneObject soData,
            SceneExpansionData expansionData,
            EditMode type,
            ZoneLockedTailsMode zoneTailMode = ZoneLockedTailsMode.LockedTails,
            string zoneKey = "Farm") {
            if (dragObject is GameObject || dragObject.GetType() == typeof(Transform)) {
                GameObject obj = null;
                if (dragObject is GameObject gameObject) {
                    obj = gameObject;
                } else {
                    obj = ((Transform) dragObject).gameObject;
                }

                // var result = _previewSceneObjects.FindIndex(s => s.prefab == obj || (s.prefab.name == obj.name && s.prefab != obj));
                var result = _previewSceneObjects.FindIndex(s => s.SceneObjectKey == soKey);
                if (result == -1 || type != EditMode.SceneObjects) {
                    var so = new PreviewObject();
                    so.prefab = obj;
                    so.SceneObjectKey = soKey;
                    so.SceneObjectData = soData;
                    so.ExpansionData = expansionData;
                    so.enable = false;
                    so.isPrefab = PrefabUtility.IsPartOfPrefabAsset(obj);
                    so.offset = 0f;

                    switch (type) {
                        case EditMode.SceneObjects:
                            _previewSceneObjects.Add(so);
                            break;
                        case EditMode.Expansions:
                            _previewExpansion = so;
                            break;
                        case EditMode.ZonesAndLockedTiles:
                            if (zoneTailMode == ZoneLockedTailsMode.LockedTails) {
                                _previewLockedTail = so;
                            } else if (zoneTailMode == ZoneLockedTailsMode.Zones) {
                                _previewZones[zoneKey] = so;
                            }

                            break;
                    }
                }
            }
        }

        private void DrawPrefabChild(PreviewObject scatterChild) {
            EditorGUILayout.BeginHorizontal();
            bool tmpButtonState = SceneEditorGuiTools.ToggleButton(scatterChild.enable, scatterChild,
                ref scatterChild.preview, scatterChild.prefab);
            if (!scatterChild.enable && tmpButtonState) {
                scatterChild.enable = true;
                List<PreviewObject> tempObj = _previewSceneObjects.FindAll(s => s.enable == true && s != scatterChild);

                foreach (PreviewObject sco in tempObj) {
                    sco.enable = false;
                }
            } else {
                scatterChild.enable = tmpButtonState;
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Tools

        private void DoMove(Vector3 pos, Vector3 normal) {
            var offsetPos = new Vector3(pos.x + _dragOffset.x, 0f, pos.z + _dragOffset.z);
            var resPos = _currentMode == EditMode.SceneObjects ? pos : offsetPos;

            var newObjects = _newSceneObjects;
            var currentObjects = _currentSceneObjects;
            var currentCommandsContainer = _oldCommandsSceneObjects;

            if (_currentMode == EditMode.Expansions) {
                newObjects = _newExpansions;
                currentObjects = _currentExpansions;
            }

            if (_currentMovingObject != null) {
                if (!CanPlace(resPos, _currentMovingObject)) {
                    return;
                }

                var oldPos = _currentMovingObject.Position;

                var command = new MoveObjectCommand(_currentMovingObject, resPos);
                command.Execute();
                switch (_currentMode) {
                    case EditMode.SceneObjects:
                        currentCommandsContainer = _oldCommandsSceneObjects;
                        break;
                    case EditMode.Expansions:
                        currentCommandsContainer = _oldCommandsExpansions;
                        break;
                    case EditMode.ZonesAndLockedTiles:
                        currentCommandsContainer = _oldCommandsZones;
                        break;
                }

                if (oldPos != _currentMovingObject.Position) {
                    currentCommandsContainer.Add(command);
                }
            } else {
                RaycastHit hit;
                if (Physics.Raycast(pos + normal.normalized, -normal, out hit, Mathf.Infinity, brush.pickableLayer)) {
                    var go = hit.transform.root.gameObject;
                    switch (_currentMode) {
                        case EditMode.ZonesAndLockedTiles:
                            if (_currentZoneLockedTailsMode == ZoneLockedTailsMode.Zones) {
                                foreach (var zone in _currentZones) {
                                    if (zone.Key == go) {
                                        _currentMovingObject = zone.Value;
                                        return;
                                    }
                                }
                            }

                            break;
                        case EditMode.SceneObjects:
                        case EditMode.Expansions:
                            foreach (var newSceneObject in newObjects) {
                                if (newSceneObject.ObjTransform.gameObject == go) {
                                    _currentMovingObject = newSceneObject;
                                    return;
                                }
                            }

                            foreach (var sceneObject in currentObjects) {
                                if (sceneObject.ObjTransform.gameObject == go) {
                                    _currentMovingObject = sceneObject;
                                    return;
                                }
                            }

                            break;
                    }

                    if (_currentMovingObject != null) {
                        DoMove(resPos, normal);
                    }
                }
            }
        }

        private void DoCreate(Vector3 targetPosition, Vector3 normal) {
            List<PreviewObject> objs = null;
            if (_currentMode == EditMode.SceneObjects) {
                objs = _previewSceneObjects.FindAll(
                    delegate(PreviewObject s) {
                        return s.enable == true;
                    }
                );
            } else if (_currentMode == EditMode.Expansions) {
                objs = new List<PreviewObject>();
                objs.Add(_previewExpansion);
            } else if (_currentMode == EditMode.ZonesAndLockedTiles) {
                objs = new List<PreviewObject>();
                objs.Add(_previewLockedTail);
            }

            // Compute normal rotation
            var rot = Quaternion.FromToRotation(Vector3.up, normal);

            if (objs.Count > 0) {
                for (int o = 0; o < brush.amount; o++) {
                    var pos = targetPosition;

                    // Position relative to brush radius
                    var angle = Random.Range(-Mathf.PI * 2, Mathf.PI * 2);
                    pos = targetPosition + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) *
                          Random.Range(-brush.size / 2f, brush.size / 2f);

                    // Compute position relative to normal
                    var dir = pos - targetPosition;
                    dir = rot * dir;
                    pos = dir + targetPosition;

                    // Cast against the scene
                    RaycastHit hit;
                    if (Physics.Raycast(pos + normal.normalized, -normal, out hit, Mathf.Infinity,
                        brush.pickableLayer)) {
                        // Compute slope
                        float slopeAngle = Mathf.Acos(Mathf.Clamp(hit.normal.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;

                        if (slopeAngle >= brush.minSlope && slopeAngle < brush.maxSlope) {
                            pos = hit.point;
                            // Create the new object
                            int rndObj = Random.Range(0, objs.Count);
                            var objForCreate = objs[rndObj];

                            pos.y = hit.point.y + objForCreate.offset;
                            var newPos = new Point(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.z));

                            if (_currentMode == EditMode.SceneObjects || _currentMode == EditMode.Expansions) {
                                if (!CanPlace(newPos, objForCreate.GetSize())) {
                                    return;
                                }
                            }

                            if (_currentMode == EditMode.ZonesAndLockedTiles) {
                                if (ContainLockedTail(newPos) ||
                                    _currentZoneLockedTailsMode != ZoneLockedTailsMode.LockedTails) {
                                    return;
                                }
                            }

                            GameObject obj = null;
                            ICommand command = null;
                            switch (_currentMode) {
                                case EditMode.SceneObjects:
                                    command = new CreateNewObjectCommand(_newSceneObjects, objForCreate, newPos);
                                    _oldCommandsSceneObjects.Add(command);
                                    break;
                                case EditMode.Expansions:
                                    command = new CreateNewExpansionCommand(_newExpansions, objForCreate, newPos,
                                        new Point(_currentExpansionSize.x, _currentExpansionSize.y));
                                    _oldCommandsExpansions.Add(command);
                                    break;
                                case EditMode.ZonesAndLockedTiles:
                                    command = new CreateNewObjectCommand(_newLockedTails, objForCreate, newPos);
                                    _oldCommandsLockedTails.Add(command);
                                    break;
                            }

                            if (command != null) {
                                command.Execute();
                                if (_autoSave) {
                                    SaveScene();
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool CanPlace(Vector3 pos, SceneObjectDataContainer data) {
            if (_currentMode == EditMode.ZonesAndLockedTiles) {
                return true;
            }

            var newPos = new Point(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.z));
            return CanPlace(newPos, data.Size, data);
        }

        private bool CanPlace(Point pos, Point size, SceneObjectDataContainer data = null) {
            for (var i = 0; i < size.X; i++)
                for (var j = 0; j < size.Y; j++)
                    if (!CheckPosition(pos + new Point(i, j), data))
                        return false;

            return true;
        }

        private bool CheckPosition(Point pos, SceneObjectDataContainer data) {
            var newObjects = _newSceneObjects;
            var currentObjects = _currentSceneObjects;

            switch (_currentMode) {
                case EditMode.Expansions:
                    newObjects = _newExpansions;
                    currentObjects = _currentExpansions;
                    break;
            }

            foreach (var newSceneObject in newObjects) {
                if (data != null && data == newSceneObject)
                    continue;

                if (ContainTail(pos, newSceneObject.Position, newSceneObject.Size))
                    return false;
            }

            foreach (var value in currentObjects) {
                if (data != null && data == value)
                    continue;

                if (ContainTail(pos, value.Position, value.Size))
                    return false;
            }

            return true;
        }

        private bool ContainTail(Point tail, Point objectPosition, Point size) {
            if (tail.X >= objectPosition.X && tail.X < objectPosition.X + size.X && tail.Y >= objectPosition.Y &&
                tail.Y < objectPosition.Y + size.Y) {
                return true;
            }

            return false;
        }

        private bool ContainLockedTail(Point pos) {
            foreach (var tail in _currentLockedTails) {
                if (tail.Position == pos) {
                    return true;
                }
            }

            foreach (var tail in _newLockedTails) {
                if (tail.Position == pos) {
                    return true;
                }
            }

            return false;
        }

        private void DoDelete(Transform obj) {
            var newObjects = _newSceneObjects;
            var forDeleteObjects = _sceneObjectsDataForDelete;
            var currentObjects = _currentSceneObjects;
            var commands = _oldCommandsSceneObjects;

            switch (_currentMode) {
                case EditMode.SceneObjects:

                    break;
                case EditMode.Expansions:
                    newObjects = _newExpansions;
                    forDeleteObjects = _expansionsDataForDelete;
                    currentObjects = _currentExpansions;
                    commands = _oldCommandsExpansions;
                    break;
                case EditMode.ZonesAndLockedTiles:
                    newObjects = _newLockedTails;
                    forDeleteObjects = _lockedTailsForDelete;
                    currentObjects = _currentLockedTails;
                    commands = _oldCommandsLockedTails;
                    break;
            }

            var rootTransform = obj.root;
            ICommand command = null;
            var newObj = newObjects.FirstOrDefault(x => x.ObjTransform == rootTransform);
            if (newObj != null) {
                command = new DeleteNewObjectCommand(newObjects, newObj);
            }

            var curObj = currentObjects.FirstOrDefault(x => x.ObjTransform.gameObject.transform == rootTransform);
            if (curObj != null) {
                command = new DeleteCurrentObjectCommand(currentObjects, forDeleteObjects, curObj);
            }

            if (command != null) {
                command.Execute();
                commands.Add(command);
                if (_autoSave) {
                    SaveScene();
                }
            }
        }

        private void ChangeCategory() {
            _prevKey = Event.current.keyCode;

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab) {
                if (_currentMode == EditMode.ZonesAndLockedTiles && isShift) {
                    if (_currentZoneLockedTailsMode == ZoneLockedTailsMode.LockedTails) {
                        _currentZoneLockedTailsMode = ZoneLockedTailsMode.Zones;
                        _showLockedTiles = false;
                    } else {
                        _currentZoneLockedTailsMode = ZoneLockedTailsMode.LockedTails;
                        _showZones = false;
                    }
                } else {
                    if ((int)_currentMode >= 2 || _currentMode == EditMode.NotSelect) {
                        _currentMode = 0;
                    } else {
                        _currentMode++;
                    }
                }
            }
            
            Repaint();
        }

        void ShortCut() {
            ChangeCategory();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.I) {
                _prevKey = Event.current.keyCode;
                _currentMode = EditMode.Info;
                Repaint();
            }

            /*if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Q && isShift) {
                _prevKey = Event.current.keyCode;
                _currentMode = EditMode.NotSelect;
                _showSceneObjects = true;
                _showExpansions = true;
                _showZones = false;
                _showLockedTiles = false;
                _currentZoneLockedTailsMode = ZoneLockedTailsMode.LockedTails;
                Repaint();
            }*/

            if (Event.current.shift && !isShift) {
                isShift = true;
            }

            if (!Event.current.shift && isShift) {
                isShift = false;
            }

            if (Event.current.alt && !isAlt) {
                isAlt = true;
            }

            if (!Event.current.alt && isAlt) {
                isAlt = false;
            }


            if (Event.current.control && !isCtrl) {
                isCtrl = true;
            }

            if (!Event.current.control && isCtrl) {
                isCtrl = false;
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Z) {
                if (isCtrl) {
                    UndoLastChange();
                }
            }
        }

        #endregion
    }
}
