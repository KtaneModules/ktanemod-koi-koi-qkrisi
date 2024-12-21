using System.Collections.Generic;

namespace KoiKoi
{
    public class PlayerInfo
    {
        public List<HanafudaCard> Hand = new List<HanafudaCard>();
        public List<HanafudaCard> Table = new List<HanafudaCard>();
        public KoiKoiHand Cards = new KoiKoiHand();

        public string[] Yakus = { };
        public int Score;
        public bool SevenDouble;

        public int FinalScore;

        public void Reset()
        {
            Hand.Clear();
            Table.Clear();
            Cards.Clear();
            Yakus = new string[0];
            Score = 0;
            SevenDouble = false;
        }
    }
}