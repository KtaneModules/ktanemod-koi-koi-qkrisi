using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KoiKoi;
using UnityEngine;
using UnityEngine.UI;

public partial class KoiKoiPlayroom : MonoBehaviour
{
    public System.Random RNG;

    [NonSerialized] public KoiKoiModule Module;
    public static GameObject TableModel;
    public RenderTexture RT;
    public Transform CardStackParent;
    public Transform TableParent;
    public Transform PlayerHandParent;
    public Transform OpponentHandParent;
    public Camera PlayerCamera;
    public KMSelectable KoiKoiButton;
    public KMSelectable StopButton;
    public Text YakuListText;
    public Text GameEndText;
    public KMSelectable NewGameButton;
    public Text OpponentSearchText;
    public KMSelectable PlayAgainstBotButton;
    
    private Stack<HanafudaCard> CardStack = new Stack<HanafudaCard>();
    [NonSerialized] public List<HanafudaCard> AllCards = new List<HanafudaCard>();

    public List<TableSlot> TableSlots = new List<TableSlot>();
    public PlayerInfo SelfInfo = new PlayerInfo();
    public PlayerInfo OpponentInfo = new PlayerInfo();
    
    private const float TableSlotXDiff = 0.165f;
    public const float CardYDiff = 0.003f;
    
    [NonSerialized] public int PlayerNum;
    private int _CurrentPlayer;

    private int CurrentPlayer
    {
        get
        {
            return _CurrentPlayer;
        }
        set
        {
            _CurrentPlayer = value;
            PlayerCurrentTurnMarkerImage.enabled = SelfTurn;
            OpponentCurrentTurnMarkerImage.enabled = !SelfTurn;
        }
    }
    internal int CurrentTurn;
    private int _KoiKois = 1;
    
    internal int KoiKois
    {
        get
        {
            return _KoiKois;
        }
        set
        {
            _KoiKois = value;
            PlayerMultiplierText.text = $"x{_KoiKois}";
            OpponentMultiplierText.text = $"x{_KoiKois}";
        }
    }

    internal string OpponentNameStr = "Bot opponent";
    internal SteamAvatar OpponentAvatar;

    public Texture2D BotAvatar;
    public Texture2D SteamDefaultAvatar;
    public Texture2D TwitchAvatar;

    public int OpponentNum => PlayerNum == 1 ? 0 : 1;
    public bool SelfTurn => CurrentPlayer == PlayerNum;
    private PlayerInfo CurrentPlayerInfo => SelfTurn ? SelfInfo : OpponentInfo;

    public BotOpponent Bot;

    private GameState _state = GameState.Wait;

    public GameState State
    {
        get
        {
            return _state;
        }
        set
        {
            _state = value;
            if(_state == GameState.ChooseIfKoiKoi)
                DrawEndChoice(); 
            ProcessStateWithBot();
        }
    }
    [NonSerialized] public HanafudaCard SelectedCard;
    private bool[] CardTakes = { true, true, true, true };
    public TakeSlotPointer[] PlayerSlotPointers;
    public TakeSlotPointer[] OpponentSlotPointers;
    public Texture2D[] CardTypeTextures;
    private Sprite[] CardTypeSprites;
    internal int CurrentPlayerSlotPointer = 3;
    internal int CurrentOpponentSlotPointer = 3;
    internal bool PlayerSlotMoveAllowed = true;
    internal bool OpponentSlotMoveAllowed = true;

    public PlayerNameDisplay PlayerName;
    public PlayerNameDisplay OpponentName;
    
    private TurnEndButtons PlayerTurnEndButtons;
    private TurnEndButtons OpponentTurnEndButtons;

    public Text TwitchPlaysIDText;
    public Text PlayerScoreText;
    public Text PlayerMultiplierText;
    public Text PlayerCurrentScoreText;
    public Text OpponentScoreText;
    public Text OpponentMultiplierText;
    public Text OpponentCurrentScoreText;
    
    public Image PlayerCurrentCardTypeImage;
    public Image OpponentCurrentCardTypeImage;
    public KMSelectable PlayerNextCardTypeBtn;
    public KMSelectable PlayerPreviousCardTypeBtn;
    public KMSelectable OpponentNextCardTypeBtn;
    public KMSelectable OpponentPreviousCardTypeBtn;
    
