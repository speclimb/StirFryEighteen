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
		/// 投稿されたSocektMessageから規定通りの行動をする
		/// </summary>
		/// <returns></returns>
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
						(hantei, score, str) = CardListHandler.CalcScore(Player, CommandList);
						CardListHandler.FieldToPerson(Player);    //_Fieldに出ているカードをプレイヤーに返す
						await Player.socketUser.SendMessageAsync(str);
						break;
					// カードを場に出して採点する
					case "!cook":
						(hantei, score, str) = CardListHandler.CalcScore(Player, CommandList);
						await Player.socketUser.SendMessageAsync(str);

						// 点数判定が成立しない場合は手札にカードを戻す
						if (hantei != true)
						{
							CardListHandler.FieldToPerson(Player);    //_Fieldに出ているカードをプレイヤーに返す
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
						int x = 0;
						(x, Program._DeclaredName) = CardListHandler.DiscardToField(Player, CommandList);
						if (x == -1)
						{
							CardListHandler.FieldToPerson(Player);
							break;
						}
						await SendTastingQuestion();
						Program._IsTasting = true;
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
		/// 各プレイヤーの情報や山札の枚数といった情報をDiscordチャンネルに投稿する
		/// </summary>
		/// <returns></returns>
		public static async Task DisplayInfo()
		{
			var embed = new EmbedBuilder();
			embed.WithColor(Color.Red);
			embed.WithTitle("ゲーム状況");
			string str = "";

			for (int i = 0; i < Person.NumberOfPerson; i++)
			{
				str = "点数=" + Program._PersonList[i].GetScore().ToString()
					+ "，手札枚数=" + Program._PersonList[i].GetHand().Count + "\n";
				embed.AddField(Program._PersonList[i].Name, str, false);
			}
			str = Program._Deck.Count.ToString() + "\n";
			embed.AddField("山札の枚数：" ,str, false);
			await Program._GameChannel.SendMessageAsync(null, false, embed.Build());
		}


		/// <summary>
		/// Program._GameChannelに味見するかしないかを投稿する
		/// </summary>
		/// <returns></returns>
		public static async Task SendTastingQuestion()
		{
			if (Program._Field.Count == 0)
			{
				return;
			}

			IEmote[] emotes = new IEmote[2];
			var embed = new EmbedBuilder();
			embed.WithTitle("味見するかい？");
			embed.WithAuthor(Player.socketUser.Username, Player.socketUser.GetAvatarUrl() ?? Player.socketUser.GetDefaultAvatarUrl());
			embed.WithColor(Color.Green);
			// 念のためnullで初期化
			string description = null;
			description += Program._DeclaredName + "を捨てると言ってカードを" + Program._Field.Count + "枚出した\n";
			// 表示する選択肢一覧をdescriptionに設定
			description += (new Emoji("🍴")).ToString() + "：嘘に違いない。味見する" + "\n";
			emotes[0] = new Emoji("🍴");
			description += (new Emoji("👍")).ToString() + "：" + Player.Name + "を信用する" + "\n";
			emotes[1] = new Emoji("👍");

			embed.WithDescription(description);
			await Program._GameChannel.SendMessageAsync(null, false, embed.Build()).GetAwaiter().GetResult().AddReactionsAsync(emotes);
		}
		public static async Task SendSentakushi(params string[] msg)
		{
			// エモート用のリスト（リアクションを設定するときに使用）
			IEmote[] emotes = new IEmote[0];
			var embed = new EmbedBuilder();
			embed.WithTitle("選択肢");
			embed.WithColor(Color.Green);
			// 念のためnullで初期化
			string description = null;
			// 表示する選択肢一覧をdescriptionに設定
			for (int i = 1; i < msg.Length; i++)
			{
				description += (new Emoji(iconUni[i - 1])).ToString() + msg[i] + "\n";
				// 選択肢一覧で使用した絵文字をエモートの配列に追加（この時配列をリサイズする）
				Array.Resize(ref emotes, i);
				emotes[i - 1] = new Emoji(iconUni[i - 1]);
			}
			embed.WithDescription(description);
			await Program._GameChannel.SendMessageAsync(null, false, embed.Build()).GetAwaiter().GetResult().AddReactionsAsync(emotes);
		}

		private static string[] iconUni = { "\uD83C\uDDE6",
					 "\uD83C\uDDE7",
					 "\uD83C\uDDE8",
					 "\uD83C\uDDE9",
					 "\uD83C\uDDEA",
					 "\uD83C\uDDEB",
					 "\uD83C\uDDEC",
					 "\uD83C\uDDED",
					 "\uD83C\uDDEE",
					 "\uD83C\uDDEF",
					 "\uD83C\uDDF0",
					 "\uD83C\uDDF1",
					 "\uD83C\uDDF2",
					 "\uD83C\uDDF3",
					 "\uD83C\uDDF4",
					 "\uD83C\uDDF5",
					 "\uD83C\uDDF6",
					 "\uD83C\uDDF7",
					 "\uD83C\uDDF8",
					 "\uD83C\uDDF9",
					 "\uD83C\uDDFA",
					 "\uD83C\uDDFB",
					 "\uD83C\uDDFC",
					 "\uD83C\uDDFD",
					 "\uD83C\uDDFE",
					 "\uD83C\uDDFF" };
	}
}
