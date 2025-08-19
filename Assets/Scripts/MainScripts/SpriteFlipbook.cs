using UnityEngine;
using UnityEngine.UI;

public class SpriteFlipbook : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 8f;          // 6~10 ±«¿Â
    public bool playOnAwake = true;

    Image img;
    float t; int last;

    void Awake() { img = GetComponent<Image>(); }
    void OnEnable() { t = 0; last = -1; if (playOnAwake && frames != null && frames.Length > 0) img.sprite = frames[0]; }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        t += Time.unscaledDeltaTime * fps;
        int idx = ((int)t) % frames.Length;
        if (idx != last) { img.sprite = frames[idx]; last = idx; }
    }
}
