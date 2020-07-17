using System.Collections.Generic;

namespace Editor.SceneEditor.Commands {
    public class DeleteNewObjectCommand : ICommand {
        private readonly SceneObjectDataContainer _newObject;
        private readonly List<SceneObjectDataContainer> _container;
        private readonly SceneObjectBrush _brush;
        
        public DeleteNewObjectCommand(List<SceneObjectDataContainer> container, SceneObjectDataContainer newObject) {
            _newObject = newObject;
            _container = container;
        }

        public void Execute() {
            _container.Remove(_newObject);
            _newObject.ObjTransform.gameObject.SetActive(false);
        }

        public void Undo() {
            _newObject.ObjTransform.gameObject.SetActive(true);
            _container.Add(_newObject);
        }
    }
}