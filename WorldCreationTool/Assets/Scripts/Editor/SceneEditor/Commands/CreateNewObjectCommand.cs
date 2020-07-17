using System.Collections.Generic;
using Editor.SceneEditor.DataTypes;
using UnityEngine;

namespace Editor.SceneEditor.Commands {
    public class CreateNewObjectCommand : ICommand {
        private readonly PreviewObject _newObject;
        private readonly Point _position;
        private readonly Point _size;
        private readonly List<SceneObjectDataContainer> _container;
        private readonly SceneObjectBrush _brush;
        private SceneObjectDataContainer _createdObject;
        
        public CreateNewObjectCommand(List<SceneObjectDataContainer> container, PreviewObject newObject, Point pos) {
            _newObject = newObject;
            _position = pos;
            _container = container;
        }

        public void Execute() {
            var obj = SceneEditorHelper.InstantiatePrefabLink(_newObject.prefab, _position);
            if (!string.IsNullOrEmpty(_newObject.SceneObjectKey)) {
                SceneEditorHelper.SetOffsetSceneObject(obj, _newObject.SceneObjectKey);
            }

            _createdObject = new SceneObjectDataContainer(-1, _newObject.SceneObjectKey, _position, _newObject.GetSize(), obj.transform, _newObject);
            _container.Add(_createdObject);
        }

        public void Undo() {
            _container.Remove(_createdObject);
            Object.DestroyImmediate(_createdObject.ObjTransform.gameObject);
        }
    }
}