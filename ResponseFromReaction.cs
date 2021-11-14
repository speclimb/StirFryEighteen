using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CardListExtension;

namespace TestBotIS
{
	class ResponseFromReaction
	{
		private static Person ReactionPerson;
		public static async Task JudgeReaction(SocketReaction reac)
		{
			foreach (Person person in Program._PersonList)
			{
				if (person.ID == reac.UserId)
				{
					ReactionPerson = person;
					break;
				}
				return;
			}
			if (Program._IsTasting == false) return;
			// Console.WriteLine("aa");

			// if(reac.Emote == "🍴"){

			// }

			var embed = new EmbedBuilder();
			embed.WithTitle("リアクションした");
			embed.WithAuthor(ReactionPerson.socketUser.Username, ReactionPerson.socketUser.GetAvatarUrl() ?? ReactionPerson.socketUser.GetDefaultAvatarUrl());
			embed.WithColor(Color.Green);
			// 念のためnullで初期化
			string description = null;
			description += Program._DeclaredName + "を捨てると言ってカードを" + Program._Field.Count + "枚出した\n";
			// 表示する選択肢一覧をdescriptionに設定
			description += (new Emoji("🍴")).ToString() + "：嘘に違いない。味見する" + "\n";
			description += (new Emoji("👍")).ToString() + "：" + ReactionPerson.Name + "を信用する" + "\n";
			embed.WithDescription(description);
			await Program._GameChannel.SendMessageAsync(null, false, embed.Build());
			Program._IsTasting = false;

		}
	}
}