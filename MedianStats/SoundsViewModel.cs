using MedianStats.IO;
using MedianStats.Properties;
using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MedianStats
{
	public class SoundsViewModel : MVVM.ViewModelBase
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public ItemsChangeObservableCollection<SoundConfig> Sounds { get; set; } = new ItemsChangeObservableCollection<SoundConfig>();

		public SoundsViewModel()
		{
			var tempSounds = new ItemsChangeObservableCollection<SoundConfig>();

			for (int i = 0; i < 5; i++) {
				var sound = new Sound() { FilePath = "C:/dummy.wav", Volume = 0.2 };
				var soundConfig = new SoundConfig(sound) { ID = i };

				tempSounds.Add(soundConfig);
			}

			Sounds = tempSounds;
		}

		public void LinkSounds(List<Sound> soundsList)
		{
			var tempSounds = new ItemsChangeObservableCollection<SoundConfig>();

			for (int i = 0; i < soundsList.Count; i++) {
				var sound = soundsList[i];

				var soundConfig = new SoundConfig(sound) { ID = i };
				tempSounds.Add(soundConfig);
			}

			Sounds = tempSounds;
			this.NotifyPropertyChanged(nameof(Sounds));
		}
	}

	public class SoundConfig : MVVM.ViewModelBase
	{
		private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		readonly Sound sound;

		public int ID { get; set; } = 0;
		public string SoundName { get { return "sound" + (ID + 1); } }
		public string FilePath { get => sound.FilePath; }
		public double Volume { get => sound.Volume; set => OnVolumeChanged(value); }

		public SoundConfig(Sound sound)
		{
			this.sound = sound;
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
				return _chooseCommand ?? (_chooseCommand = new CommandHandler(() => ShowDialog(), () => true));
			}
		}

		private void ShowDialog()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.InitialDirectory = MainWindow.ExeDir + "\\resources";
			if (openFileDialog.ShowDialog() == true) {
				sound.FilePath = openFileDialog.FileName;
				Settings.Default.notifierSounds[ID].FilePath = openFileDialog.FileName;
				Settings.Default.Save();
				NotifyPropertyChanged(nameof(FilePath));
			}
		}

		private void OnVolumeChanged(double newVolume)
		{
			// Slider has value from 0...1
			logger.Debug(newVolume);

			sound.Volume = newVolume;
			Settings.Default.notifierSounds[ID].Volume = newVolume;
			Settings.Default.Save();
		}
	}
}
