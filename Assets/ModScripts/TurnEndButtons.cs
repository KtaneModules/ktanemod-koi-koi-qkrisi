using UnityEngine;

[RequireComponent(typeof(Animation))]
public class TurnEndButtons : MonoBehaviour
{
    internal GameObject KoiKoiButton;
    internal GameObject StopButton;
    
    private Animation animation;

    void Awake()
    {
        animation = GetComponent<Animation>();
    }

    public void OpenButtons()
    {
        animation.Play("case_open");
        KoiKoiButton?.SetActive(true);
        StopButton?.SetActive(true);
    }

    public void CloseButtons()
    {
        KoiKoiButton?.SetActive(false);
        StopButton?.SetActive(false);
        animation.Play("case_close");
    }
}