using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using KoiKoi;
using Steamworks;
using UnityEngine;

public class KoiKoiService : MonoBehaviour
{
    public static KoiKoiService Instance;
    internal static Harmony HarmonyInstance;
    
    public GameObject CardPrefab;
    [SerializeField] private Texture2D[] CardTextures;
    public Material PlayerAvatarMaterial;
    public TextAsset TableModelBundle;

    private List<CardInfo> _CardInfos = new List<CardInfo>();
    public List<CardInfo> CardInfos => _CardInfos.ToList();
    
    internal bool SteamApiInitialized;
    private ulong SteamUserID;
    internal string SteamName = "Player";
    internal SteamAvatar PlayerAvatar;
    
    
    internal List<KoiKoiModule> SearchingModules = new List<KoiKoiModule>();
    private bool Searching;
    private int SearchingNum;

    public Dictionary<CSteamID, KoiKoiModule> NetworkModules = new Dictionary<CSteamID, KoiKoiModule>();

    private const string KoiKoiID = "Module_KoiKoi";
    private const int MaxLobbies = 1;
    private CallResult<LobbyMatchList_t> callResultLobbyList;
    private CallResult<LobbyCreated_t>[] callResultsLobbyCreated = new CallResult<LobbyCreated_t>[MaxLobbies];
    private CallResult<LobbyEnter_t>[] callResultsLobbyEnter = new CallResult<LobbyEnter_t>[MaxLobbies];
    private Callback<LobbyChatMsg_t> callbackLobbyChatMsg;
    private Callback<LobbyChatUpdate_t> callbackLobbyChatUpdate;

    public static int GetRandomSeed() => UnityEngine.Random.Range(1, int.MaxValue);
    
    void Awake()
    {
        Instance = this;
        SteamApiInitialized = SteamAPI.Init();
        PlayerNameDisplay.AvatarMaterial = PlayerAvatarMaterial;
        if (SteamApiInitialized)
        {
            SteamUserID = SteamUser.GetSteamID().m_SteamID;
            SteamName = SteamFriends.GetPersonaName();
            var avatar_handle = SteamFriends.GetMediumFriendAvatar(new CSteamID(SteamUserID));
            if (avatar_handle != 0)
                PlayerAvatar = new SteamAvatar(avatar_handle);
            callResultLobbyList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
            for (int i = 0; i < MaxLobbies; i++)
            {
                callResultsLobbyCreated[i] = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
                callResultsLobbyEnter[i] = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
            }
            callbackLobbyChatMsg = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);
            callbackLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        }
        
        var assetBundle = AssetBundle.LoadFromMemory(TableModelBundle.bytes);
        KoiKoiPlayroom.TableModel = assetBundle.LoadAsset<GameObject>("TableModel");
        assetBundle.Unload(false);
        TableModelBundle = null;
        
        foreach (var texture in CardTextures)
        {
            var splitName = texture.name.Split('_');
            _CardInfos.Add(new CardInfo
            {
                Suit = (CardSuit)Enum.Parse(typeof(CardSuit), splitName[0]),
                Type = (CardType)Enum.Parse(typeof(CardType), splitName[1]),
                Texture = texture
            });
        }

