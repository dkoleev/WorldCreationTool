using Editor.SceneEditor.DataTypes;
using UnityEngine;

namespace Editor.SceneEditor {
	public class PreviewObject {
		public bool enable;
		public GameObject prefab;
		public string SceneObjectKey;
		public bool isPrefab = false;
		public float offset = 0;
		public Texture2D preview;

		public Point GetSize() {
			//TODO: setup object size
			return new Point(1, 1);
		}
	}
}