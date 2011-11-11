﻿namespace BrightIdeasSoftware
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Forms;

    [ToolboxItem(false)]
    public class AutoCompleteCellEditor : ComboBox
    {
        public AutoCompleteCellEditor(ObjectListView lv, OLVColumn column)
        {
            base.DropDownStyle = ComboBoxStyle.DropDown;
            Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
            for (int i = 0; i < Math.Min(lv.GetItemCount(), 0x3e8); i++)
            {
                string stringValue = column.GetStringValue(lv.GetModelObject(i));
                if (!dictionary.ContainsKey(stringValue))
                {
                    base.Items.Add(stringValue);
                    dictionary[stringValue] = true;
                }
            }
            base.Sorted = true;
            base.AutoCompleteSource = AutoCompleteSource.ListItems;
            base.AutoCompleteMode = AutoCompleteMode.Append;
        }
    }
}