        if (HarmonyInstance == null)
        {
            HarmonyInstance = new Harmony("qkrisi.koikoi.patches");
            HarmonyInstance.PatchAll();
        }
    }

    void Update()
    {
        if(SteamApiInitialized)
            SteamAPI.RunCallbacks();
    }

    public void RegisterForSearch(KoiKoiModule module)
    {
        SearchingModules.Add(module);
    }

    public void InitiateSearch()
    {
        if (Searching || !SteamApiInitialized)
            return;
        Searching = true;
        SearchingNum = 0;
        SteamMatchmaking.AddRequestLobbyListStringFilter("GameID", KoiKoiID, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter("UserID", SteamUserID.ToString(), ELobbyComparison.k_ELobbyComparisonNotEqual);
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        callResultLobbyList.Set(SteamMatchmaking.RequestLobbyList(), OnLobbyMatchList);
    }

    void ForceBot()
    {
        foreach(var module in SearchingModules)
            module.Playroom.NewGameWithBot();
        SearchingModules.Clear();
    }

    bool ProcessFail(bool fail)
    {
        if (SearchingModules.Count == 0)
            return true;
        if(fail)
            SearchingModules[0].Playroom.NewGameWithBot();
        return fail;
    }

    void OnLobbyMatchList(LobbyMatchList_t lobbies, bool fail)
    {
        Searching = false;
        if (fail)
        {
            ForceBot();
            return;
        }
        if (lobbies.m_nLobbiesMatching < SearchingModules.Count)
        {
            for (int i = 0; i < MaxLobbies - lobbies.m_nLobbiesMatching; i++)
            {
                SearchingNum++;
                callResultsLobbyCreated[i].Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeInvisible, 2), OnLobbyCreated);
            }
        }
        var joinableLobbies = Mathf.Min(lobbies.m_nLobbiesMatching, MaxLobbies);
        for (int i = 0; i < joinableLobbies; i++)
        {
            SearchingNum++;
            var lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            callResultsLobbyEnter[i].Set(SteamMatchmaking.JoinLobby(lobbyId), OnLobbyEntered);
        }

        while (SearchingNum < SearchingModules.Count) 
            SearchingModules[SearchingModules.Count - 1].Playroom.NewGameWithBot();
    }

    void OnLobbyCreated(LobbyCreated_t lobby, bool fail)
    {
        if (ProcessFail(fail || lobby.m_eResult != EResult.k_EResultOK))
            return;
        var lobbyId = new CSteamID(lobby.m_ulSteamIDLobby);
        if (NetworkModules.ContainsKey(lobbyId))
            return;
        SteamMatchmaking.SetLobbyData(lobbyId, "GameID", KoiKoiID);
        SteamMatchmaking.SetLobbyData(lobbyId, "UserID", SteamUserID.ToString());
        var s1 = GetRandomSeed();
        var s2 = GetRandomSeed();
        var s3 = GetRandomSeed();
        SteamMatchmaking.SetLobbyData(lobbyId, "Seeds", $"{s1}_{s2}_{s3}");
        var module = SearchingModules[0];
        SearchingModules.RemoveAt(0);
        module.LobbyID = lobbyId;
        module.Log($"Created lobby {lobbyId.m_SteamID}");
        NetworkModules.Add(lobbyId, module);
        module.Playroom.Seeds.Clear();
        module.Playroom.Seeds.Enqueue(s1);
        module.Playroom.Seeds.Enqueue(s2);
        module.Playroom.Seeds.Enqueue(s3);
        module.Playroom.Bot.Active = false;
        module.Playroom.PlayerNum = 0;
        /*if(--SearchingNum == 0)
            ForceBot();*/
    }

    void OnLobbyEntered(LobbyEnter_t lobby, bool fail)
    {
        if (ProcessFail(fail || lobby.m_bLocked || lobby.m_EChatRoomEnterResponse !=
                (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess))
            return;
        var lobbyId = new CSteamID(lobby.m_ulSteamIDLobby);
        var ownerId = SteamMatchmaking.GetLobbyOwner(lobbyId);
        if (NetworkModules.ContainsKey(lobbyId) || ownerId.m_SteamID == SteamUserID)
            return;
        var module = SearchingModules[0];
        SearchingModules.RemoveAt(0);
        module.LobbyID = lobbyId;
        module.Playroom.OpponentNameStr = SteamFriends.GetFriendPersonaName(ownerId);
        var avatarHandle = SteamFriends.GetMediumFriendAvatar(ownerId);
        if(avatarHandle != 0)
            module.Playroom.OpponentAvatar = new SteamAvatar(avatarHandle);
        module.Playroom.UpdateOpponentName();
        NetworkModules.Add(lobbyId, module);
        var seeds = SteamMatchmaking.GetLobbyData(lobbyId, "Seeds").Split('_');
        module.Playroom.Seeds.Clear();
        foreach(var s in seeds)
            module.Playroom.Seeds.Enqueue(int.Parse(s));
        module.Playroom.Bot.Active = false;
        module.Playroom.PlayerNum = 1;
        /*if(--SearchingNum == 0)
            ForceBot();*/
        module.Playroom.NewGame();
    }

    void OnLobbyChatMsg(LobbyChatMsg_t message)
    {
        var lobbyId = new CSteamID(message.m_ulSteamIDLobby);
        KoiKoiModule module;
        if (message.m_eChatEntryType != (byte)EChatEntryType.k_EChatEntryTypeChatMsg || message.m_ulSteamIDUser == SteamUserID || !NetworkModules.TryGetValue(lobbyId, out module))
            return;
        CSteamID _sender;
        var messageBuffer = new byte[16];
        EChatEntryType _entryType;
        var messageLength = SteamMatchmaking.GetLobbyChatEntry(lobbyId, (int)message.m_iChatID, out _sender, messageBuffer,
            messageBuffer.Length, out _entryType);
        var messageData = new byte[messageLength];
        Array.Copy(messageBuffer, messageData, messageLength);
        var messageString = Encoding.UTF8.GetString(messageData).TrimEnd('\0');
        module.Playroom.NetworkInput.Enqueue(int.Parse(messageString));
    }

    void OnLobbyChatUpdate(LobbyChatUpdate_t update)
    {
        var lobbyId = new CSteamID(update.m_ulSteamIDLobby);
        KoiKoiModule module;
        if (SteamMatchmaking.GetLobbyOwner(lobbyId).m_SteamID != SteamUserID ||
            !NetworkModules.TryGetValue(lobbyId, out module) || update.m_ulSteamIDUserChanged == SteamUserID)
            return;
        var opponentID = new CSteamID(update.m_ulSteamIDUserChanged);
        var opponentName = SteamFriends.GetFriendPersonaName(opponentID);
        if (HasChangeFlag(update.m_rgfChatMemberStateChange,
                EChatMemberStateChange.k_EChatMemberStateChangeEntered))
        {
            module.Log($"{opponentName} has joined");
            module.Playroom.OpponentNameStr = opponentName;
            var avatarHandle = SteamFriends.GetMediumFriendAvatar(opponentID);
            if(avatarHandle != 0)
                module.Playroom.OpponentAvatar = new SteamAvatar(avatarHandle);
            module.Playroom.Bot.Active = false;
            module.Playroom.UpdateOpponentName();
            SteamMatchmaking.SetLobbyJoinable(lobbyId, false);
            module.Playroom.NewGame();
        }
        else
        {
            module.Log($"{opponentName} has left");
            module.Playroom.Bot.Active = true;
            module.Playroom.UpdateOpponentName();
            module.Playroom.ProcessStateWithBot();
        }
    }

    private bool HasChangeFlag(uint change, EChatMemberStateChange flag) => (change & (uint)flag) == (uint)flag;
}