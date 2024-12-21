using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace KoiKoi
{
    public class BotOpponent
    {
        private readonly KoiKoiPlayroom Playroom;

        private bool _Active;

        public bool Active
        {
            get
            {
                return _Active;
            }
            set
            {
                _Active = value;
                Playroom.LeaveGameBtn.gameObject.SetActive(!_Active);
                Playroom.Module.Log($"Bot activated: {_Active}");
            }
        }
        
        public IEnumerator ProcessState(GameState state)
        {
            if (!Active)
                yield break;
            yield return new WaitForSecondsRealtime(1.5f);
            switch (state)
            {
                case GameState.SelectCardFromHand:
                    var sorted = Playroom.OpponentInfo.Hand.Concat(Playroom.TableSlots.Where(slot => !slot.IsFree)
                            .Select(slot => slot.CurrentCard))
                        .OrderByDescending(card => card.SortType)
                        .ThenBy(card => card.State).ToArray();
                    foreach (var card in sorted)
                    {
                        if (card.State == CardState.OnTable)
                        {
                            var candidates = Playroom.OpponentInfo.Hand.Where(c => c.Suit == card.Suit)
                                .OrderByDescending(c => c.SortType).ToArray();
                            if (candidates.Length == 0)
                                continue;
                            Playroom.ProcessInput(Playroom.OpponentInfo.Hand.IndexOf(candidates[0]) + 1, Playroom.OpponentNum);
                            yield break;
                        }

                        if (Playroom.TableSlots.Any(slot => slot.CurrentCard?.Suit == card.Suit))
                        {
                            Playroom.ProcessInput(Playroom.OpponentInfo.Hand.IndexOf(card) + 1, Playroom.OpponentNum);
                            yield break;
                        }
                    }
                    var handSorteds = sorted.Where(c => c.State != CardState.OnTable).ToArray();
                    Playroom.ProcessInput(Playroom.OpponentInfo.Hand.IndexOf(handSorteds[handSorteds.Length - 1]) + 1, Playroom.OpponentNum);
                    yield break;
                case GameState.SelectCardOnBoardFromHand:
                case GameState.SelectCardOnBoardFromStack:
                    var selectionCandidates = Playroom.TableSlots.Where(slot => slot.CurrentCard?.Suit == Playroom.SelectedCard.Suit)
                        .OrderByDescending(slot => slot.CurrentCard.SortType).ToArray();
                    var index = Playroom.TableSlots.IndexOf(selectionCandidates[0]) + 1;
                    if(index % 2 == 0)
                        Playroom.ProcessInput(0, Playroom.OpponentNum);
                    Playroom.ProcessInput((index-1)/2+1, Playroom.OpponentNum);
                    yield break;
                case GameState.ChooseIfKoiKoi:
                    Playroom.ProcessInput(ShouldKoiKoi() ? 2 : 1, Playroom.OpponentNum);
                    yield break;
            }
        }

        private bool HasType(CardType type) => Playroom.OpponentInfo.Hand.Any(c => c.HasType(type)) ||
                                               Playroom.OpponentInfo.Table.Any(c => c.HasType(type));
        
        private bool ShouldKoiKoi()
        {
            if (Playroom.CurrentTurn == 2)
                return Playroom.OpponentInfo.FinalScore + Playroom.OpponentInfo.Score * Playroom.KoiKois <= Playroom.SelfInfo.FinalScore;
            
            var playerTableCount = Playroom.SelfInfo.Cards.ToDictionary(pair => pair.Key, pair => pair.Value.Count);
            var opponentTableCount =
                Playroom.OpponentInfo.Cards.ToDictionary(pair => pair.Key, pair => pair.Value.Count);
            foreach (var type in Enum.GetValues(typeof(CardType)))
            {
                var cType = (CardType)type;
                if (!playerTableCount.ContainsKey(cType))
                    playerTableCount.Add(cType, 0);
                if (!opponentTableCount.ContainsKey(cType))
                    opponentTableCount.Add(cType, 0);
            }

            if (Playroom.SelfInfo.Score >= 5 || Playroom.OpponentInfo.Score * Playroom.KoiKois >= 10)
                return false;
            
            if (playerTableCount[CardType.Plain] >= 8 || playerTableCount[CardType.Animal] >= 4 ||
                playerTableCount[CardType.Ribbon] >= 4)
                return false;

            if (playerTableCount[CardType.Sake] == 1 && (!HasType(CardType.Moon) || !HasType(CardType.Curtain)))
                return false;

            if (playerTableCount[CardType.Moon] == 1 && !HasType(CardType.Sake))
                return false;

            if (playerTableCount[CardType.Curtain] == 1 && !HasType(CardType.Sake))
                return false;

            if (opponentTableCount[CardType.Plain] >= 8 && Playroom.OpponentInfo.Hand.Count >= 2)
                return true;
            
            if ((opponentTableCount[CardType.Plain] >= 8 || opponentTableCount[CardType.Animal] >= 4 ||
                opponentTableCount[CardType.Ribbon] >= 4) && Playroom.OpponentInfo.Hand.Count >= 2)
                return true;

            if (Playroom.OpponentInfo.Hand.Count >= 4)
                return true;
            
            return false;
        }

        public BotOpponent(KoiKoiPlayroom playroom)
        {
            Playroom = playroom;
        }
    }
}