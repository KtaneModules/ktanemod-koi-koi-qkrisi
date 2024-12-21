using System;

namespace KoiKoi
{
    public enum CardSuit
    {
        January,
        February,
        March,
        April,
        May,
        June,
        July,
        August,
        September,
        October,
        November,
        December
    }

    [Flags]
    public enum CardType
    {
        Plain = 1 << 0,
        Ribbon = 1 << 1,
        Animal = 1 << 2,
        Bright = 1 << 3,
        BRibbon = Ribbon | (1 << 4),
        Poetry = Ribbon | (1 << 5),
        Boar = Animal | (1 << 6),
        Deer = Animal | (1 << 7),
        Butterflies = Animal | (1 << 8),
        Curtain = Bright | (1 << 9),
        Moon = Bright | (1 << 10),
        Rain = Bright | (1 << 11),
        Sake = Plain | Animal | (1 << 12),
    }

    public enum GameState
    {
        SelectCardFromHand,
        SelectCardOnBoardFromHand,
        SelectCardOnBoardFromStack,
        ChooseIfKoiKoi,
        Wait,
        SearchForOpponent,
        GameEnd
    }

    public enum CardState
    {
        InStack,
        OnTable,
        InPlayerHand,
        InOpponentHand,
        TakenByPlayer,
        TakenByOpponent
    }

    public enum ScoreboardState
    {
        Searching,
        Ingame,
        GameEnd
    }
}