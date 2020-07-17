using UnityEngine;

namespace Editor.SceneEditor {
    public class SceneObjectBrush {
        public enum AlignVector {Left,Front,right,Back,Up,down};

        public float size=0;
        public int amount=1;
        public float flux=5;
        public float minSlope=0;
        public float maxSlope=90;
        public LayerMask pickableLayer = -1; // 1<<19;
        public bool align2Surface = false;
    }
}
