using System.Windows.Controls;

namespace Better_Printing_for_OneNote.Views.Controls
{
    public class IntValueTextBox : TextBox
    {
        private string oldText = "";

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Text) && !int.TryParse(Text, out int _))
            {
                Text = oldText;
                e.Handled = true;
            }
            else
            {
                oldText = Text;
                base.OnTextChanged(e);
            }
        }
    }
}
