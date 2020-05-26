using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace MedianStats
{
	/// <summary>
	/// Interaction logic for NotifyHelper.xaml
	/// </summary>
	public partial class NotifyHelper : UserControl
	{
		public NotifyHelper()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var matches = MainWindow.mainInstance.notifier.FindItemsFromNotifierString(testString.Text);
			foreach (var item in matches) {
				matchesList.Items.Add(new ListBoxItem() { Content = item });
			}

			//	if (not @error) {
			//		if (IsIngame()) {
			//			notifierHelp($sInput);
			//		} else {/*else*/
			//			MsgBox($MB_ICONINFORMATION, "D2Stats", "You need to be ingame to do that.");
			//		}
			//	}
		}
	}
}
