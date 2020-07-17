using UnityEngine;

namespace Editor.SceneEditor.Commands {
    public class MoveObjectCommand : ICommand {
        private readonly SceneObjectDataContainer _movingObject;
        private readonly Vector3 _position;
        private readonly Vector3 _prevPosition;
        
        public MoveObjectCommand(SceneObjectDataContainer movingObject, Vector3 pos) {
            _movingObject = movingObject;
            _position = pos;
            _prevPosition = _movingObject.ObjTransform.position;
        }

        public void Execute() {
            _movingObject.SetPosition(_position);
        }

        public void Undo() {
            _movingObject.SetPosition(_prevPosition);
        }
    }
}