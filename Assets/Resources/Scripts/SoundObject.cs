using UnityEngine;

public class SoundObject : MonoBehaviour
{
    public int adsCount = 3;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
