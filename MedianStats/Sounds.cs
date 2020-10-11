using MedianStats.IO;
using MedianStats.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedianStats
{
	public class Sounds
	{
		/// <summary>
		/// Index 0 = sound1
		/// Index 1 = sound2
		/// ...
		/// </summary>
		public List<Sound> List;

		public Sounds()
		{
			List = Settings.Default.notifierSounds.Sounds;
		}
	}
}
