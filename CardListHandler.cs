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
					string ImgURL = Program._GithubContentStr + "main/img/cards/" + ImgName;
					string Note = list2[11];

					var Card1 = new Card(CardID, Name, Kind, Number, DrawNumber, FlagKind
										, FlagNumberList, FlagStrList, ImgURL, Note);
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

			var embed = new EmbedBuilder();
			embed.WithTitle("プレイヤーNo." + person.Number.ToString() + "のドロー");
			embed.WithAuthor(person.socketUser.Username, person.socketUser.GetAvatarUrl() ?? person.socketUser.GetDefaultAvatarUrl());
			embed.WithColor(Color.Purple);
			string str = String.Join(", ", gotList.GetStrCardName()) + "を引いた";
			embed.WithDescription(str);
			await person.socketUser.SendMessageAsync(null, false, embed.Build());
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
			// embed.WithDescription(strPlayer.ToString());
			// Card card = person.GetHand()[0];
			foreach (Card card in person.GetHand())
			{
				embed.AddField(card.Name, card.Note, true);
			}
			// embed.WithImageUrl(person.GetHand()[0].ImgURL);	//画像は一枚しか出せないらしい
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
		/// <returns>(bool 成功/不成功, 宣言するカード名(あるいはエラーメッセージ))</returns>
		public static (bool, string) DiscardToField(Person Player, List<string> CommandList)
		{
			string str = "";
			string DeclaredName = "";
			CommandList.RemoveAt(0);
			if (CommandList.Count == 0)
			{
				str = "捨てるカード名を指定して下さい";
				return (false, str);
			}

			var CardNameList = CommandList.GetRange(0, CommandList.Count);
			str = "選択カード：" + String.Join(", ", CardNameList) + "\n";
			var (SelectCardList, NonSelectedCardList) = Player.GetHand().FindCardList(CardNameList);


			if (SelectCardList.Count == 0)
			{
				str = "捨てるカード名が間違っています";
				return (false, str);
			}
			else if (SelectCardList.Count == 1)
			{
				foreach (string cmd in CommandList)
				{
					if (cmd.StartsWith('!'))
					{
						DeclaredName = Regex.Replace(cmd, @"[!]", "");
						bool IsProtein = false;
						if (Player.IsOneCardDicarded == true)
						{
							str = "1枚捨てるのは既にやったためこのターンはできません";
							return (false, str);
						}
						//タンパク質のみを捨て札にできるので，宣言名がタンパク質か判定
						foreach (Card card in Program._CardList)
						{
							if (card.Name == DeclaredName && card.Kind == "タンパク質")
							{
								IsProtein = true;
								Player.IsOneCardDicarded = true;
								break;
							}
						}
						if (IsProtein == false)
						{
							str = "タンパク質以外のカードを宣言することは出来ません";
							return (false, str);
						}
					}
				}
			}
			else if (SelectCardList.Count == 2)
			{
				if (Player.IsTwoCardDicarded == true)
				{
					str = "2枚捨てるのは既にやったためこのターンはできません";
					return (false, str);
				}
				Player.IsTwoCardDicarded = true;
				DeclaredName = "野菜";
			}
			else
			{
				str = "選択するカードは1枚か2枚です";
				return (false, str);
			}

			Program._Field = SelectCardList.DeepCopy();
			Player.SetHand(NonSelectedCardList.DeepCopy());

			return (true, DeclaredName);
		}

		/// <summary>
		/// 味見に成功し，ブラフに失敗したPlayerはこれ以上味見審議する権利を失い，成功したPersonは次の手番に追加で1枚の手札を得る。
		/// </summary>
		/// <returns></returns>
		public static async Task TastingSuccsess(Person Player, Person person)
		{
			var embed = new EmbedBuilder();
			embed.WithTitle("味見成功");
			embed.WithAuthor(person.socketUser.Username, person.socketUser.GetAvatarUrl() ?? person.socketUser.GetDefaultAvatarUrl());
			embed.WithColor(Color.Green);
			string str = Player.Name + "が場に出したカードは" + String.Join(", ", Program._Field.GetStrCardName()) + "だった。\n";
			str += Player.Name + "はこのターン中はカードを新たに得ることはできません。\n";
			str += person.Name + "は次の手番に1枚多くドローできます。";
			embed.WithDescription(str);
			await Program._GameChannel.SendMessageAsync(null, false, embed.Build());

			Player.IsTwoCardDicarded = true;
			Player.IsOneCardDicarded = true;
			Player.IsDiscardFailed = true;
			person.NumberOfTasteSuccess++;
			return;
		}

		/// <summary>
		/// 味見に失敗し，宣言通りのカードを出していたPlayerはカードを引き，失敗したPersonは手札を失う(手札0の場合勝利点5を失う)。
		/// </summary>
		/// <returns></returns>
		public static async Task TastingFault(Person Player, Person person)
		{
			int DrawN = 3;
			if(Program._Field.Count != 2){
				// DrawN = Program._Field[0].DrawNumber;
				List<string> strlist = new List<string>() {Program._DeclaredName};
				(var list,_) = Program._CardList.FindCardList(strlist);
				DrawN = list[0].DrawNumber;
			}

			var embed = new EmbedBuilder();
			embed.WithTitle("味見失敗");
			embed.WithAuthor(person.socketUser.Username, person.socketUser.GetAvatarUrl() ?? person.socketUser.GetDefaultAvatarUrl());
			embed.WithColor(Color.Green);
			string str = Player.Name + "が場に出したカードは" + String.Join(", ", Program._Field.GetStrCardName()) + "だった。\n";
			str += Player.Name + "は新たに" + DrawN + "枚のカードを引きます。\n";
			if(person.GetHand().Count == 0){
				person.AddScore(-5);
				str += person.Name + "は手札がないので勝利点を5点失います。";
			}else{
				Program._Trash.AddRange(person.GetHand().DeepCopy());
				person.SetHand(new List<Card>(0));
				str += person.Name + "は手札をすべて失った。";
			}
			
			embed.WithDescription(str);
			await Program._GameChannel.SendMessageAsync(null, false, embed.Build());
			await CardListHandler.DealCardToPerson(Program._Deck, Player, DrawN);
			await SendMsgToUserHand(Player);
			return;
		}

		/// <summary>
		/// 味見は行われず，Playerは宣言通りにカードを引く。
		/// </summary>
		/// <returns></returns>
		public static async Task TastingThrough(Person Player){
			int DrawN = 3;
			if(Program._Field.Count != 2){
				// DrawN = Program._Field[0].DrawNumber;
				List<string> strlist = new List<string>() {Program._DeclaredName};
				(var list,_) = Program._CardList.FindCardList(strlist);
				DrawN = list[0].DrawNumber;
			}

			var embed = new EmbedBuilder();
			embed.WithTitle("味見は行われなかった");
			embed.WithAuthor(Player.socketUser.Username, Player.socketUser.GetAvatarUrl() ?? Player.socketUser.GetDefaultAvatarUrl());
			embed.WithColor(Color.Green);
			string str = Player.Name + "は新たに" + DrawN + "枚のカードを引きます。\n";
			embed.WithDescription(str);
			await Program._GameChannel.SendMessageAsync(null, false, embed.Build());
			await CardListHandler.DealCardToPerson(Program._Deck, Player, DrawN);
			await SendMsgToUserHand(Player);
		}
	}
}