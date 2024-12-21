using UnityEngine;

public class YakuListText : MonoBehaviour
{
   private RectTransform selfTransform;
   private Vector2 startPosition;

   private const float DisplaySpeed = 100f;
   
   void Start()
   {
      selfTransform = GetComponent<RectTransform>();
      startPosition = selfTransform.anchoredPosition;
   }
   
   void Update()
   {
      if (selfTransform.sizeDelta.y < 50)
      {
         selfTransform.anchoredPosition = startPosition;
         return;
      }

      if (selfTransform.anchoredPosition.y > startPosition.y + selfTransform.sizeDelta.y * 2.5f)
         selfTransform.anchoredPosition = new Vector2(startPosition.x, -startPosition.y);
      selfTransform.anchoredPosition += new Vector2(0f, DisplaySpeed * Time.deltaTime);
   }
}