using System.Collections.Generic;

namespace Editor.SceneEditor.Commands {
    public class UndoCommand : ICommand {
        private readonly List<ICommand> _commands;
        
        public UndoCommand(List<ICommand> commands) {
            _commands = commands;
        }

        public void Execute() {
            if (_commands.Count > 0) {
                _commands[_commands.Count - 1].Undo();
                _commands.RemoveAt(_commands.Count -1);
            }
        }

        public void Undo() {
        }
    }
}