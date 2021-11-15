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

			if (Program._IsTasting == true
				&& reac.Emote.ToString() == "🍴")
			{
				await Tasting();
			} else if (Program._IsTasting == true
					&& reac.Emote.ToString() == "👍"){
				Program._TastingThroughN++;
				if(Program._TastingThroughN >= Program._PersonList.Count - 1){
					await noTasting();
				}
			}
			return;
		}

		/// <summary>
		/// 味見処理を行う
		/// </summary>
		/// <returns></returns>
		public static async Task Tasting()
		{
			bool IsFault = false;
			//手札0枚かつ勝利点5点未満のやつに味見する権利はない
			if (ReactionPerson.GetHand().Count == 0 && ReactionPerson.GetScore() < 5)
			{
				var embed = new EmbedBuilder();
				embed.WithTitle("貴様に味見する権利はない");
				embed.WithAuthor(ReactionPerson.socketUser.Username, ReactionPerson.socketUser.GetAvatarUrl() ?? ReactionPerson.socketUser.GetDefaultAvatarUrl());
				embed.WithColor(Color.Red);
				embed.WithDescription("手札0枚かつ勝利点5点未満の奴に味見する権利などない");
				await Program._GameChannel.SendMessageAsync(null, false, embed.Build());
				return;
			}
			//野菜を宣言して2枚出した場合
			if (Program._DeclaredName == "野菜")
			{
				if (Program._Field.Count != 2)
					return;
				if (Program._Field[0].Name == Program._Field[1].Name)
				{
					// 出したカードと宣言が一致
					Console.WriteLine("一致1");
					IsFault = true;
				}
				else if ((Program._Field[0].Name == "豆腐" && Program._Field[1].Kind != "タンパク質")
					  || (Program._Field[1].Name == "豆腐" && Program._Field[0].Kind != "タンパク質"))
				{
					// 出したカードと宣言が一致
					Console.WriteLine("一致2");
					IsFault = true;
				}

			}
			//タンパク質の名前を宣言して1枚出した場合
			else
			{
				if (Program._Field.Count != 1)
					return;

				if (Program._Field[0].Name == Program._DeclaredName)
				{
					// 出したカードと宣言が一致
					Console.WriteLine("一致2");
					IsFault = true;
				}
			}
			if (IsFault)
			{
				await CardListHandler.TastingFault(ResponseFromMsg.Player, ReactionPerson);
			}
			else
			{
				//一致しない
				Console.WriteLine("一致なし");
				await CardListHandler.TastingSuccsess(ResponseFromMsg.Player, ReactionPerson);
			}
			
			Program._Trash.AddRange(Program._Field.DeepCopy());
			Program._Field.Clear();
			Program._IsTasting = false;
		}
		/// <summary>
		/// 味見処理を行う
		/// </summary>
		/// <returns></returns>
		public static async Task noTasting(){
			await CardListHandler.TastingThrough(ResponseFromMsg.Player);
			Program._Trash.AddRange(Program._Field.DeepCopy());
			Program._Field.Clear();
			Program._IsTasting = false;
		}
	}
}