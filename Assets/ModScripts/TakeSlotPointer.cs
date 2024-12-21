using System;
using System.Collections;
using System.Collections.Generic;
using KoiKoi;
using UnityEngine;

public class TakeSlotPointer : MonoBehaviour
{
    public CardType Type;
    [SerializeField] internal int Multiplier;

    private int Row;
    private int Column;
    private Vector3 StartingPos = Vector3.zero;
    internal Vector3 ResetPos = Vector3.zero;

    private const float RowDiff = -0.091f;
    private const float ColDiff = 0.07f;
    
    internal List<HanafudaCard> Cards = new List<HanafudaCard>();

    void Start()
    {
        if (Multiplier == 1)
        {
           StartingPos = new Vector3(-0.038f, 0f, -0.52f); 
           ResetPos = new Vector3(-0.2f, 0f, -0.6f);
        }
        else
        {
            StartingPos = new Vector3(1.2f, 0f, 0.52f);
            ResetPos = new Vector3(1.4f, 0f, 0.6f);
        }
    }

    public Vector3 GetPositionAndAdvance()
    {
        var target = StartingPos + new Vector3(Column * ColDiff * Multiplier, (Row+1) * (Column+1) * KoiKoiPlayroom.CardYDiff,
            Row * RowDiff * Multiplier);
        
        if (++Column == 13)
        {
            Column = 0;
            Row++;
        }

        return target;
    }

    private IEnumerator MoveAll(Func<Vector3> getPos)
    {
        var coroutines = new IEnumerator[Cards.Count];
        for (int i = 0; i < Cards.Count; i++)
            coroutines[i] = Cards[i].MoveTo(getPos(), false, straight: true);
        return Utils.SynchronizedCoroutines(this, coroutines);
    }

    public IEnumerator ResetAll()
    {
        return MoveAll(() => ResetPos);
    }
    
    public IEnumerator SpreadOut()
    {
        return MoveAll(GetPositionAndAdvance);
    }

    public void ResetPosition(bool clearCards)
    {
        Row = 0;
        Column = 0;
        if (clearCards)
            Cards.Clear();
    }
}