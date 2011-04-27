﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenIDENet.CodeEngine.Core.Caching;

namespace OpenIDENet.CodeEngine.Core.UI
{
    public partial class TypeSearchForm : Form
    {
		private ITypeCache _cache;
		private Action<string, int, int> _action;
		private Action _cancelAction;
		
        public TypeSearchForm(ITypeCache cache, Action<string, int, int> action, Action cancelAction)
        {
            InitializeComponent();
			Refresh();
			_cache = cache;
			_action = action;
			_cancelAction = cancelAction;
        }
		
		void HandleHandleFormClosing (object sender, FormClosingEventArgs e)
        {
			Visible = false;
			Dispose();
        }
		
		void HandleTextBoxSearchhandleTextChanged(object sender, System.EventArgs e)
        {
			try
			{
	        	informationList.Items.Clear();
				var items = _cache.Find(textBoxSearch.Text);
				if (items.Count > 30)
					items = items.GetRange(0, 30);
				items.ForEach(x => addItem(x));
				if (informationList.Items.Count > 0)
					informationList.Items[0].Selected = true;
			}
			catch (Exception ex)
			{
				informationList.Items.Add(ex.ToString());
				Console.WriteLine(ex.ToString());
			}
        }
		
		void HandleTextBoxSearchhandleKeyDown(object sender, KeyEventArgs e)
        {
			if (e.KeyCode == Keys.Enter)
			{
	        	if (informationList.SelectedItems.Count != 1)
					return;
				var type = (ICodeType) informationList.SelectedItems[0].Tag;
				_action.Invoke(type.Fullpath, type.Line, type.Column);
				Close();
			}
			else if (e.KeyCode == Keys.Down)
			{
				if (informationList.Items.Count == 0)
					return;
				if (informationList.SelectedItems.Count != 1)
					return;
				if (informationList.SelectedItems[0].Index < informationList.Items.Count - 1)
					informationList.Items[informationList.SelectedItems[0].Index + 1].Selected = true;
			}
			else if (e.KeyCode == Keys.Up)
			{
				if (informationList.Items.Count == 0)
					return;
				if (informationList.SelectedItems.Count != 1)
					return;
				if (informationList.SelectedItems[0].Index != 0)
					informationList.Items[informationList.SelectedItems[0].Index - 1].Selected = true;
			}
			else if (e.KeyCode == Keys.Escape)
			{
				_cancelAction.Invoke();
				Close();
			}
        }
		
		private void addItem(ICodeType type)
		{
			var item = informationList.Items.Add(type.Signature);
			item.Tag = type;
		}
    }
}
