using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using CardListExtension;

namespace TestBotIS
{
	class Program
	{
		private DiscordSocketClient _client;
		public static CommandService _commands;
		public static IServiceProvider _services;
		public static List<Person> _PersonList;
		public static List<Card> _CardList;
		public static List<Card> _Deck;
		public static List<Card> _Trash;
		public static List<Card> _Field;
		public static ISocketMessageChannel _GameChannel;
		public static bool _IsGame;     //ゲーム中フラグ
		public static bool _IsDiscardDownPhase; //ターン終了時の手札カード調整フェイズフラグ
		public static bool _IsTasting;  //味見判断待ちフラグ
		public static int _TurnIndex;
		public static string _tokenstr;
		public static string _DeclaredName; //味見するときに宣言する名前
		public static string _githubstr;

		static void Main(string[] args)
		{
			_tokenstr = "";
			_githubstr = "https://github.com/speclimb/StirFryEighteen";
			using (System.IO.StreamReader reader = new System.IO.StreamReader("./DiscordToken.txt", System.Text.Encoding.GetEncoding("UTF-8")))
			{
				_tokenstr = reader.ReadLine().Trim();
			}
			_CardList = CardListHandler.CreateCardListFromCsv("./CardListNew.csv");
			_Deck = _CardList.Shuffle();
			_PersonList = new List<Person>(0);
			_Field = new List<Card>();
			_Trash = new List<Card>();
			_IsGame = false;
			_IsDiscardDownPhase = false;
			_TurnIndex = 0;
			new Program().MainAsync().GetAwaiter().GetResult();
		}

		public async Task MainAsync()
		{
			_client = new DiscordSocketClient(new DiscordSocketConfig
			{
				LogLevel = LogSeverity.Info
			});
			_client.Log += Log;
			_commands = new CommandService();
			_services = new ServiceCollection().BuildServiceProvider();
			_client.MessageReceived += CommandRecieved;
			_client.ReactionAdded += ReactionRecieved;

			//次の行に書かれているstring token = "hoge"に先程取得したDiscordTokenを指定する。
			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
			await _client.LoginAsync(TokenType.Bot, _tokenstr);
			await _client.StartAsync();

			await Task.Delay(-1);
		}

		/// <summary>
		/// 何かしらのメッセージの受信
		/// </summary>
		/// <param name="msgParam"></param>
		/// <returns></returns>
		private async Task CommandRecieved(SocketMessage messageParam)
		{
			var message = messageParam as SocketUserMessage;

			//デバッグ用メッセージを出力
			Console.WriteLine("{0} {1}:{2}", message.Channel.Name, message.Author.Username, message);
			//メッセージがnullの場合
			if (message == null)
				return;

			//発言者がBotの場合無視する
			if (message.Author.IsBot)
				return;


			var context = new CommandContext(_client, message);

			//ここから記述--------------------------------------------------------------------------
			await ResponseFromMsg.JudgeMsg(message);

		}

		private async Task ReactionRecieved(Cacheable<IUserMessage, ulong> a
											, ISocketMessageChannel ch, SocketReaction reac)
		{
			await ResponseFromReaction.JudgeReaction(reac);
		}
		private Task Log(LogMessage message)
		{
			Console.WriteLine(message.ToString());
			return Task.CompletedTask;
		}
	}
}