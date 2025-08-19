// SceneChange.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChange : MonoBehaviour
{
    [Header("기본 옵션")]
    [SerializeField] private string mainMenuScene = "Main"; // 메인 메뉴 씬 이름
    [SerializeField] private float minShowTime = 0f;        // 로딩 최소 표시 시간(옵션)
    [SerializeField] private bool resetTimeScaleOnLoad = true;

    bool busy = false; // 중복 클릭 방지

    // ========== 공용 로더 ==========
    public void LoadByName(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[SceneChange] Build Settings에 '{sceneName}'가 없습니다.");
            return;
        }
        TryLoad(() => SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single));
    }

    public void LoadByIndex(int buildIndex)
    {
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"[SceneChange] 유효하지 않은 인덱스: {buildIndex}");
            return;
        }
        TryLoad(() => SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single));
    }

    // ========== 자주 쓰는 바로가기 ==========
    public void StartStage1() => LoadByName("Stage1");
    public void StartStage2() => LoadByName("Stage2");
    public void GoMain() => LoadByName(mainMenuScene);
    public void ReloadCurrent() => LoadByIndex(SceneManager.GetActiveScene().buildIndex);

    public void LoadNext()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings) LoadByIndex(next);
        else Debug.LogWarning("[SceneChange] 마지막 씬입니다.");
    }

    public void LoadPrev()
    {
        int prev = SceneManager.GetActiveScene().buildIndex - 1;
        if (prev >= 0) LoadByIndex(prev);
        else Debug.LogWarning("[SceneChange] 첫 번째 씬입니다.");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ========== 내부 구현 ==========
    void TryLoad(System.Func<AsyncOperation> loader)
    {
        if (busy) return;
        if (resetTimeScaleOnLoad) Time.timeScale = 1f;
        StartCoroutine(CoLoad(loader));
    }

    IEnumerator CoLoad(System.Func<AsyncOperation> loader)
    {
        busy = true;
        yield return null; // UI 반영 1프레임
        var op = loader();
        if (op == null) { busy = false; yield break; }

        op.allowSceneActivation = false;
        float start = Time.realtimeSinceStartup;

        while (op.progress < 0.9f) yield return null; // 로딩 진행(0.9 ≒ 준비 완료)

        // 로딩 화면 유지가 필요하면 최소 표기 시간 보장
        while (Time.realtimeSinceStartup - start < minShowTime) yield return null;

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
        busy = false;
    }

    // 유틸(선택)
    public bool CanLoad(string sceneName) => Application.CanStreamedLevelBeLoaded(sceneName);
}
