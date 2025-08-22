using UnityEngine;

public class BootWebGLSceneCleaner : MonoBehaviour
{
    public GameObject[] disableOnWebGL;   // WebGL에서 끌 대상들

    void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        foreach (var go in disableOnWebGL)
            if (go) go.SetActive(false);
#endif
    }
}

