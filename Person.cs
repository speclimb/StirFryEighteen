using System;
using System.Collections.Generic;
using CardListExtension;
using Discord.WebSocket;

namespace TestBotIS
{
	class Person
	{
		public static int NumberOfPerson = 0;
		public readonly int Number;
		public readonly ulong ID;
		public readonly string Name;
		private int job;
		private List<Card> Hand;
		public readonly SocketUser socketUser;
		private int score;
		public bool IsCooked;
		public int TastingNumber;
		public int NumberOfTasteSuccess;
		public bool IsOneCardDicarded;
		public bool IsTwoCardDicarded;
		public bool IsDiscardFailed;
		
		public Person(SocketUser user, int job)
		{
			this.socketUser = user;
			this.Hand = new List<Card>(0);
			this.ID = user.Id;
			this.Name = user.Username;
			this.job = job;
			NumberOfPerson++;
			this.Number = NumberOfPerson;
			this.score = 0;
			this.IsCooked = false;
			this.IsOneCardDicarded = false;
			this.IsTwoCardDicarded= false;
			this.IsDiscardFailed = false;
		}
		public int GetJob()
		{
			return this.job;
		}

		public int GetScore(){
			return this.score;
		}
		public void AddScore(int a){
			this.score += a;
		}

		public List<Card> GetHand()
		{
			return Hand;
		}
		public void SetHand(List<Card> cardList)
		{
			this.Hand = cardList;
		}
		public void SortHand(){
			this.Hand.SortID();
		}
	}
}