using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways, RequireComponent(typeof(RectTransform)), RequireComponent(typeof(Image))]
public class RoundedPanel : MonoBehaviour
{
    public float radiusPx = 24f;
    public float borderPx = 6f;
    public Color fillColor = new Color32(0xF6, 0xC1, 0x5B, 255);
    public Color borderColor = new Color32(0xB8, 0x84, 0x2E, 255);
    public bool useGraphicColor = false; // Image.color 틴트 사용 여부

    RectTransform rt;
    Image img;
    Material inst;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        if (img.material == null || img.material.shader.name != "UI/RoundedRect")
            img.material = new Material(Shader.Find("UI/RoundedRect"));
        // 인스턴스화된 머티리얼을 사용
        inst = img.material;
        Apply();
    }

    void OnEnable() { Apply(); }
    void OnValidate() { if (rt == null) rt = GetComponent<RectTransform>(); if (img == null) img = GetComponent<Image>(); Apply(); }
    void OnRectTransformDimensionsChange() { Apply(); }

    void Apply()
    {
        if (rt == null || img == null) return;
        if (inst == null) { inst = img.material; if (inst == null) return; }

        Vector2 size = rt.rect.size;               // px
        float R = Mathf.Clamp(radiusPx, 0, Mathf.Min(size.x, size.y) * 0.5f - 0.5f);
        float B = Mathf.Clamp(borderPx, 0, R);

        inst.SetVector("_RectSize", size);
        inst.SetFloat("_Radius", R);
        inst.SetFloat("_Border", B);
        inst.SetColor("_FillColor", fillColor);
        inst.SetColor("_BorderColor", borderColor);
        inst.SetFloat("_UseGraphicColor", useGraphicColor ? 1f : 0f);
    }
}
