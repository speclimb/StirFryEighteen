using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CardListExtension;

namespace TestBotIS
{
	class ResponseFromMsg
	{
		public static Person Player;

		/// <summary>
		/// DiscordのSocketUserからPersonインスタンスを生成して返す
		/// </summary>
		/// <returns>SocketUserから生成されたPersonインスタンス</returns>
		public static async Task JudgeMsg(SocketUserMessage NowMsg)
		{
			var CommandContext = NowMsg.Content;
			var CommandList = new List<string>(CommandContext.Split(',', ' ', '　'));
			// コマンド("おはよう")かどうか判定
			if (Program._IsGame == false)
			{
				Program._GameChannel = NowMsg.Channel;
				switch (CommandList[0].ToLower())
				{
					case "!a":
						await NowMsg.Channel.SendMessageAsync("Hello!");
						await NowMsg.Channel.SendMessageAsync(CommandList[1]);
						await NowMsg.Channel.SendMessageAsync(NowMsg.Author.Id.ToString());
						break;

					//_PersonList末尾に!jコマンド入力者の情報を追加
					case "!j":
						Program._PersonList.Add(GetOnePerson(NowMsg));
						break;
					//全参加者を一覧表示
					case "!ls":

						string str = "";
						str += "プレイヤー総数:" + Person.NumberOfPerson.ToString() + "\n";
						for (int i = 0; i < Person.NumberOfPerson; i++)
						{
							str += "プレイヤー情報：ID=" + Program._PersonList[i].ID.ToString()
							 + ", 名前=" + Program._PersonList[i].Name
							 + ", 役割=" + Program._PersonList[i].GetJob().ToString() + "\n";
						}
						str += "\n";
						await NowMsg.Channel.SendMessageAsync(str);
						break;
					case "!r":
						Program._PersonList.Clear();
						Person.NumberOfPerson = 0;
						await NowMsg.Channel.SendMessageAsync("プレイヤー情報クリア");
						break;
					case "!s":
						//参加プレイヤー全員にカードを3枚配る
						Program._GameChannel = NowMsg.Channel;
						foreach (Person p in Program._PersonList)
						{
							await CardListHandler.DealCardToPerson(Program._Deck, p, 3);
							p.SortHand();
							await CardListHandler.SendMsgToUserHand(p);
						}
						Program._Deck.DisplayCardList();
						Program._IsGame = true;
						await Program._GameChannel.SendMessageAsync("ゲームスタート!");
						Program._TurnIndex = 0;
						Player = Program._PersonList[Program._TurnIndex];
						await Program._GameChannel.SendMessageAsync("---\n" + Player.Name + Player.Number.ToString() + "の番です");
						await Player.socketUser.SendMessageAsync(Player.Name + Player.Number.ToString() + "の番です");
						await CardListHandler.DealCardToPerson(Program._Deck, Player, 1);
						await DisplayInfo();
						await CardListHandler.SendMsgToUserHand(Player);
						break;
					case "!start":

						break;
				}
			}
			else if (!Program._IsDiscardDownPhase && NowMsg.Author.Id == Player.socketUser.Id)  //ゲーム中
			{
				string str;
				int score;
				bool hantei;
				Player = Program._PersonList[Program._TurnIndex];
				switch (CommandList[0].ToLower())
				{
					// 一枚カードを引く
					case "!h":
						await CardListHandler.DealCardToPerson(Program._Deck, Player, 1);
						Player.SortHand();
						await CardListHandler.SendMsgToUserHand(Player);
						break;
					case "!hand":
						await CardListHandler.SendMsgToUserHand(Player);
						break;
					// 点数チェック
					case "!check":
						(hantei, score, str) = CalcScore(CommandList);
						FieldToPerson();	//_Fieldに出ているカードをプレイヤーに返す
						await Player.socketUser.SendMessageAsync(str);
						break;
					// カードを場に出して採点する
					case "!cook":
						(hantei, score, str) = CalcScore(CommandList);
						await Player.socketUser.SendMessageAsync(str);

						// 点数判定が成立しない場合は手札にカードを戻す
						if (hantei != true)
						{
							FieldToPerson();	//_Fieldに出ているカードをプレイヤーに返す
							break;
						}
						await Program._GameChannel.SendMessageAsync(Player.Name + "は調理を行った。\n" + str);
						// var templist = Program._Field.DeepCopy();
						Program._Trash.AddRange(Program._Field.DeepCopy());
						Program._Field.Clear();
						Player.AddScore(score);
						Player.IsCooked = true;
						await StartNextTurn();
						break;
					// カードを場を出さずにターン終了する
					case "!nocook":
						await Program._GameChannel.SendMessageAsync(Player.Name + "は調理を行わなかった。\n");
						await StartNextTurn();
						break;
					// カードを場に出して味見審議する
					case "!discard":
						DiscardToField(CommandList);
						break;
				}
			}
			else if (Program._IsDiscardDownPhase && NowMsg.Author.Id == Player.socketUser.Id)   // ターン終了時のカード廃棄
			{
				switch (CommandList[0].ToLower())
				{
					case "!trash":
						var (Discards, RemainigCards) = Player.GetHand().FindCardList(CommandList);
						if (RemainigCards.Count > 3)
						{
							await Player.socketUser.SendMessageAsync("カードを3枚以下になるまで捨ててください");
							break;
						}
						Program._Trash.AddRange(Discards.DeepCopy());
						Player.SetHand(RemainigCards.DeepCopy());
						Program._IsDiscardDownPhase = false;
						await StartNextTurn();
						break;
				}
			}
			Console.WriteLine("Command Process Finished.");
		}

