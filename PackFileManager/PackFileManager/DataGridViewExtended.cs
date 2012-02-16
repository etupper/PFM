using System.Windows.Forms;

namespace PackFileManager
{
	public class DataGridViewExtended : DataGridView
	{
        protected override bool ProcessDialogKey(Keys key)
        {
            return
                base.ProcessDialogKey(key) &&
                !((key & Keys.KeyCode) == Keys.C &&
                  (key & Keys.Control) == Keys.Control &&
                  (key & Keys.Alt) != Keys.Alt);

            //The Ctrl and C keys must be pressed but the Alt key cannot be pressed
            //This is the exact logic used by the DataGrid to trigger its copy implementation        }
        }
	}
}
