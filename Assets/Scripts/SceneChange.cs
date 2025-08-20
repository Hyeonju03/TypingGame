using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class SceneChange : MonoBehaviour
{
    [Header("씬 이름")]
    [SerializeField] private string mainScene = "Main";
    [SerializeField] private string tutorialScene = "Tutorial";
    [SerializeField] private string stage1Scene = "Stage1";
    [SerializeField] private string stage2Scene = "Stage2";
    [SerializeField] private string stage3Scene = "Stage3";
    [SerializeField] private bool resetTimeScaleOnLoad = true;

    [Header("로딩 오버레이(연결 안하면 비표시)")]
    [SerializeField] private GameObject loadingRoot;          // 패널(검은 반투명 등)
    [SerializeField] private CanvasGroup loadingCg;            // 페이드용
    [SerializeField] private Slider progressBar;               // 선택
    [SerializeField] private TextMeshProUGUI loadingText;      // 선택
    [SerializeField, Range(0f, 2f)] private float minShowTime = 0.8f;
    [SerializeField, Range(0f, 1f)] private float fadeInTime = 0.15f;

    bool busy;
    bool UseOverlay => loadingRoot != null && loadingCg != null;

    void Awake()
    {
        // ✔ 오버레이 연결 상태와 무관하게 클릭 막힘 방지
        if (loadingRoot) loadingRoot.SetActive(false);

        // 나머지는 있으면 초기화
        if (loadingCg) loadingCg.alpha = 0f;
        if (progressBar) progressBar.value = 0f;
        if (loadingText) loadingText.text = "";
    }

    // 내부 공용 로더
    void Load(string sceneName)
    {
        if (busy) return;
        if (resetTimeScaleOnLoad) Time.timeScale = 1f;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[SceneChange] Build Settings에 '{sceneName}'가 없습니다.");
            return;
        }
        StartCoroutine(LoadAsync(sceneName));
    }

    IEnumerator LoadAsync(string sceneName)
    {
        busy = true;

        // 오버레이 안 쓰면 그냥 비동기 로드만
        if (!UseOverlay)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            busy = false;
            yield break;
        }

        // 오버레이 사용
        loadingRoot.SetActive(true);
        yield return Fade(loadingCg, 0f, 1f, fadeInTime);

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        float shown = 0f;
        while (op.progress < 0.9f)
        {
            float p = Mathf.Clamp01(op.progress / 0.9f);
            if (progressBar) progressBar.value = p;
            if (loadingText) loadingText.text = $"로딩중... {(int)(p * 100)}%";
            shown += Time.unscaledDeltaTime;
            yield return null;
        }

        if (progressBar) progressBar.value = 1f;
        if (loadingText) loadingText.text = "마무리 중...";

        // 최소 노출 시간 보장 (같은 씬 전환은 안 씀)
        while (shown < minShowTime)
        {
            shown += Time.unscaledDeltaTime;
            yield return null;
        }

        // 씬 활성화(오버레이는 씬 교체와 함께 사라짐)
        op.allowSceneActivation = true;
        busy = false;
    }

    static IEnumerator Fade(CanvasGroup cg, float from, float to, float dur)
    {
        cg.alpha = from;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        cg.alpha = to;
    }

    // 버튼용
    public void GoMain() => Load(mainScene);
    public void GoTutorial() => Load(tutorialScene);
    public void StartStage1() => Load(stage1Scene);
    public void StartStage2() => Load(stage2Scene);
    public void StartStage3() => Load(stage3Scene);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}
