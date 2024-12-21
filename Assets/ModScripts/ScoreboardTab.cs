using KoiKoi;
using UnityEngine;

public class ScoreboardTab : MonoBehaviour
{
    public ScoreboardState State;
    public GameObject[] Objects;

    public void SetObjectsActive(bool active)
    {
        if (Objects == null)
            return;
        foreach(var obj in Objects)
            obj.SetActive(active);
    }
}