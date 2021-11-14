using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text.RegularExpressions;
using CardListExtension;

namespace TestBotIS
{
	static class CardListHandler
	{
		// csvファイルからカードリストを作成する
		public static List<Card> CreateCardListFromCsv(string filepath)
		{
			var CardList = new List<Card>(0);
			int CardID = 0;
			using (System.IO.StreamReader reader = new System.IO.StreamReader(filepath, System.Text.Encoding.GetEncoding("UTF-8")))
			{
				while (!reader.EndOfStream)
				{
					string l = reader.ReadLine();
					string[] ary = l.Split(',');
					var list = ary.ToList();
					var list2 = new List<string>();

					if (list[0].Contains('#')) continue;
					foreach (string str in list)
					{
						if (!string.IsNullOrEmpty(str)) { list2.Add(str.Trim()); }
						else { list2.Add("なし"); }
					}

					string Name = list2[0];
					string Kind = Regex.Replace(list2[1], @"[0-9]", "");
					string DrawNumberStr = Regex.Replace(list2[1], @"[^0-9]", "");
					int DrawNumber = 0;
					if (DrawNumberStr != "")
						DrawNumber = int.Parse(DrawNumberStr);

					string FlagKind = list2[2];

					double Number = double.Parse(list2[3]);
					List<double> FlagNumberList = list2.GetRange(4, 3).ConvertAll(x => double.Parse(x));
					List<string> FlagStrList = list2.GetRange(7, 3);
					string ImgName = list2[10];
					string Note = list2[11];

					var Card1 = new Card(CardID, Name, Kind, Number, DrawNumber, FlagKind
										, FlagNumberList, FlagStrList, ImgName, Note);
					CardList.Add(Card1);
					CardID++;
				}
			}
			return CardList;
		}

		// listからpersonにNumber枚のカードを配り，引いたカードをDMで通知する
		public static async Task<int> DealCardToPerson(List<Card> list, Person person, int Number)
		{
			if (list.Count < Number)
			{
				return -1;
			}
			var newList = new List<Card>(0);
			newList = person.GetHand();
			var gotList = list.GetRange(0, Number).DeepCopy();
			newList.AddRange(gotList);
			list.RemoveRange(0, Number);
			await person.socketUser.SendMessageAsync(String.Join(", ", gotList.GetStrCardName()) + "を引いた");
			return 0;
		}

		//現在の手札カード一覧をDMでプレイヤーに通知する
		public static async Task<int> SendMsgToUserHand(Person person)
		{
			// 山札から引いたカード情報をList<string>で受け取り，\n区切りで一つの配列にする
			StringBuilder strPlayer = new StringBuilder("");
			string str = String.Join("\n", person.GetHand().GetStrCardList().ToArray());
			strPlayer.Append(str);

			var embed = new EmbedBuilder();
			embed.WithTitle("プレイヤーNo." + person.Number.ToString() + "の手札カード");
			embed.WithAuthor(person.socketUser.Username, person.socketUser.GetAvatarUrl() ?? person.socketUser.GetDefaultAvatarUrl());
			embed.WithColor(Color.Blue);
			embed.WithDescription(strPlayer.ToString());
			await person.socketUser.SendMessageAsync(null, false, embed.Build());
			return 0;
		}

		/// <summary>
		/// Program._Fieldに出ているカードを現在の手番プレイヤーに返す
		/// </summary>
		/// <returns></returns>
		public static void FieldToPerson(Person Player)
		{
			List<Card> list1 = Player.GetHand().DeepCopy();
			var templist1 = Program._Field.DeepCopy();
			list1.AddRange(templist1);
			Player.SetHand(list1);
			Player.SortHand();
			Program._Field.Clear();
		}

		/// <summary>
		/// カードを場(_Field)に出して得点計算する
		/// </summary>
		/// <returns>名前や説明などが格納されたstring List</returns>
		public static (bool, int, string) CalcScore(Person Player, List<string> CommandList)
		{
			int score = 0;
			string str = "";
			if (CommandList.Count == 0)
				str = "点数計算するカード名を指定して下さい";

			var CardNameList = CommandList.GetRange(1, CommandList.Count - 1);
			str = "選択カード：" + String.Join(", ", CardNameList) + "\n";
			var (SelectCardList, NonSelectedCardList) = Player.GetHand().FindCardList(CardNameList);

			Program._Field = SelectCardList.DeepCopy();
			Player.SetHand(NonSelectedCardList.DeepCopy());

			string strscore = "";
			bool hantei;
			(hantei, score, strscore) = Program._Field.CalcScore();
			str += String.Format($"合計点数：{score}\n{strscore}");
			return (hantei, score, str);
		}

		/// <summary>
		/// カードを場(_Field)に出して味見待機状態にする
		/// </summary>
		/// <returns>名前や説明などが格納されたstring List</returns>
		public static (int, string) DiscardToField(Person Player, List<string> CommandList)
		{
			string str = "";
			string DeclaredName = "";
			CommandList.RemoveAt(0);
			if (CommandList.Count == 0)
				str = "捨てるカード名を指定して下さい";

			var CardNameList = CommandList.GetRange(0, CommandList.Count);
			str = "選択カード：" + String.Join(", ", CardNameList) + "\n";
			var (SelectCardList, NonSelectedCardList) = Player.GetHand().FindCardList(CardNameList);

			if (SelectCardList.Count == 1)
			{
				foreach (string cmd in CommandList)
				{
					if (cmd.StartsWith('!'))
					{
						DeclaredName = Regex.Replace(cmd, @"[!]", "");
					}
				}
			}
			else if (SelectCardList.Count == 2)
			{
				DeclaredName = "野菜";
			}
			else
			{
				str = "選択するカードは1枚か2枚です";
				return (-1, "");
			}

			Program._Field = SelectCardList.DeepCopy();
			Player.SetHand(NonSelectedCardList.DeepCopy());

			return (0, DeclaredName);
		}
	}
}