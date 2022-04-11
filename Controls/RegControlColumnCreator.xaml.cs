﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ColdStorageManager.Models;

namespace ColdStorageManager.Controls
{
	/// <summary>
	/// Interaction logic for RegControlColumnCreator.xaml
	/// </summary>
	public partial class RegControlColumnCreator : UserControl
	{
		public RegControlColumnCreator()
		{
			InitializeComponent();
		}

		public ColumnInfo GetValue()
		{
			return new ColumnInfo(ColTypeCmbx.SelectedIndex, ColNameTxtBox.Text);
		}
	}
}
