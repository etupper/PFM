using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DBTableControl
{
    public partial class DBDataGrid : DataGrid
    {
        protected override void OnExecutedCopy(ExecutedRoutedEventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    base.OnExecutedCopy(e);
                    break;
                }
                catch { }
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
