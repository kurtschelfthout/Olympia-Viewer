using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace WpfOly
{
    class MoveCommand : ICommand
    {
        private ICSharpCode.AvalonEdit.TextEditor textEditor;
        private string direction;

        public MoveCommand(ICSharpCode.AvalonEdit.TextEditor textEditor, string direction)
        {
            this.textEditor = textEditor;
            this.direction = direction;

        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add {  }
            remove {  }
        }

        void ICommand.Execute(object parameter)
        {
            var offset = textEditor.TextArea.Caret.Offset;
            textEditor.Document.Insert(offset, "move " + direction + "\r\n");
        }
    }
}
