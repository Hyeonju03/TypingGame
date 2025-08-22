using UnityEngine;

public class BootWebGLSceneCleaner : MonoBehaviour
{
    public GameObject[] disableOnWebGL;   // WebGL���� �� ����

    void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        foreach (var go in disableOnWebGL)
            if (go) go.SetActive(false);
#endif
    }
}

