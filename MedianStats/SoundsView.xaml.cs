using MedianStats.IO;
using MedianStats.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MedianStats
{
	/// <summary>
	/// Interaction logic for SoundsView.xaml
	/// </summary>
	public partial class SoundsView : UserControl
	{
		private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public SoundsView()
		{
			InitializeComponent();

			// Only set the sliders in runtime and not at design-time. Because at design-time MainWindow.mainInstance will be null
			if (MainWindow.mainInstance != null && DesignerProperties.GetIsInDesignMode(this) == false) {
				model.LinkSounds(MainWindow.mainInstance.Sounds);
			}
		}
	}
}
