using UnityEngine;

public class KoiKoiServiceInitializer : MonoBehaviour
{
    public GameObject ServicePrefab;

    void Awake()
    {
        var obj = Instantiate(ServicePrefab, null, true);
        DontDestroyOnLoad(obj);
    }
}