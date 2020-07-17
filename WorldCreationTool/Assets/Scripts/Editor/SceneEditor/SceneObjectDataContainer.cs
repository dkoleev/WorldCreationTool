using Editor.SceneEditor.DataTypes;
using UnityEngine;

namespace Editor.SceneEditor {
    public class SceneObjectDataContainer {
        public string Key { get; set; }
        public int Id { get; set; }
        public Transform ObjTransform { get; }
        public Point Position { get; set; }
        public Vector2Int PositionV2I { get; set; }
        public Point Size { get; set; }
        public Vector2Int SizeV2I { get; set; }
        public PreviewObject PreviewObject { get; }

        private Point _startPosition;
        private Point _startSize;
        

        public SceneObjectDataContainer(int id, string key, Point position, Point size, Transform transform, PreviewObject preview) {
            Key = key;
            Id = id;
            ObjTransform = transform;
            Position = position;
            PositionV2I = new Vector2Int(Position.X, Position.Y);
            Size = size;
            SizeV2I = new Vector2Int(Size.X, Size.Y);

            _startPosition = Position;
            _startSize = Size;
            PreviewObject = preview;
        }

        public void SetPosition(Vector3 pos) {
            var newPos = new Point(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.z));
            Position = newPos;
            PositionV2I = new Vector2Int(newPos.X, newPos.Y);
            ObjTransform.position = new Vector3(newPos.X, ObjTransform.position.y, newPos.Y);
        }

        public void SetSize(Vector2Int size) {
            var newSize = new Point(size.x, size.y);
            Size = newSize;
            SizeV2I = size;
            ObjTransform.localScale = new Vector3(size.x, ObjTransform.localScale.y, size.y);
        }

        public bool IsModified() {
            return _startPosition != Position;
        }

        public void ApplyChanges() {
            _startPosition = Position;
            _startSize = Size;
        }

        public void DiscardChanges(bool changeSize = false) {
            SetPosition(new Vector3(_startPosition.X, ObjTransform.position.y, _startPosition.Y));
            if (changeSize) {
                SetSize(new Vector2Int(_startSize.X, _startSize.Y));
            }
        }
    }
}