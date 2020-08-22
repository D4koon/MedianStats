using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static MedianStats.StatsView;

namespace MedianStats
{
	public class StatsViewModel : MVVM.ViewModelBase
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public ItemsChangeObservableCollection<StatsGroup> statsListBasic { get; set; } = new ItemsChangeObservableCollection<StatsGroup>();
		public ItemsChangeObservableCollection<StatsGroup> statsListDefens { get; set; } = new ItemsChangeObservableCollection<StatsGroup>();
		public ItemsChangeObservableCollection<StatsGroup> StatsListOffens { get; set; } = new ItemsChangeObservableCollection<StatsGroup>();

		public StatsViewModel()
		{
			StatsView.CreateStatsGroups();

			var tempStatsListBasic = new ItemsChangeObservableCollection<StatsGroup>();
			//tempStatsListBasic.Add(new StatsItem("dummy_text", "dummy_desc", Brushes.Violet) {  });
			var tempStatsListDefens = new ItemsChangeObservableCollection<StatsGroup>();
			var tempStatsListOffens = new ItemsChangeObservableCollection<StatsGroup>();

			foreach (var statsGroup in StatsView.StatsGroups) {
				ItemsChangeObservableCollection<StatsGroup> curList;
				switch (statsGroup.StatGroup) {
					case StatGroups.Basic:
						curList = tempStatsListBasic;
						break;
					case StatGroups.Defense:
						curList = tempStatsListDefens;
						break;
					case StatGroups.Offense:
						curList = tempStatsListOffens;
						break;
					default:
						throw new Exception("Button_Click_Read() - Unknown StatGroup \"" + statsGroup.StatGroup + "\"");
				}
				curList.Add(statsGroup);
			}

			statsListBasic = tempStatsListBasic;
			statsListDefens = tempStatsListDefens;
			StatsListOffens = tempStatsListOffens;
		}
	}
}
