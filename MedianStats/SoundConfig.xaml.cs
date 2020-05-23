using MedianStats.IO;
using MedianStats.Properties;
using System;
using System.Collections.Generic;
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
	/// Interaction logic for SoundConfig.xaml
	/// </summary>
	public partial class SoundConfig : UserControl
	{
		Sound sound;

		public Sound Sound
		{
			set {
				sound = value;
				slider.Value = value.Volume;
			}
		}
		public int ID;

		public SoundConfig()
		{
			InitializeComponent();

			DataContext = this;
		}

		private ICommand _playCommand;
		public ICommand PlayCommand
		{
			get {
				return _playCommand ?? (_playCommand = new CommandHandler(() => sound.Play(), () => true));
			}
		}

		private ICommand _chooseCommand;
		public ICommand ChooseCommand
		{
			get {
				return _chooseCommand ?? (_chooseCommand = new CommandHandler(() => sound.Play(), () => true));
			}
		}

		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			// Slider has value from 0...1
			var volume = ((Slider)sender).Value;
			Debug.WriteLine(volume);

			sound.Volume = volume;
			Settings.Default.notifierSounds[ID].Volume = volume;
			Settings.Default.Save();
		}
	}
}
