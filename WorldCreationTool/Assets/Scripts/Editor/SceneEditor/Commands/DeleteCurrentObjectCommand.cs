using System.Collections.Generic;

namespace Editor.SceneEditor.Commands {
    public class DeleteCurrentObjectCommand : ICommand {
        private readonly SceneObjectDataContainer _objectToDelete;
        private readonly List<SceneObjectDataContainer> _currentContainer;
        private readonly List<SceneObjectDataContainer> _toDeleteContainer;
        
        public DeleteCurrentObjectCommand(List<SceneObjectDataContainer> currentContainer, List<SceneObjectDataContainer> toDeleteContainer, SceneObjectDataContainer objectToDelete) {
            _objectToDelete = objectToDelete;
            _currentContainer = currentContainer;
            _toDeleteContainer = toDeleteContainer;
        }

        public void Execute() {
            _currentContainer.Remove(_objectToDelete);
            _toDeleteContainer.Add(_objectToDelete);
            _objectToDelete.ObjTransform.gameObject.SetActive(false);
        }

        public void Undo() {
            _toDeleteContainer.Remove(_objectToDelete);
            _currentContainer.Add(_objectToDelete);
            _objectToDelete.ObjTransform.gameObject.SetActive(true);
        }
    }
}