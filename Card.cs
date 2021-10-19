using System;
using System.Collections.Generic;

namespace TestBotIS
{
	class Card
	{
		public readonly int ID;
		public readonly string Name;
		public readonly string Kind;
		public readonly double Number;
		public readonly int DrawNumber;
		public readonly string FlagKind;
		public readonly List<string> FlagStrList;
		public readonly List<double> FlagNumberList;
		public readonly string Note;
		public Card(int ID, string Name, string Kind, double Number, int DrawNumber, string FlagKind
					, List<double> FlagNumberList, List<string> FlagStrList, string Note)
		{
			this.ID = ID;
			this.Name = Name;
			this.Kind = Kind;
			this.Number = Number;
			this.DrawNumber = DrawNumber;
			this.FlagKind = FlagKind;
			this.FlagNumberList = FlagNumberList;
			this.FlagStrList = FlagStrList;
			this.Note = Note;
		}
		public void DisplayCardData()
		{
			Console.WriteLine($"ID:{ID,3}, Name:{Name,4}, Kind:{Kind,5}, Number:{Number,2}, Note:{Note}");
		}
		public string GetStrCardData()
		{
			return String.Format($"ID:{ID,3}, Name:{Name,4}, Kind:{Kind,5}, Number:{Number,2}, Note:{Note}");
		}
		public string GetStrCardName()
		{
			return String.Format($"{Name}  /  {Note}");
		}
		public int GetID()
		{
			return ID;
		}
		public string GetName()
		{
			return Name;
		}
		public Card Clone()
		{
			return new Card(this.ID, this.Name, this.Kind, this.Number, this.DrawNumber, this.FlagKind
							, this.FlagNumberList, this.FlagStrList, this.Note);
		}
	}
}