		/// <summary>
		/// DiscordのSocketUserからPersonインスタンスを生成して返す
		/// </summary>
		/// <returns>SocketUserから生成されたPersonインスタンス</returns>
		public static Person GetOnePerson(SocketUserMessage message)
		{
			var Person1 = new Person(message.Author, 1);
			message.Channel.SendMessageAsync("プレイヤー総数:" + Person.NumberOfPerson.ToString());
			message.Channel.SendMessageAsync("プレイヤー情報：ID=" + Person1.ID.ToString()
			 + ", 名前=" + Person1.Name + ", 役割=" + Person1.GetJob().ToString());
			return Person1;
		}

		/// <summary>
		/// カードのを場(_Field)に出して得点計算する
		/// </summary>
		/// <returns>名前や説明などが格納されたstring List</returns>
		public static (bool, int, string) CalcScore(List<string> CommandList)
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
		/// カードのを場に出して味見待機状態にする
		/// </summary>
		/// <returns>名前や説明などが格納されたstring List</returns>
		public static (int, string) DiscardToField(List<string> CommandList)
		{
			string str = "";
			string DeclaredName = "";
			CommandList.RemoveAt(0);
			if (CommandList.Count == 0)
				str = "捨てるカード名を指定して下さい";

			var CardNameList = CommandList.GetRange(1, CommandList.Count - 1);
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

			}
			else
			{
				str = "選択するカードは1枚か2枚です";
			}

			return (0, "ss");
		}


		/// <summary>
		/// 次のプレイヤーのターンに移行する
		/// </summary>
		/// <returns></returns>
		public static async Task StartNextTurn()
		{
			Program._Deck.AddRange(Program._Trash.DeepCopy());
			Program._Deck.Shuffle();
			Program._Trash.Clear();
			// 料理したらカードを一枚配る
			if (Player.IsCooked)
			{
				await CardListHandler.DealCardToPerson(Program._Deck, Player, 1);
				Player.SortHand();
				Player.IsCooked = false;
			}
			await CardListHandler.SendMsgToUserHand(Player);
			if (Player.GetHand().Count > 3)
			{
				await Program._GameChannel.SendMessageAsync("カードを3枚以下になるまで捨ててください");
				Program._IsDiscardDownPhase = true;
				return;
			}

			TurnIncrement();
			Player = Program._PersonList[Program._TurnIndex];
			await Program._GameChannel.SendMessageAsync("---\n" + Player.Name + Player.Number.ToString() + "の番です");
			await Player.socketUser.SendMessageAsync(Player.Name + Player.Number.ToString() + "の番です");
			await CardListHandler.DealCardToPerson(Program._Deck, Player, 1);
			Player.SortHand();
			await DisplayInfo();
			await CardListHandler.SendMsgToUserHand(Player);
		}

		/// <summary>
		/// TurnIndexを1進め，プレイヤー人数を上回ったら0に戻してスタートプレイヤーの手番とする
		/// </summary>
		/// <returns></returns>
		public static void TurnIncrement()
		{
			Program._TurnIndex++;
			if (Program._TurnIndex >= Program._PersonList.Count)
				Program._TurnIndex = 0;
		}
		
		/// <summary>
		/// TurnIndexを1進め，プレイヤー人数を上回ったら0に戻してスタートプレイヤーの手番とする
		/// </summary>
		/// <returns></returns>
		public static async Task DisplayInfo()
		{
			string str = "";
			str = "プレイヤー情報：\n";
			for (int i = 0; i < Person.NumberOfPerson; i++)
			{
				str += Program._PersonList[i].Name
					+ "，点数=" + Program._PersonList[i].GetScore().ToString()
					+ "，手札枚数=" + Program._PersonList[i].GetHand().Count + "\n";
			}
			str += "山札の枚数：" + Program._Deck.Count.ToString() + "\n";
			await Program._GameChannel.SendMessageAsync(str);
		}
		
		/// <summary>
		/// Program._Fieldに出ているカードを現在の手番プレイヤーに返す
		/// </summary>
		/// <returns></returns>
		public static void FieldToPerson()
		{
			List<Card> list1 = Player.GetHand().DeepCopy();
			var templist1 = Program._Field.DeepCopy();
			list1.AddRange(templist1);
			Player.SetHand(list1);
			Player.SortHand();
			Program._Field.Clear();
		}
	}
}
