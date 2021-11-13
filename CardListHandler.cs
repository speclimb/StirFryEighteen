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
					string Note = list2[10];

					var Card1 = new Card(CardID, Name, Kind, Number, DrawNumber, FlagKind
										, FlagNumberList, FlagStrList, Note);
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
			StringBuilder strPlayer = new StringBuilder("プレイヤーNo.");
			strPlayer.Append(person.Number.ToString());
			strPlayer.Append("\n手札カード：\n");
			string str = String.Join("\n", person.GetHand().GetStrCardList().ToArray());
			strPlayer.Append(str);
			strPlayer.Append("\n");
			await person.socketUser.SendMessageAsync(strPlayer.ToString());
			return 0;
		}
		

	}
}