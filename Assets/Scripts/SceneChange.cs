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
        if (loadingRoot) loadingRoot.SetActive(false); // 클릭 막힘 방지
        if (loadingCg) loadingCg.alpha = 0f;
        if (progressBar) progressBar.value = 0f;
        if (loadingText) loadingText.text = "";
    }

    // ===== 내부 공용 로더 =====
    void Load(string sceneName)
    {
        if (busy) return;
        if (string.IsNullOrWhiteSpace(sceneName)) return;

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

        if (!UseOverlay)
        {
            // 오버레이 없이도 WebGL에서 안정적 동작하도록 비동기+실시간 대기 사용
            var opNoUi = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            opNoUi.allowSceneActivation = true; // 바로 활성화
            while (!opNoUi.isDone) yield return null;
            busy = false;
            yield break;
        }

        // 오버레이 사용
        loadingRoot.SetActive(true);
        yield return Fade(loadingCg, 0f, 1f, fadeInTime);

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        float t0 = Time.realtimeSinceStartup; // 타임스케일 0 영향 배제
        while (op.progress < 0.9f)
        {
            float p = Mathf.Clamp01(op.progress / 0.9f);
            if (progressBar) progressBar.value = p;
            if (loadingText) loadingText.text = $"로딩중... {(int)(p * 100)}%";
            yield return null;
        }

        // 0.9 이후 활성화 대기 구간
        if (progressBar) progressBar.value = 1f;
        if (loadingText) loadingText.text = "마무리 중...";

        // 최소 노출 시간 보장(실시간 기준)
        while (Time.realtimeSinceStartup - t0 < minShowTime)
            yield return null;

        op.allowSceneActivation = true;

        // 씬 전환 완료까지 대기
        while (!op.isDone) yield return null;

        // 다음 씬에서 이 오브젝트가 파괴될 것이므로 여기서 busy만 해제
        busy = false;
    }

    static IEnumerator Fade(CanvasGroup cg, float from, float to, float dur)
    {
        cg.alpha = from;
        float t0 = Time.realtimeSinceStartup;
        float t;
        while ((t = Time.realtimeSinceStartup - t0) < dur)
        {
            cg.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        cg.alpha = to;
    }

    // ===== 버튼용 메서드 =====
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