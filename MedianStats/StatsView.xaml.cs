using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
	/// Interaction logic for StatsView.xaml
	/// </summary>
	public partial class StatsView : UserControl
	{
		private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		private readonly Stats stats = new Stats();
		public static List<StatsGroup> StatsGroups = null;

		public StatsView()
		{
			InitializeComponent();
			CreateStatsGroups();
		}

		public void UpdateStats()
		{
			stats.UpdateCache();

			UpdateStatsUI();
		}

		private void UpdateStatsUI()
		{
			// TODO: Is That Really needed anymore???????
			if (!CheckAccess()) {
				Dispatcher.Invoke(() => UpdateStatsUI());
				return;
			}

			foreach (var statsGroup in StatsGroups) {
				statsGroup.UpdateStatItems();
			}
		}

		public static void CreateStatsGroups()
		{
			if (StatsView.StatsGroups != null) {
				return;
			}

			var brushConverter = new BrushConverter();
			Brush fireBrush = Brushes.Red;
			Brush coldBrush = Brushes.Blue;
			Brush poisonBrush = Brushes.Green;
			Brush magicBrush = Brushes.Orange;
			Brush lightningBrush = (Brush)brushConverter.ConvertFromString("#FFCEBD00");

			var g1 = new StatsGroup(GT("Base_stats"), StatGroups.Basic);
			g1.AddStat(GT("Strength_Base") + ": {0} " + GT("Bonus") + ": {359}%/{900}");
			g1.AddStat(GT("Dexterity_Base") + ": {2} " + GT("Bonus") + ": {360}%/{901}");
			g1.AddStat(GT("Vitality_Base") + ": {3} " + GT("Bonus") + ": {362}%/{902}");
			g1.AddStat(GT("Energy_Base") + ": {1} " + GT("Bonus") + ": {361}%/{903}");

			var g2 = new StatsGroup(GT("Other_stats"), StatGroups.Basic);
			g2.AddStat("{76}% " + GT("Maximum_Life"));
			g2.AddStat("{77}% " + GT("Maximum_Mana"));

			g2.AddStat("{96}%/{67}% " + GT("Faster_Run/Walk"));
			g2.AddStat("{80}% " + GT("Magic_Find"));
			g2.AddStat("{79}% " + GT("Gold_Find"));
			g2.AddStat("{85}% " + GT("Experience_gained"));
			g2.AddStat("{479} " + GT("Maximum_Skill_Level"));
			g2.AddStat("{185} " + GT("Signets_of_Learning"), GT("Signets_of_Learning_description"), new int[] { 185, 400, 400 });
			//g2.AddItem("Veteran tokens", "On Nightmare and Hell difficulty, you can find veteran monsters near the end of|each Act. There are five types of veteran monsters, one for each Act||[Class Charm] + each of the 5 tokens ? returns [Class Charm] with added bonuses| +1 to [Your class] Skill Levels| +20% to Experience Gained", new int[] { 219, 1, 1});

			g2.AddStat("{278} " + GT("Strength_Factor_(SF)"));
			g2.AddStat("{485} " + GT("Energy_Factor_(EF)"));
			g2.AddStat("{904}% " + GT("Factor_cap."), GT("Factor_cap_description"));

			g2.AddStat("{409}% " + GT("Buff/Debuff/Cold_Skill_Duration"));
			g2.AddStat("{27}% " + GT("Mana_Regeneration"));

			var g3 = new StatsGroup(GT("Life_/_Mana"), StatGroups.Basic);
			g3.AddStat("{60}%/{62}% " + GT("Life/Mana_Stolen_per_Hit"));
			g3.AddStat("{86}/{138} " + GT("Life/Mana_after_each_Kill_(*aeK)"));
			g3.AddStat("{208}/{209} " + GT("Life/Mana_on_Striking_(*oS)"));
			g3.AddStat("{210}/{295} " + GT("Life/Mana_on_Attack_(*oA)"));

			var g4 = new StatsGroup(GT("Minions"), StatGroups.Basic);
			g4.AddStat("{444}% " + GT("Life"));
			g4.AddStat("{470}% " + GT("Damage"));
			g4.AddStat("{487}% " + GT("Resist"));
			g4.AddStat("{500}% " + GT("Attack_Rating_(AR)"));

			var g5 = new StatsGroup(GT("Other"), StatGroups.Basic);
			g5.AddStat(GT("Slain_Monsters_Rest_In_Peace_(RIP)"), "", new int[] { 108, 1, 1 });

			var g6 = new StatsGroup(GT("Resistance"), StatGroups.Defense);
			g6.AddStat("{39}% " + GT("Fire"), "", fireBrush);
			g6.AddStat("{43}% " + GT("Cold"), "", coldBrush);
			g6.AddStat("{41}% " + GT("Lightning"), "", lightningBrush);
			g6.AddStat("{45}% " + GT("Poison"), "", poisonBrush);
			g6.AddStat("{37}% " + GT("Magic"), "", magicBrush);
			g6.AddStat("{36}% " + GT("Physical"));

			g6.AddStat("{171}% " + GT("Total_Character_Defense_(TCD)"));
			g6.AddStat("{35} " + GT("Magic_Damage_Reduction_(MDR)"));
			g6.AddStat("{34} " + GT("Physical_Damage_Reduction_(PDR)"));
			g6.AddStat("{338}% " + GT("Dodge"), GT("Chance_to_avoid_melee_attacks_while_standing_still"));
			g6.AddStat("{339}% " + GT("Avoid"), GT("Chance_to_avoid_projectiles_while_standing_still"));
			g6.AddStat("{340}% " + GT("Evade"), GT("Chance_to_avoid_any_attack_while_moving"));

			g6.AddStat("{109}% " + GT("Curse_Length_Reduction_(CLR)"));
			g6.AddStat("{110}% " + GT("Poison_Length_Reduction_(PLR)"));

			var g7 = new StatsGroup(GT("Item_/_Skill"), StatGroups.Defense, GT("Speed_from_items_and_skills_behave_differently._Use_SpeedCalc_to_find_your_breakpoints"));
			g7.AddStat("{99}%/{69}% " + GT("Faster_Hit_Recovery_(FHR)"));
			g7.AddStat("{102}%/{69}% " + GT("Faster_Block_Rate_(FBR)"));

			var g8 = new StatsGroup(GT("Slow"), StatGroups.Defense);
			g8.AddStat("{150}%/{376}% " + GT("Slows_Target_/_Slows_Melee_Target"));
			g8.AddStat("{363}%/{493}% " + GT("Slows_Attacker_/_Slows_Ranged_Attacker"));

			var g9 = new StatsGroup(GT("Absorb_/_Flat_absorb"), StatGroups.Defense);
			g9.AddStat("{142}%/{143} " + GT("Fire"), "", fireBrush);
			g9.AddStat("{148}%/{149} " + GT("Cold"), "", coldBrush);
			g9.AddStat("{144}%/{145} " + GT("Lightning"), "", lightningBrush);
			g9.AddStat("{146}%/{147} " + GT("Magic"), "", magicBrush);

			var g10 = new StatsGroup(GT("Item_/_Skill"), StatGroups.Offense, GT("Speed_from_items_and_skills_behave_differently._Use_SpeedCalc_to_find_your_breakpoints"));
			g10.AddStat("{93}%/{68}% " + GT("Increased_Attack_Speed_(IAS)"));
			g10.AddStat("{105}%/0% " + GT("Faster_Cast_Rate_(FCR)"));

			var g11 = new StatsGroup(GT("Offens"), StatGroups.Offense);
			g11.AddStat("{25}% " + GT("Enchanced_Weapon_Damage_(EWD)"));
			g11.AddStat("{119}% " + GT("Attack_Rating_(AR)"));
			g11.AddStat("{136}% " + GT("Crushing_Blow._Chance_to_deal_physical_damage_based_on_target's_current_health_(CB)"));
			g11.AddStat("{141}% " + GT("Deadly_Strike._Chance_to_double_physical_damage_of_attack_(DS)"));
			g11.AddStat("{164}% " + GT("Uninterruptable_Attack_(UA)"));
			g11.AddStat("{489} " + GT("Target_Takes_Additional_Damage_(TTAD)"));

			var g12 = new StatsGroup(GT("Spell_damage_/_-Enemy_resist"), StatGroups.Offense);
			g12.AddStat("{329}%/{333}% " + GT("Fire"), "", fireBrush);
			g12.AddStat("{331}%/{335}% " + GT("Cold"), "", coldBrush);
			g12.AddStat("{330}%/{334}% " + GT("Lightning"), "", lightningBrush);
			g12.AddStat("{332}%/{336}% " + GT("Poison"), "", poisonBrush);
			g12.AddStat("{431}% " + GT("Poison_Skill_Duration_(PSD)"), "", poisonBrush);
			g12.AddStat("{357}%/0% " + GT("Physical/Magic"), "", magicBrush);

			var g13 = new StatsGroup(GT("Weapon_Damage"), StatGroups.Offense);
			g13.AddStat("{48}-{49} " + GT("Fire"), "", fireBrush);
			g13.AddStat("{54}-{55} " + GT("Cold"), "", coldBrush);
			g13.AddStat("{50}-{51} " + GT("Lightning"), "", lightningBrush);
			g13.AddStat("{57}-{58} " + GT("Poison/sec"), "", poisonBrush);
			g13.AddStat("{52}-{53} " + GT("Magic"), "", magicBrush);
			g13.AddStat("{21}-{22} " + GT("One-hand_physical_damage._Estimated;_may_be_inaccurate,_especially_when_dual_wielding"));
			g13.AddStat("{23}-{24} " + GT("Two-hand/Ranged_physical_damage._Estimated;_may_be_inaccurate,_especially_when_dual_wielding"));

			var statsGroups = new List<StatsGroup>();
			statsGroups.AddRange(new[] { g1, g2, g3, g4, g5, g6, g7, g8, g9, g10, g11, g12, g13 });

			StatsView.StatsGroups = statsGroups;

			// GetTranslation()
			string GT(string key)
			{
				// WARNING: The null-check for MainWindow.mainInstance is only needed at design-time so it will display the labels (in StatsView.xaml). So dont remove it!
				if (MainWindow.mainInstance == null || (bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(StatsView)).DefaultValue)) {
					// Replace "_" with " " so it is more easy to read at design-time.
					return key.Replace("_", " ");
				}
				var translationString = MainWindow.mainInstance?.Resources.MergedDictionaries[0][key] as string;
				if (translationString == null) {
					throw new Exception($"No translation found for key '{key}'");
				}
				return translationString;
			}
		}

		public enum StatGroups
		{
			Basic,
			Defense,
			Offense,
		}

		public class StatsGroup : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;

			public StatGroups StatGroup;
			public string ShortDescription { get; set; }
			public string FullDescription { get; set; }

			public List<StatsItem> StatsItemList { get; set; } = new List<StatsItem>();

			public StatsGroup(string shortDescription, StatGroups statGroup, string fullDescription = "")
			{
				this.ShortDescription = "==== " + shortDescription + " ====";
				this.StatGroup = statGroup;
				this.FullDescription = fullDescription;
			}

			public void AddStat(string text, string description = "", Brush color = null)
			{
				var statsItem = new StatsItem(text, description, color);
				ConnectePropertyChangedEvent(statsItem);
				StatsItemList.Add(statsItem);
			}

			/// <summary>Statitem that changes color depending on value { statindex, thresholdGreen thresholdGold }</summary>
			public void AddStat(string text, string description, int[] dynamicColor)
			{
				var statsItem = new StatsItem(text, description, dynamicColor);
				ConnectePropertyChangedEvent(statsItem);
				StatsItemList.Add(statsItem);
			}

			private void ConnectePropertyChangedEvent(StatsItem statsItem)
			{
				statsItem.PropertyChanged += (s, e) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e.PropertyName));
			}

			public void UpdateStatItems()
			{
				foreach (var statsItem in StatsItemList) {
					statsItem.Update();
				}
			}
		}

		public class StatsItem : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;

			private string formatString;
			private string description;
			private int[] dynamicColor = null;
			private List<int> statIndexList = null;
			private List<int> statValueList = null;

			/// <summary>Important: Freeze brush after assigned, because it will be used in the UI-thread and will otherwise throw an exception!</summary>
			public Brush Color { get; set; } = Brushes.Black;

			public string Text
			{
				get {
					var statValueStringList = new List<string>();
					foreach (var statValue in statValueList) {
						statValueStringList.Add(statValue.ToString());
					}

					string resultText = string.Format(formatString, statValueStringList.ToArray());

					if (description.Length > 0) {
						resultText += " | " + description;
					}

					return resultText;
				}
			}

			public StatsItem(string formatString, string description, Brush color)
			{
				statIndexList = GetStatIndexList(formatString);
				this.formatString = ReformatFormatString(formatString);
				this.description = description;
				if (color != null) {
					this.Color = color;
				}

				UpdateValues();
			}

			public StatsItem(string formatString, string description = "", int[] dynamicColor = null)
			{
				statIndexList = GetStatIndexList(formatString);
				this.formatString = ReformatFormatString(formatString);
				this.description = description;
				this.dynamicColor = dynamicColor;

				UpdateValues();
			}

			private List<int> GetStatIndexList(string formatString)
			{
				var tempStatIndexList = new List<int>();

				var matches = Regex.Matches(formatString, "{(\\d+)}");
				for (int i = 0; i < matches.Count; i++) {
					var statIndex = int.Parse(matches[i].Groups[1].Value);
					tempStatIndexList.Add(statIndex);
				}

				return tempStatIndexList;
			}

			private string ReformatFormatString(string formatString)
			{
				string newFormatString = formatString;

				for (int i = 0; i < statIndexList.Count; i++) {
					newFormatString = newFormatString.Replace("{" + statIndexList[i] + "}", "{" + i + "}");
				}

				return newFormatString;
			}

			public void Update()
			{
				UpdateColor();
				UpdateValues();
			}

			private void UpdateColor()
			{
				Brush newColor = Color;

				if (dynamicColor != null && dynamicColor.Length == 3) {

					int statValue = Stats.GetStatValue(dynamicColor[0]);
					if (statValue >= dynamicColor[1]) {
						newColor = Brushes.Green;
					} else if (statValue >= dynamicColor[2]) {
						newColor = Brushes.LightGoldenrodYellow;
					} else {
						newColor = Brushes.Red;
					}
				}

				newColor.Freeze();
				if (newColor != Color) {
					Color = newColor;
					this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
				}
			}

			private void UpdateValues()
			{
				var tempStatValueList = new List<int>();
				bool propertyChanged = false;

				for (int i = 0; i < statIndexList.Count; i++) {
					var statValue = Stats.GetStatValue(statIndexList[i]);
					tempStatValueList.Add(statValue);

					// Check for statValueList != null because on the first execution (initialization) there is no previous data...
					if (statValueList != null && statValue != statValueList[i]) {
						propertyChanged = true;
					}
				}

				this.statValueList = tempStatValueList;

				// Trigger the cahnged-event after the new list is set.
				if (propertyChanged == true) {
					this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
				}
			}
		}
	}
}
