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
		//public int ID;
		public string FilePath { get; set; }
		public double Volume { get; set; }

		/// <summary>Needed for serializiation</summary>
		public Sound() { }

		public Sound(string filePath, double volume)
		{
			//ID = id;
			this.FilePath = filePath;
			this.Volume = volume;
		}

		public void Play()
		{
			Debug.WriteLine("Play sound: " + FilePath);
			PlaySound(FilePath, Volume);
		}

		public static void PlaySound(string soundRelativeFilePath, double volume)
		{
			PlaySound(new Uri(soundRelativeFilePath, UriKind.Relative), volume);
		}

		public static void PlaySound(Uri soundFileUri, double volume)
		{
			if (volume > 0) {
				var mediaPlayer = new MediaPlayer();
				mediaPlayer.Open(soundFileUri);
				mediaPlayer.Volume = volume;
				mediaPlayer.Play();
			}
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
