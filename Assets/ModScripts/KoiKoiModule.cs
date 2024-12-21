using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using KoiKoi;
using MemberForwarding;
using Steamworks;
using UnityEngine;
using Patches = KoiKoi.Patches;

public class KoiKoiModule : MonoBehaviour
{
    public GameObject PlayroomPrefab;
    public Transform Monitor;
    public GameObject Backing;
    
    [NonSerialized] public KoiKoiPlayroom Playroom;

    private KMSelectable Selectable;
    public KMSelectable[] ModuleSelectables;
    [NonSerialized] public KMSelectable[] PermanentChildren;

    internal KMAudio Audio;

    private static int ModuleIDCounter;
    private int ModuleID;

    [NonSerialized] public CSteamID LobbyID = CSteamID.Nil;
    
    internal bool TwitchPlaysActive;
    internal string TwitchID;
    internal string TwitchClaim = "";
    private static Type TwitchModuleType = null;
    internal bool TPActive => TwitchPlaysActive && TwitchModuleType != null;
    
#if UNITY_EDITOR
    void Awake_TH()
#else
    void Awake()
#endif
    {
        Selectable = GetComponent<KMSelectable>();
        Audio = GetComponent<KMAudio>();
        ModuleIDCounter = 0;
        KoiKoiService.Instance.SearchingModules.Clear();
        KoiKoiService.Instance.NetworkModules.Clear();
        PermanentChildren = ModuleSelectables.ToArray();
        GetComponent<KMBombModule>().OnActivate += CreatePlayroom;
        if (TwitchModuleType == null)
        {
            TwitchModuleType = ReflectionHelper.FindType("TwitchModule", "TwitchPlaysAssembly");
            if (TwitchModuleType != null)
            {
                KoiKoiService.HarmonyInstance.Patch(AccessTools.Method(TwitchModuleType, "SetClaimedBy"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.TPOnClaimChange))));
                KoiKoiService.HarmonyInstance.Patch(AccessTools.Method(TwitchModuleType, "SetUnclaimed"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.TPOnUnclaim))));
                MemberForwardControls.ForwardTypes("qkrisi.koikoi.forwards", typeof(Forwards));
            }
        }
    }
    
    void Start()
    {
#if UNITY_EDITOR
        Awake_TH();     //Let KoiKoiService initialize first
#endif
        ModuleID = ++ModuleIDCounter;
        KoiKoiService.Instance.RegisterForSearch(this);
        Monitor.gameObject.layer = 11;
        var selectable = GetComponent<KMSelectable>();
        selectable.OnFocus += () =>
        {
            if (Playroom == null)
                return;
            Patches.CurrentCamera = Playroom.PlayerCamera;
            Patches.CurrentMonitor = Monitor;
        };
        selectable.OnDefocus += GameFixes.OnDefocus(() =>
        {
            if (Playroom == null)
                return;
            if(Patches.CurrentCamera == Playroom.PlayerCamera)
                Patches.CurrentCamera = null;
            if(Patches.CurrentMonitor == Monitor)
                Patches.CurrentMonitor = null;
        });
    }

    void CreatePlayroom()
    {
        if (TPActive)
        {
            foreach (var module in Forwards.TPModules)
            {
                var bombComponent = Forwards.TPBombComponent(module);
                if (bombComponent.GetComponent<KoiKoiModule>() == this)
                {
                    TwitchID = Forwards.TPModuleID(module);
                    ((MonoBehaviour)module).transform.Find("MultiDeckerUI").gameObject.SetActive(false);
                }
            }
        }
        PermanentChildren = ModuleSelectables.ToArray();
        Playroom = Instantiate(PlayroomPrefab,
                new Vector3(1000f, -1000f, 1000f) + (ModuleID - 1) * new Vector3(100f, 0f, 100f),
                Quaternion.Euler(0f, 0f, 0f), null)
            .GetComponent<KoiKoiPlayroom>();
        Monitor.GetComponent<Renderer>().material.mainTexture = Playroom.InitTexture();
        Monitor.transform.localScale = new Vector3(0.16757f, 0.001f, 0.16575f);
        Backing.SetActive(false);
        Playroom.Module = this;
        Playroom.SetSelectables();
        if(!TPActive && KoiKoiService.Instance.SteamApiInitialized)
            KoiKoiService.Instance.InitiateSearch();
        else Playroom.NewGameWithBot();
    }

    public void SetChildren(IEnumerable<KMSelectable> children)
    {
        Selectable.Children = PermanentChildren.Concat(children).ToArray();
        Selectable.UpdateChildrenProperly();
    }

    public void LeaveLobby()
    {
        Log("Leaving lobby");
        if (LobbyID.m_SteamID != CSteamID.Nil.m_SteamID)
        {
            SteamMatchmaking.SetLobbyJoinable(LobbyID, false);
            SteamMatchmaking.LeaveLobby(LobbyID);
            KoiKoiService.Instance.NetworkModules.Remove(LobbyID);
        }
        LobbyID = CSteamID.Nil;
    }

    public void Send(int output)
    {
        if (LobbyID.m_SteamID == CSteamID.Nil.m_SteamID)
            return;
        var message = Encoding.UTF8.GetBytes(output.ToString());
        SteamMatchmaking.SendLobbyChatMsg(LobbyID, message, message.Length);
    }

    public void SetTPClaim(string username)
    {
        TwitchClaim = username;
        Playroom?.UpdateTwitchClaim(TwitchClaim);
    }

    void OnDestroy()
    {
        LeaveLobby();
        Destroy(Playroom);
    }

    public void Log(string message)
    {
        Debug.Log($"[Koi-Koi #{ModuleID}] {message}");
    }

    [NonSerialized] private string TwitchHelpMessage = "Use '!{0} select #' to select a card from your hand in reading order (1-8). Use '!{0} take 1/2' to take select either the first or second card of the same month to take from the table (reading order). Use '!{0} koikoi/stop' to call Koi-Koi or Stop. Use '!{0} view <plains/ribbons/animals/brights>' to view the taken cards of the specified type for both players. Use '!{0} cycle' to cycle the card types. Use '!{0} newgame' to start a new game.";

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if (command == "newgame")
        {
            if (Playroom.State != GameState.GameEnd)
                goto error;

            yield return null;
            Playroom.NewGameButton.OnInteract();
            yield break;
        }

        Match match;
        match = Regex.Match(command, @"^view (plains?|ribbons?|animals?|brights?)$");
        if (match.Success)
        {
            if (Playroom.State >= GameState.Wait || !Playroom.PlayerSlotMoveAllowed ||
                !Playroom.OpponentSlotMoveAllowed)
                goto error;
            var cType = match.Groups[1].Value;
            int pDelta = 0;
            int oDelta = 0;
            switch (cType[0])
            {
                case 'p':
                    pDelta = 3 - Playroom.CurrentPlayerSlotPointer;
                    oDelta = 3 - Playroom.CurrentOpponentSlotPointer;
                    break;
                case 'r':
                    pDelta = 2 - Playroom.CurrentPlayerSlotPointer;
                    oDelta = 2 - Playroom.CurrentOpponentSlotPointer;
                    break;
                case 'a':
                    pDelta = 1 - Playroom.CurrentPlayerSlotPointer;
                    oDelta = 1 - Playroom.CurrentOpponentSlotPointer;
                    break;
                case 'b':
                    pDelta = -Playroom.CurrentPlayerSlotPointer;
                    oDelta = -Playroom.CurrentOpponentSlotPointer;
                    break;
            }
            yield return null;
            if(pDelta != 0)
                Playroom.NextPointer(true, pDelta);
            if(oDelta != 0)
                Playroom.NextPointer(false, oDelta);
            yield break;
        }

        if (command == "cycle")
        {
            if (Playroom.State >= GameState.Wait || !Playroom.PlayerSlotMoveAllowed ||
                !Playroom.OpponentSlotMoveAllowed)
                goto error;
            yield return null;
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitUntil(() => Playroom.PlayerSlotMoveAllowed && Playroom.OpponentSlotMoveAllowed);
                Playroom.NextPointer(true, -1);
                Playroom.NextPointer(false, -1);
                yield return new WaitForSeconds(1f);
            }
            yield break;
        }
        
        if (!Playroom.SelfTurn)
            goto error;
        
        match = Regex.Match(command, @"^select ([1-8])$");
        if (match.Success)
        {
            if (Playroom.State != GameState.SelectCardFromHand && Playroom.State != GameState.SelectCardOnBoardFromHand)
                goto error;
            var n = int.Parse(match.Groups[1].Value);
            if (Playroom.SelfInfo.Hand.Count < n)
            {
                yield return null;
                yield return "sendtochaterror You don't have that many cards in your hand!";
                yield break;
            }

            yield return null;
            var card = Playroom.SelfInfo.Hand[n - 1];
            if (Playroom.TableSlots.Count(c => !c.IsFree && c.CurrentCard.Suit == card.Suit) == 2)
                yield return $"sendtochat Please select which {card.Suit} card you'd like to take from the table!";
            card.Select();
            yield break;
        }
        
        match = Regex.Match(command, @"^take ([1-2])$");
        if (match.Success)
        {
            if (Playroom.State != GameState.SelectCardOnBoardFromHand &&
                Playroom.State != GameState.SelectCardOnBoardFromStack)
                goto error;
            bool take = match.Groups[1].Value == "1";

            yield return null;
            for (int i = 0; i < 12; i += 2)
            {
                int x = i == 0 ? 0 : i == 2 ? 10 : i - 2;
                if (!Playroom.TableSlots[x].IsFree &&
                    Playroom.TableSlots[x].CurrentCard.Suit == Playroom.SelectedCard.Suit)
                {
                    if (take)
                    {
                        Playroom.TableSlots[x].CurrentCard.Select();
                        yield break;
                    }
                    take ^= true;
                }
            }
            for (int i = 1; i < 12; i += 2)
            {
                int x = i == 1 ? 1 : i == 3  ? 11 : i - 2;
                if (!Playroom.TableSlots[x].IsFree &&
                    Playroom.TableSlots[x].CurrentCard.Suit == Playroom.SelectedCard.Suit)
                {
                    if (take)
                    {
                        Playroom.TableSlots[x].CurrentCard.Select();
                        yield break;
                    }
                    take ^= true;
                }
            }
            yield break;
        }

        match = Regex.Match(command, @"^(koi-?koi|stop)$");
        if (match.Success)
        {
            if (Playroom.State != GameState.ChooseIfKoiKoi)
                goto error;
            yield return null;
            if(match.Groups[1].Value.StartsWith("k"))
                Playroom.KoiKoiButton.OnInteract();
            else Playroom.StopButton.OnInteract();
            yield break;
        }
        
        yield break;
        error:
        yield return null;
        yield return "sendtochaterror You can't use this command now!";
    }
}