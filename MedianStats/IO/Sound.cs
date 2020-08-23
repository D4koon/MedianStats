using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MedianStats.IO
{
	[SettingsSerializeAs(SettingsSerializeAs.Xml)]
	public class Sound
	{
		private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public string FilePath { get; set; }
		public double Volume { get; set; }

		/// <summary>
		/// ATTENTION: It is importent to keep the reference to the MediaPlayer-object, otherwise it can happen that it will get garbage-collected before the sound even plays.
		/// That means .Play() is called and then it is garbage-collected...
		/// </summary>
		private MediaPlayer mediaPlayer;

		/// <summary>Needed for serializiation</summary>
		public Sound() { }

		public Sound(string filePath, double volume)
		{
			this.FilePath = filePath;
			this.Volume = volume;
		}

		public void Play()
		{
			//logger.Debug("Play sound: " + FilePath);
			PlaySound(FilePath, Volume);
		}

		public void PlaySound(string soundRelativeFilePath, double volume)
		{
			PlaySound(new Uri(soundRelativeFilePath, UriKind.Relative), volume);
		}

		public void PlaySound(Uri soundFileUri, double volume)
		{
			if (volume > 0) {
				logger.Debug($"Play sound: '{soundFileUri}' Volume: {volume}");
				mediaPlayer = new MediaPlayer();
				mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
				mediaPlayer.Open(soundFileUri);
				mediaPlayer.Volume = volume;
				mediaPlayer.Play();
			}
		}

		private static void MediaPlayer_MediaFailed(object sender, ExceptionEventArgs e)
		{
			logger.Debug($"Play sound failed");
		}
	}

	[SettingsSerializeAs(SettingsSerializeAs.Xml)]
	public class SoundList
	{
		public List<Sound> Sounds { get; set; }

		public Sound this[int key]
		{
			get => Sounds[key];
			set => Sounds[key] = value;
		}
	}
}
