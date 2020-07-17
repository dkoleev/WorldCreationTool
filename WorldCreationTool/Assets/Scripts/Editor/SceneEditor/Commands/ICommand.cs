namespace Editor.SceneEditor.Commands {
    public interface ICommand {
        void Execute();
        void Undo();
    }
}
