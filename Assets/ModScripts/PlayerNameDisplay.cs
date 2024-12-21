using KoiKoi;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameDisplay : MonoBehaviour
{
	public Image Avatar;
	public Text NameText;

	private RectTransform selfRect;
	private RectTransform nameTextRect;
	
	private const float DisplaySpeed = 100f;

	public static Material AvatarMaterial;
	
	void Start()
	{
		selfRect = GetComponent<RectTransform>();
		nameTextRect = NameText.GetComponent<RectTransform>();
	}
	
	void Update()
	{
		if (selfRect.anchoredPosition.x < nameTextRect.sizeDelta.x * -3 - 30f)
			selfRect.anchoredPosition = new Vector2(110f, 0f);
		selfRect.anchoredPosition -= new Vector2(DisplaySpeed * Time.deltaTime, 0f);
	}

	public void UpdatePlayer(string name, Texture2D avatar)
	{
		NameText.text = name;
		Avatar.enabled = avatar != null;
		if (Avatar.enabled)
			Avatar.sprite = Sprite.Create(avatar, new Rect(0f, 0f, avatar.width, avatar.height), Vector2.zero);
	}

	public void UpdatePlayer(string name, SteamAvatar avatar)
	{
		UpdatePlayer(name, avatar?.AvatarTexture);
	}
}