    public Texture2D[] CurrentTurnMarkerTextures;
    private Sprite[] CurrentTurnMarkerSprites;
    public Image PlayerCurrentTurnMarkerImage;
    public Image OpponentCurrentTurnMarkerImage;

    public KMSelectable LeaveGameBtn;

    public ScoreboardTab[] ScoreboardTabs;

    private bool Row2;

    public Queue<int> Seeds = new Queue<int>();
    public Queue<int> NetworkInput = new Queue<int>();
    
    private Coroutine SearchingCoroutine;
    
    void Awake()
    {
        //Seeds.Enqueue(3000);
        //Seeds.Enqueue(4000);
        Bot = new BotOpponent(this);
        CardTypeSprites = CardTypeTextures
            .Select(t => Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero)).ToArray();
        PlayerCurrentCardTypeImage.sprite = CardTypeSprites[CurrentPlayerSlotPointer];
        OpponentCurrentCardTypeImage.sprite = CardTypeSprites[CurrentOpponentSlotPointer];
        CurrentTurnMarkerSprites = CurrentTurnMarkerTextures.Select(t => Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero)).ToArray();
        PlayerCurrentTurnMarkerImage.sprite = CurrentTurnMarkerSprites[0];
        OpponentCurrentTurnMarkerImage.sprite = CurrentTurnMarkerSprites[0];
        for (int i = 0; i < 10; i++)
        {
            TableSlots.Add(new TableSlot(0.08f + ((i % 2 == 0 ? i : i - 1)/2+1) * TableSlotXDiff, 0f,
                i % 2 == 0 ? 0.11f : -0.11f));
        }
        
        TableSlots.Add(new TableSlot(0.3f, CardYDiff, 0.11f));
        TableSlots.Add(new TableSlot(0.3f, CardYDiff, -0.11f));

        KoiKoiButton.OnInteract += () =>
        {
            if (State == GameState.ChooseIfKoiKoi)
            {
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, KoiKoiButton.transform);
                Module.Send(2);
                ProcessInput(2, PlayerNum);
            }

            return false;
        };

        StopButton.OnInteract += () =>
        {
            if (State == GameState.ChooseIfKoiKoi)
            {
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, StopButton.transform);
                Module.Send(1);
                ProcessInput(1, PlayerNum);
            }

            return false;
        };
        
        NewGameButton.OnInteract += () =>
        {
            if (State != GameState.GameEnd)
                return false;
            OpponentNameStr = "";
            OpponentAvatar = null;
            OpponentName.UpdatePlayer(OpponentNameStr, OpponentAvatar);
            KoiKoiService.Instance.RegisterForSearch(Module);
            SearchingCoroutine = StartCoroutine(SearchingRoutine());
            State = GameState.SearchForOpponent;
            SetScoreboardTab(ScoreboardState.Searching);
            //SetScoreboardTabAndOpen(OpponentSearchTab);
            if(!Module.TPActive && KoiKoiService.Instance.SteamApiInitialized)
                KoiKoiService.Instance.InitiateSearch();
            else NewGameWithBot();
            return false;
        };
        
        PlayAgainstBotButton.OnInteract += () =>
        {
            if (State == GameState.SearchForOpponent)
                NewGameWithBot();
            return false;
        };

        LeaveGameBtn.OnInteract += () =>
        {
            Module.LeaveLobby();
            Bot.Active = true;
            UpdateOpponentName();
            ProcessStateWithBot();
            return false;
        };

        PlayerNextCardTypeBtn.OnInteract += () =>
        {
            NextPointer(true, -1);
            return false;
        };

        PlayerPreviousCardTypeBtn.OnInteract += () =>
        {
            NextPointer(true, 1);
            return false;
        };

        OpponentNextCardTypeBtn.OnInteract += () =>
        {
            NextPointer(false, -1);
            return false;
        };

        OpponentPreviousCardTypeBtn.OnInteract += () =>
        {
            NextPointer(false, 1);
            return false;
        };
        //Initialize(500, 0);   //1 - Duplicate
    }

    public RenderTexture InitTexture()
    {
        Patches.Texture = RT;
        var texture = new RenderTexture(RT);
        PlayerCamera.targetTexture = texture;
        return texture;
    }

    private IEnumerator SearchingRoutine()
    {
        var dots = 0;
        while (true)
        {
            OpponentSearchText.text = $"Searching for opponent{new string('.', ++dots)}";
            if (dots == 3)
                dots = 0;
            yield return new WaitForSeconds(.5f);
        }
    }

    public void NewGameWithBot()
    {
        Module.LeaveLobby();
        KoiKoiService.Instance.SearchingModules.Remove(Module);
        Seeds.Clear();
        Seeds.Enqueue(KoiKoiService.GetRandomSeed());
        Seeds.Enqueue(KoiKoiService.GetRandomSeed());
        Seeds.Enqueue(KoiKoiService.GetRandomSeed());
        Bot.Active = true;
        PlayerNum = 0;
        UpdateOpponentName();
        NewGame();
    }
    
    public void NewGame()
    {
        Module.Log("Starting new game");
        StopCoroutine(SearchingCoroutine);
        SelfInfo.Reset();
        OpponentInfo.Reset();
        CurrentTurn = 0;
        PlayerCurrentScoreText.text = "0";
        OpponentCurrentScoreText.text = "0";
        PlayerScoreText.text = "0";
        OpponentScoreText.text = "0";
        KoiKois = 1;
        PlayerCurrentTurnMarkerImage.sprite = CurrentTurnMarkerSprites[0];
        OpponentCurrentTurnMarkerImage.sprite = CurrentTurnMarkerSprites[0];
        SearchingCoroutine = null;
        State = GameState.Wait;
        SetScoreboardTab(ScoreboardState.Ingame);
        SelfInfo = new PlayerInfo();
        OpponentInfo = new PlayerInfo();
        CurrentPlayer = 0;
        Module.Log("Starting player: " + (SelfTurn ? "player" : "opponent"));
        Initialize(Seeds.Dequeue());
    }

    public void ProcessStateWithBot()
    {
        if (_state < GameState.Wait && Bot.Active && !SelfTurn)
            StartCoroutine(Bot.ProcessState(_state));
    }
    
    void ResetRound()
    {
        State = GameState.Wait;
        SelfInfo.Reset();
        OpponentInfo.Reset();
        CardStack.Clear();
        KoiKois = 1;
        PlayerCurrentScoreText.text = "0";
        OpponentCurrentScoreText.text = "0";
        SelectedCard = null;
        CardTakes = new[] { true, true, true, true };
        Row2 = false;
        foreach (var tableSlot in TableSlots)
            tableSlot.CurrentCard = null;
        foreach(var slot in PlayerSlotPointers.Concat(OpponentSlotPointers))
            slot.ResetPosition(true);
        while (AllCards.Count > 0)
        {
            Destroy(AllCards[0].gameObject);
            AllCards.RemoveAt(0);
        }

        CurrentPlayerSlotPointer = 3;
        CurrentOpponentSlotPointer = 3;
        PlayerCurrentCardTypeImage.sprite = CardTypeSprites[CurrentPlayerSlotPointer];
        OpponentCurrentCardTypeImage.sprite = CardTypeSprites[CurrentOpponentSlotPointer];
        PlayerSlotMoveAllowed = true;
        OpponentSlotMoveAllowed = true;
        
        Module.SetChildren(new KMSelectable[0]);
        if (++CurrentTurn < 3)
        {
            PlayerCurrentTurnMarkerImage.sprite = CurrentTurnMarkerSprites[CurrentTurn];
            OpponentCurrentTurnMarkerImage.sprite = CurrentTurnMarkerSprites[CurrentTurn];
            Initialize(Seeds.Dequeue());
        }
        else EndGame(SelfInfo.FinalScore, OpponentInfo.FinalScore);
    }

    void EndGame(int selfScore, int opponentScore)
    {
        Module.LeaveLobby();
        //GameEndText.text = $"Your score: {selfScore}\nOpponent's score: {opponentScore}\n";
        if (selfScore > opponentScore)
        {
            Module.Log("Victory!");
            GameEndText.text = "You win!";
            NewGameButton.gameObject.SetActive(false);
            var moduleSelectable = Module.GetComponent<KMSelectable>();
            moduleSelectable.OnFocus = null;
            moduleSelectable.OnDefocus();
            moduleSelectable.OnDefocus();       //Recreate the double call from the game
            moduleSelectable.OnDefocus = null;
            Module.GetComponent<KMBombModule>().HandlePass();
        }
        else if (selfScore < opponentScore)
        {
            Module.Log("Defeat!");
            GameEndText.text = "You lose!";
            NewGameButton.gameObject.SetActive(true);
            //Module.GetComponent<KMBombModule>().HandleStrike();
        }
        else
        {
            Module.Log("Tie!");
            GameEndText.text = "Tie!";
            NewGameButton.gameObject.SetActive(true);
        }
        State = GameState.GameEnd;
        SetScoreboardTab(ScoreboardState.GameEnd);
    }

    void DrawEndChoice()
    {
        var score = CurrentPlayerInfo.Score * KoiKois;
        var yakus = CurrentPlayerInfo.Yakus.ToArray();
        (SelfTurn ? PlayerCurrentScoreText : OpponentCurrentScoreText).text = score.ToString();
        YakuListText.text = string.Join("\n", yakus);
        Module.Log($"Yaku! {score} -> {string.Join("; ", yakus)}");
    }
    
    public void Initialize(int seed)
    {
        RNG = new System.Random(seed);
        foreach (var cardType in Enum.GetValues(typeof(CardType)))
        {
            SelfInfo.Cards.Add((CardType)cardType, new List<HanafudaCard>());
            OpponentInfo.Cards.Add((CardType)cardType, new List<HanafudaCard>());
        }
        StartCoroutine(CreateStackAndDeal());
    }

    public void SetSelectables()
    {
        CreateTable();
		TwitchPlaysIDText.text = string.IsNullOrEmpty(Module.TwitchID) ? "" : Module.TwitchID;
        if (Module.TPActive)
            UpdateTwitchClaim(Module.TwitchClaim);
        else
            PlayerName.UpdatePlayer(KoiKoiService.Instance.SteamName,
                KoiKoiService.Instance.PlayerAvatar?.AvatarTexture ?? SteamDefaultAvatar);
        SetPermanentChildren(KoiKoiButton, StopButton, NewGameButton, PlayAgainstBotButton,
            LeaveGameBtn, PlayerNextCardTypeBtn, PlayerPreviousCardTypeBtn, OpponentNextCardTypeBtn,
            OpponentPreviousCardTypeBtn);
        SearchingCoroutine = StartCoroutine(SearchingRoutine());
        State = GameState.SearchForOpponent;
        KoiKoiButton.gameObject.SetActive(false);
        StopButton.gameObject.SetActive(false);
        PlayerTurnEndButtons.KoiKoiButton = KoiKoiButton.gameObject;
        PlayerTurnEndButtons.StopButton = StopButton.gameObject;
        SetScoreboardTab(ScoreboardState.Searching);
    }

    private void CreateTable()
    {
        var table = Instantiate(TableModel, transform, false).transform;
        PlayerTurnEndButtons = table.Find("PlayerTurnEndButtonContainer").Find("PlayerTurnEndButtons").gameObject.AddComponent<TurnEndButtons>();
        OpponentTurnEndButtons = table.Find("OpponentTurnEndButtonContainer").Find("OpponentTurnEndButtons").gameObject.AddComponent<TurnEndButtons>();
    }
    
    void SetPermanentChildren(params KMSelectable[] selectables)
    {
        var moduleSelectable = Module.GetComponent<KMSelectable>();
        foreach (var selectable in selectables)
        {
            selectable.Parent = moduleSelectable;
            selectable.UpdateChildrenProperly();
        }
        Module.PermanentChildren = Module.PermanentChildren.Concat(selectables).ToArray();
        Module.SetChildren(new KMSelectable[0]);
    }

    private bool IsValid(List<CardInfo> cardInfos)
    {
        for (int i = 0; i < 3; i++)
        {
            var suits = new Dictionary<CardSuit, int>();
            for (int j = 0; j < 8; j++)
            {
                int x = i * 8 + j;
                var card = cardInfos[x];
                if(!suits.ContainsKey(card.Suit))
                    suits.Add(card.Suit, 1);
                else suits[card.Suit]++;
            }
            if (suits.Values.Any(x => x == 4) || suits.Values.All(x => x == 2))
                return false;
        }
        return true;
    }

    IEnumerator CreateStackAndDeal()
    {
        int i = -1;
        
        var cardInfos = new List<CardInfo>();
        var moduleSelectable = Module.GetComponent<KMSelectable>();
        do
        {
            cardInfos.Clear();
            var _cardInfos = KoiKoiService.Instance.CardInfos;
            while (_cardInfos.Count > 0)
            {
                var j = RNG.Next(0, _cardInfos.Count);
                var info = _cardInfos[j];
                _cardInfos.RemoveAt(j);
                cardInfos.Add(info);
            }
            yield return null;
        } while(!IsValid(cardInfos));
        while (cardInfos.Count > 0)
        {
            i++;
            var info = cardInfos[cardInfos.Count - 1];
            cardInfos.RemoveAt(cardInfos.Count - 1);
            var card = Instantiate(KoiKoiService.Instance.CardPrefab, CardStackParent, false);
            var targetPosition = new Vector3(-0.005f, i * CardYDiff, 0f);
            card.transform.localPosition = targetPosition + new Vector3(-0.005f, 0f, 0.1f);
            card.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 180f));
            var cardComponent = card.GetComponent<HanafudaCard>();
            cardComponent.Playroom = this;
            cardComponent.SetInfo(info);
            cardComponent.State = CardState.InStack;
            CardStack.Push(cardComponent);
            AllCards.Add(cardComponent);
            var cardSelectable = card.GetComponent<KMSelectable>();
            cardSelectable.Parent = moduleSelectable;
            cardSelectable.UpdateChildrenProperly();
            while (Vector3.Distance(card.transform.localPosition, targetPosition) > 0.001f)
            {
                card.transform.localPosition =
                    Vector3.MoveTowards(card.transform.localPosition, targetPosition, 1f * Time.deltaTime);
                yield return null;
            }
            card.transform.localPosition = targetPosition;
        }

        Module.SetChildren(AllCards.Select(c => c.GetComponent<KMSelectable>()));
        
        yield return DealTableCards();
        if (PlayerNum == 0)
            yield return DealPlayerHand();
        yield return DealOpponentHand();
        if (PlayerNum == 1)
            yield return DealPlayerHand();
        State = GameState.SelectCardFromHand;
        StartNetworkWait();
    }

    IEnumerator DealTableCards()
    {
        for (int i = 0; i < 8; i++)
        {
            var card = CardStack.Pop();
            card.State = CardState.OnTable;
            card.transform.SetParent(TableParent);
            card.Slot = TableSlots[i];
            TableSlots[i].CurrentCard = card;
            yield return card.MoveTo(TableSlots[i].Position, true);
        }
    }

    IEnumerator DealPlayerHand()
    {
        for (int i = 0; i < 8; i++)
        {
            var card = CardStack.Pop();
            card.State = CardState.InPlayerHand;
            card.transform.SetParent(PlayerHandParent);
            SelfInfo.Hand.Add(card);
            yield return card.MoveTo(new Vector3(i*0.1655f, 0f, -0.34f), true);
        }
    }
    
    IEnumerator DealOpponentHand()
    {
        for (int i = 0; i < 8; i++)
        {
            var card = CardStack.Pop();
            card.State = CardState.InOpponentHand;
            card.transform.SetParent(OpponentHandParent);
            OpponentInfo.Hand.Add(card);
            yield return card.MoveTo(new Vector3(1.1585f - i * 0.1655f, 0f, 0.34f), false, true);
        }
    }

    public void ProcessInput(int input, int playerNum)
    {
        if (playerNum != CurrentPlayer)
            return;
        switch (State)
        {
            case GameState.SelectCardFromHand:
                if (input == 0 || input > CurrentPlayerInfo.Hand.Count)
                    return;
                SelectedCard = CurrentPlayerInfo.Hand[input-1];
                HandleSelected(false);
                break;
            case GameState.SelectCardOnBoardFromHand:
                if (input == -1)
                {
                    State = GameState.SelectCardFromHand;
                    StartNetworkWait();
                    return;
                }
                goto case GameState.SelectCardOnBoardFromStack;
            case GameState.SelectCardOnBoardFromStack:
                if (input == 0)
                {
                    Row2 ^= true;
                    StartNetworkWait();
                    return;
                }

                var slot = TableSlots[Row2 ? (input - 1) * 2 + 1 : (input - 1) * 2];
                if (slot.CurrentCard?.Suit != SelectedCard.Suit)
                {
                    Row2 = false;
                    return;
                }
                var _state = State;
                State = GameState.Wait;
                StartCoroutine(Take(_state == GameState.SelectCardOnBoardFromStack, slot));
                break;
            case GameState.ChooseIfKoiKoi:
                if(SelfTurn)
                    PlayerTurnEndButtons.CloseButtons();
                else OpponentTurnEndButtons.CloseButtons();
                //PlayerCurrentScoreText.text = "";
                //OpponentCurrentScoreText.text = "";
                YakuListText.text = "";
                if (input == 1) //Stop
                {
                    Module.Log("Stop!");
                    Stop(false);
                    return;
                }

                if (input == 2) //Koi-Koi
                {
                    Module.Log("Koi-Koi!");
                    KoiKois++;
                    NextPlayer();
                    return;
                }
                break;
        }
    }

    private IEnumerator WaitForNetworkInput()
    {
        yield return new WaitUntil(() => NetworkInput.Count > 0);
        ProcessInput(NetworkInput.Dequeue(), OpponentNum);
    }

    private void StartNetworkWait()
    {
        if(CurrentPlayer == OpponentNum)
            StartCoroutine(WaitForNetworkInput());
    }

    void Stop(bool draw)
    {
        if (!draw)
        {
            CurrentPlayerInfo.FinalScore += CurrentPlayerInfo.Score * KoiKois;
            (SelfTurn ? PlayerScoreText : OpponentScoreText).text = CurrentPlayerInfo.FinalScore.ToString();
        }

        ResetRound();
    }

    public void HandleSelected(bool fromStack)
    {
        var selectableCards = TableSlots.Where(slot => slot.CurrentCard?.Suit == SelectedCard.Suit).ToArray();
        switch (selectableCards.Length)
        {
            case 0:
                State = GameState.Wait;
                StartCoroutine(MoveToEmpty(fromStack));
                break;
            case 1:
            case 3:
                State = GameState.Wait;
                StartCoroutine(Take(fromStack, selectableCards));
                break;
            case 2:
                Row2 = false;
                State = fromStack ? GameState.SelectCardOnBoardFromStack : GameState.SelectCardOnBoardFromHand;
                StartNetworkWait();
                break;
        }
    }

    IEnumerator MoveToEmpty(bool fromStack)
    {
        var slot = TableSlots.First(_slot => _slot.IsFree);
        if(!fromStack)
            CurrentPlayerInfo.Hand.Remove(SelectedCard);
        SelectedCard.State = CardState.OnTable;
        SelectedCard.Slot = slot;
        slot.CurrentCard = SelectedCard;
        SelectedCard.transform.SetParent(TableParent);
        yield return SelectedCard.MoveTo(slot.Position, !fromStack && !SelfTurn, !SelfTurn);
        if (!fromStack)
            yield return Draw();
        else HandleEndTurn();
    }

    public IEnumerator Take(bool fromStack, params TableSlot[] slots)
    {
        if (!fromStack)
            CurrentPlayerInfo.Hand.Remove(SelectedCard);
        yield return SelectedCard.MoveTo(
            slots[0].Position + new Vector3(0f, 2*CardYDiff, SelfTurn ? -0.05f : 0.05f),
            !fromStack && !SelfTurn);
        yield return new WaitForSecondsRealtime(.1f);
        CardTakes[0] = false;
        StartCoroutine(TakeCard(SelectedCard, 0));
        for (int i = 0; i < slots.Length; i++)
        {
            var card = slots[i].CurrentCard;
            card.Slot = null;
            slots[i].CurrentCard = null;
            CardTakes[i + 1] = false;
            StartCoroutine(TakeCard(card, i + 1));
            yield return new WaitForSecondsRealtime(.1f);
        }
        yield return new WaitUntil(() => CardTakes[0] && CardTakes[1] && CardTakes[2] && CardTakes[3]);
        if (!fromStack)
            yield return Draw();
        else HandleEndTurn();
    }

    IEnumerator Draw()
    {
        var card = CardStack.Pop();
        yield return card.MoveTo(new Vector3(0.14f, 0f, 0f), true, !SelfTurn);
        SelectedCard = card;
        HandleSelected(true);
    }
    
    void HandleEndTurn()
    {
        var otherPlayer = PlayerNum == CurrentPlayer ? OpponentInfo : SelfInfo;
        var forceStop = otherPlayer.Hand.Count == 0;
        if (RefreshScore())
        {
            forceStop |= CurrentPlayerInfo.Hand.Count == 0;
            if (!forceStop)
            {
                State = GameState.ChooseIfKoiKoi;
                if (SelfTurn)
                    PlayerTurnEndButtons.OpenButtons();
                else OpponentTurnEndButtons.OpenButtons();
                StartNetworkWait();
                return;
            }
            Stop(false);
            return;
        }

        if (forceStop)
        {
            Module.Log("Out of cards, ending round");
            Stop(true);
        }
        else NextPlayer();
    }

    void NextPlayer()
    {
        CurrentPlayer = (CurrentPlayer + 1) % 2;
        Module.Log("Next player: " + (SelfTurn ? "player" : "opponent"));
        State = GameState.SelectCardFromHand;
        StartNetworkWait();
    }

    IEnumerator TakeCard(HanafudaCard card, int index)
    {
        Module.Log($"Taking card {card.Suit} {card.Type}");
        var oldState = card.State;
        card.State = SelfTurn ? CardState.TakenByPlayer : CardState.TakenByOpponent;
        CurrentPlayerInfo.Table.Add(card);
        foreach(var cardType in Enum.GetValues(typeof(CardType)))
        {
            var _type = (CardType)cardType;
            if (card.HasType(_type))
                CurrentPlayerInfo.Cards[_type].Add(card);
        }
        var takePointers = SelfTurn ? PlayerSlotPointers : OpponentSlotPointers;
        var currentPointerIndex = SelfTurn ? CurrentPlayerSlotPointer : CurrentOpponentSlotPointer;
        var match = takePointers.First(p => card.HasType(p.Type));
        match.Cards.Add(card);
        card.transform.SetParent(match.transform);
        yield return card.MoveTo(
            match == takePointers[currentPointerIndex] ? match.GetPositionAndAdvance() : match.ResetPos,
            false, oldState == CardState.OnTable && !SelfTurn);
        CardTakes[index] = true;
    }

    private bool RefreshScore()
    {
        var player = CurrentPlayerInfo;
        int score = 0;
        var _yakus = new List<string>();
        foreach (var yaku in Yakus)
        {
            if (yaku.Check(player.Cards))
            {
                var points = yaku.GetPoints(player.Cards);
                score += points;
                _yakus.Add($"{yaku.Name} ({points})");
            }
        }

        /*if (score >= 7)
        {
            score *= 2;
            player.SevenDouble = true;
        }*/

        if (score > player.Score)
        {
            player.Yakus = _yakus.ToArray();
            player.Score = score;
            return true;
        }

        return false;
    }

    internal void NextPointer(bool player, int delta)
    {
        var allowed = State < GameState.Wait && (player ? PlayerSlotMoveAllowed : OpponentSlotMoveAllowed);
        if(!allowed) 
            return;
        if (player)
            PlayerSlotMoveAllowed = false;
        else OpponentSlotMoveAllowed = false;
        var slotPointers = player ? PlayerSlotPointers : OpponentSlotPointers;
        var index = player ? CurrentPlayerSlotPointer : CurrentOpponentSlotPointer;
        slotPointers[index].ResetPosition(false);
        var index2 = index + delta;
        if (index2 >= slotPointers.Length)
            index2 = 0;
        if(index2 < 0)
            index2 = slotPointers.Length - 1;
        if (player)
        {
            CurrentPlayerSlotPointer = index2;
            PlayerCurrentCardTypeImage.sprite = CardTypeSprites[index2];
        }
        else
        {
            CurrentOpponentSlotPointer = index2;
            OpponentCurrentCardTypeImage.sprite = CardTypeSprites[index2];
        }
        StartCoroutine(Utils.CallbackCoroutine(slotPointers[index].ResetAll(),
            () => StartCoroutine(Utils.CallbackCoroutine(slotPointers[index2].SpreadOut(), () =>
            {
                if (player)
                    PlayerSlotMoveAllowed = true;
                else OpponentSlotMoveAllowed = true;
            }))));
    }

    private void SetScoreboardTab(ScoreboardState sbState)
    {
        foreach(var tab in ScoreboardTabs)
            tab.SetObjectsActive(tab.State == sbState);
    }

    internal void UpdateOpponentName()
    {
        if (Bot.Active)
            OpponentName.UpdatePlayer("Bot opponent", BotAvatar);
        else OpponentName.UpdatePlayer(OpponentNameStr, OpponentAvatar?.AvatarTexture ?? SteamDefaultAvatar);
    }

    internal void UpdateTwitchClaim(string username)
    {
        PlayerName.UpdatePlayer(username, TwitchAvatar);
    }
}
