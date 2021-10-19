using System;
using System.Collections.Generic;
using System.Linq;
using TestBotIS;

namespace CardListExtension
{
	static class CardListExtensionClass
	{
		public static List<Card> Shuffle(this IReadOnlyCollection<Card> list)
		{
			return list.OrderBy(a => Guid.NewGuid()).ToList();
		}
		public static void DisplayCardList(this IReadOnlyCollection<Card> list)
		{
			foreach (Card card in list)
			{
				card.DisplayCardData();
			}
		}

		/// <summary>
		/// カードの説明付き名前リストを返す
		/// </summary>
		/// <returns>名前や説明などが格納されたstring List</returns>
		public static List<string> GetStrCardList(this IReadOnlyCollection<Card> list)
		{
			var strList = new List<string>(0);
			foreach (Card card in list)
			{
				// strList.Add(card.GetStrCardData());
				strList.Add(card.GetStrCardName());
			}
			return strList;
		}

		/// <summary>
		/// カードの名前リストを返す
		/// </summary>
		/// <returns>名前が格納されたstring List</returns>
		public static List<string> GetStrCardName(this IReadOnlyCollection<Card> list)
		{
			var strList = new List<string>(0);
			foreach (Card card in list)
			{
				strList.Add(card.Name);
			}
			return strList;
		}


		/// <summary>
		/// IDでソートする
		/// </summary>
		/// <returns></returns>
		public static void SortID(this List<Card> list)
		{
			list.Sort((a, b) => a.ID - b.ID);
		}

		/// <summary>
		/// 名前でソートする
		/// </summary>
		/// <returns></returns>
		public static void SortName(this List<Card> list)
		{
			list.Sort((a, b) => a.Name.CompareTo(b.Name));
		}

		/// <summary>
		/// 点数計算する。重複やカードが2枚以下，6枚以上の場合はエラーを返す
		/// </summary>
		/// <param name="list">点数計算したいカードのリスト</param>
		/// <returns>(点数, 処理結果メッセージ)</returns>
		public static (bool, int, string) CalcScore(this List<Card> list)
		{
			bool IsNoodle = false;
			double[] score = new double[list.Count];
			string scoremsg = "内訳：";
			double sum = 0;
			list.SortID();
			string errmsg = "";
			bool hantei = true;

			if (list.Count < 3 || list.Count > 5)
			{
				errmsg = "カードは3枚以上5枚以下である必要があります\n";
				hantei = false;
			}
			List<string> duplicates = list.FindDuplication();
			if (duplicates.Count != 0)
			{
				errmsg += "以下のカードが重複しています：";
				errmsg += string.Join(",", duplicates) + "\n";
				// System.Console.WriteLine(str);
				hantei = false;
			}

			int index = -1;
			foreach (Card card in list)
			{
				index++;
				bool[] Flag = new bool[3] { false, false, false };
				if (card.Kind == "炭水化物")
				{
					IsNoodle = true;
				}
				foreach (Card scard in list)
				{
					for (int i = 0; i < 3; i++)
					{
						if (card.FlagStrList[i] == scard.Name && scard != card) Flag[i] = true;
						if (card.FlagStrList[i] == scard.Kind && scard != card) Flag[i] = true;
					}
				}
				score[index] = card.Number;
				string[] ope = card.FlagKind.Split('/');

				if (ope[0] == "AND")
				{
					for (int i = 0; i < int.Parse(ope[1]); i++)
					{
						if (Flag[i])
						{
							score[index] = Math.Max(score[index], card.FlagNumberList[i]);
							continue;
						}
						break;
					}
				}

				// ORの場合，最も高い数字を取ってくる
				if (ope[0] == "OR")
				{
					for (int i = 0; i < int.Parse(ope[1]); i++)
					{
						if (Flag[i])
						{
							score[index] = Math.Max(score[index], card.FlagNumberList[i]);
						}

					}
				}
				if (ope.Length >= 3)
				{
					if (ope[2] == "T")
					{
						if (Flag[2])
						{
							score[index] = card.FlagNumberList[2];
						}
					}
				}

				sum += score[index];
				scoremsg += card.Name + "：" + score[index].ToString() + "点 / ";
			}
			scoremsg += "\n";
			
			if (IsNoodle == false)
			{
				// return (false, (int)sum, "麺(炭水化物)がありません");
				errmsg += "麺(炭水化物)がありません\n";
				hantei = false;
			}


			sum = Math.Max(sum, 0.0);
			return (hantei, (int)sum, scoremsg + errmsg);
		}


		/// <summary>
		/// 重複要素を発見して返す
		/// </summary>
		/// <param name="list">重複を発見したいリスト</param>
		/// <returns>重複カード名を格納した文字列リスト</returns>
		public static List<string> FindDuplication(this IReadOnlyCollection<Card> list)
		{
			var duplicates = list.GroupBy(name => name.Name).Where(name => name.Count() > 1)
								.Select(group => group.Key).ToList();
			return duplicates;
		}
		/// <summary>
		/// 名前を格納したstringリストから同名カードリストを作る
		/// </summary>
		/// <param name="list">発見したいカードの名前リスト</param>
		/// <returns>発見したカードのリスト，発見カードを除いたカードのリスト</returns>
		public static (List<Card>, List<Card>) FindCardList(this IReadOnlyCollection<Card> list, List<string> CardNameList)
		{
			var listResult = new List<Card>(0);
			var list2 = list.DeepCopy();
			foreach (string str in CardNameList)
			{
				foreach (Card card in list2)
				{
					if (str == card.Name)
					{
						listResult.Add(card);
						list2.Remove(card);
						// System.Console.WriteLine(str);
						break;
					}
				}
			}
			return (listResult, list2);
		}

		/// <summary>
		/// ディープコピー(値渡し)する
		/// </summary>
		/// <param name="list">ディープコピーしたいリスト</param>
		/// <returns>内容が全く同じで違うインスタンス</returns>
		public static List<Card> DeepCopy(this IReadOnlyCollection<Card> list)
		{
			var retlist = new List<Card>();
			retlist.AddRange(list.Select(i => (Card)i.Clone()));
			return retlist;
		}
	}
}
