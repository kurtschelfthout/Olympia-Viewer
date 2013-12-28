using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Document;

namespace WpfOly
{
    class CompletionData : ICompletionData
    {
        public string Completion { get; private set; }

        public CompletionData(string text, string description, string completion)
        {
            this.Text = text;
            this.Completion = completion;
            this.Description = description;
        }

        public CompletionData(string text, string description)
            : this(text, description, text)
        { }

        public CompletionData(string text)
            : this(text, "")
        { }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.Completion);
        }

        public object Content
        {
            get { return this.Text; }
        }

        public object Description
        {
            get;
            private set;
        }

        public System.Windows.Media.ImageSource Image
        {
            get { return null; }
        }

        public double Priority
        {
            get { return 10.0; }
        }

        public string Text
        {
            get;
            private set;
        }
    }
}
