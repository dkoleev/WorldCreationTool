using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace Editor.SceneEditor {
    [Serializable]
    public class ScatterPreference {
        public Color brushColor = new Color(24f / 255f, 118f / 255f, 175f / 255f);
        public int brushDetail = 18;
        public float dotSize = 0.1f;

        // Prefab
        public bool keepPrefabLink;
        public int maxAmount = 100;
        public float maxFlux = 50;

        // Inspector
        public int maxSize = 200;
        public float minBrushDrawSize = 0.5f;
        public Color normalColor = new Color(147f / 255f, 244f / 255f, 66f / 255f);
        public bool showBrushSize = true;

        public bool showCentralDot = true;

        public bool showNormal = true;
        public bool showPrefabPreview = true;

        public bool LoadPreference() {
            ScatterPreference objXml = null;
            var guids = AssetDatabase.FindAssets("EasyScaterPref", null);

            if (guids.Length > 0) {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);

                Stream fs = new FileStream(path, FileMode.Open);
                var serializer = new XmlSerializer(typeof(ScatterPreference));

                objXml = (ScatterPreference) serializer.Deserialize(fs);
                fs.Close();
            }

            if (objXml != null) {
                showBrushSize = objXml.showBrushSize;
                brushDetail = objXml.brushDetail;
                minBrushDrawSize = objXml.minBrushDrawSize;
                brushColor = objXml.brushColor;

                showCentralDot = objXml.showCentralDot;
                dotSize = objXml.dotSize;

                showNormal = objXml.showNormal;
                normalColor = objXml.normalColor;
                keepPrefabLink = objXml.keepPrefabLink;
                showPrefabPreview = objXml.showPrefabPreview;

                maxSize = objXml.maxSize;
                maxAmount = objXml.maxAmount;
                maxFlux = objXml.maxFlux;

                return true;
            }

            SavePreference();
            return false;
        }

        public void SavePreference() {
            var guids = AssetDatabase.FindAssets("SceneEditorWindow", null);

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);

            path = Path.GetDirectoryName(path) + "/EasyScaterPref.xml";

            Stream fs = new FileStream(path, FileMode.Create);
            XmlWriter writer = new XmlTextWriter(fs, Encoding.Unicode);
            var serializer = new XmlSerializer(typeof(ScatterPreference));
            serializer.Serialize(writer, this);
            writer.Close();

            AssetDatabase.Refresh();
        }

        public void LoadDefault() {
            showBrushSize = true;
            brushDetail = 18;
            minBrushDrawSize = 0.5f;
            brushColor = new Color(24f / 255f, 118f / 255f, 175f / 255f);

            showCentralDot = true;
            dotSize = 0.1f;

            showNormal = true;
            normalColor = new Color(147f / 255f, 244f / 255f, 66f / 255f);

            keepPrefabLink = false;
            showPrefabPreview = true;

            maxSize = 200;
            maxAmount = 100;
            maxFlux = 50;

            SavePreference();
        }
    }
}
