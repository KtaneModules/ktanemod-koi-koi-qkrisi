using System;
using System.Collections;
using KoiKoi;
using UnityEngine;

public class HanafudaCard : MonoBehaviour
{
    [SerializeField] private Renderer Front;

    [NonSerialized] public CardSuit Suit;
    [NonSerialized] public CardType Type;
    [NonSerialized] public CardState State;
    [NonSerialized] public KoiKoiPlayroom Playroom;
    public TableSlot Slot;

    private const CardType RainDummy = CardType.Bright - 1;

    public CardType SortType => Type == CardType.Rain ? RainDummy : Type;
    
    public bool HasType(CardType type) => (Type & type) == type;

    public IEnumerator MoveTo(Vector3 target, bool flipZ, bool flipY = false, bool straight = false)
    {
        //flipY = false;
        var start = transform.localPosition;
        var dist = new Vector3(target.x, transform.localPosition.y, target.z) - transform.localPosition;
        var control = straight ? target : (dist / 2) + new Vector3(0f, Mathf.Abs(target.y - transform.localPosition.y)*7, 0f);
        var targetAngles = transform.eulerAngles - new Vector3(0f, flipY ? 180f : 0f, flipZ ? 180f : 0f);
        var targetAngleZ = transform.eulerAngles.z - 180f;
        var targetAngleY = transform.eulerAngles.y + 180f;
        var flip = flipZ || flipY;
        var t = 0f;
        while (t < 1f)
        {
            transform.localPosition = BezierCurves.Quad(start, control, target, t);
            if (flip)
            {
                var rt = 2f * Time.deltaTime;
                var currentAngles = transform.eulerAngles;
                transform.eulerAngles = new Vector3(currentAngles.x,
                    flipY ? Mathf.Lerp(currentAngles.y, targetAngleY, rt) : currentAngles.y,
                    flipZ ? Mathf.Lerp(currentAngles.z, targetAngleZ, rt) : currentAngles.z);
                //transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, targetAnglesZ, 2f * Time.deltaTime);
            }

            t += 1.5f*Time.deltaTime;
            yield return null;
        }
        transform.localPosition = target;
        if (flip)
            transform.eulerAngles = targetAngles;
    }

    public void SetInfo(CardInfo info)
    {
        Suit = info.Suit;
        Type = info.Type;
        Front.material.mainTexture = info.Texture;
        gameObject.name = info.Texture.name;
    }

    void Start()
    {
        GetComponent<KMSelectable>().OnInteract += Select;
    }
    
    public bool Select()
    {
        if (!Playroom.SelfTurn)
            return false;
        switch (Playroom.State)
        {
            case GameState.SelectCardFromHand:
                if (State != CardState.InPlayerHand)
                    break;
                Playroom.Module.Send(Playroom.SelfInfo.Hand.IndexOf(this) + 1);
                Playroom.SelectedCard = this;
                Playroom.HandleSelected(false);
                break;
            case GameState.SelectCardOnBoardFromHand:
            case GameState.SelectCardOnBoardFromStack:
                if (State == CardState.InPlayerHand && Playroom.State == GameState.SelectCardOnBoardFromHand)
                {
                    Playroom.Module.Send(-1);
                    goto case GameState.SelectCardFromHand;
                }

                if (State != CardState.OnTable)
                    break;
                if (Suit != Playroom.SelectedCard?.Suit)
                {
                    Debug.LogError($"Unmatching suits: {Suit} -> {Playroom.SelectedCard?.Suit}");
                    break;
                }

                var index = Playroom.TableSlots.IndexOf(Slot) + 1;
                if(index % 2 == 0)
                    Playroom.Module.Send(0);
                Playroom.Module.Send((index-1)/2+1);
                var _state = Playroom.State;
                Playroom.State = GameState.Wait;
                Playroom.StartCoroutine(Playroom.Take(_state == GameState.SelectCardOnBoardFromStack, Slot));
                break;
        }
        return false;
    }
}