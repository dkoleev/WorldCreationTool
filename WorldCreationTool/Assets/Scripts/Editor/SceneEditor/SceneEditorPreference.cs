using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace Editor.SceneEditor {
	[System.Serializable]
	public class SceneEditorPreference {
		public bool showBrushSize = true;
		public int brushDetail = 18;
		public float minBrushDrawSize = 0.5f;
		public Color brushColor = new Color(24f / 255f, 118f / 255f, 175f / 255f);

		public bool showCentralDot = true;
		public float dotSize = 0.1f;

		public bool showNormal = true;
		public Color normalColor = new Color(147f / 255f, 244f / 255f, 66f / 255f);
		public bool autoSave;
		
		public void LoadDefault() {
			showBrushSize = true;
			brushDetail = 18;
			minBrushDrawSize = 0.5f;
			brushColor = new Color(24f / 255f, 118f / 255f, 175f / 255f);

			showCentralDot = true;
			dotSize = 0.1f;

			showNormal = true;
			normalColor = new Color(147f / 255f, 244f / 255f, 66f / 255f);
		}
		
		public bool LoadPreference(){
			SceneEditorPreference  objXml = null;

			string[] guids = AssetDatabase.FindAssets( "SceneEditorPref",null);
			if (guids.Length >0){
				string path = AssetDatabase.GUIDToAssetPath( guids[0]);

				Stream fs = new FileStream(path,FileMode.Open);
				XmlSerializer serializer = new XmlSerializer(typeof(SceneEditorPreference));

				objXml = (SceneEditorPreference)serializer.Deserialize( fs);
				fs.Close();
			}

			if (objXml!=null) {
				autoSave = objXml.autoSave;
				return true;
			}
			else{
				SavePreference();
				return false;
			}
		}

		public void SavePreference(){
			string[] guids = AssetDatabase.FindAssets( "SceneEditorWindow",null);
			string path = AssetDatabase.GUIDToAssetPath( guids[0]);
			path = Path.GetDirectoryName( path) + "/SceneEditorPref.xml";
			Stream fs = new FileStream(path, FileMode.Create);
			XmlWriter writer = new XmlTextWriter(fs, Encoding.Unicode);
			XmlSerializer serializer = new XmlSerializer(typeof(SceneEditorPreference));
			serializer.Serialize(writer, this);
			writer.Close(); 
		
			AssetDatabase.Refresh();
		}
	}
}
