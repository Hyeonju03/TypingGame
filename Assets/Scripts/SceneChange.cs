using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    [Header("씬 이름")]
    [SerializeField] private string mainScene = "Main";
    [SerializeField] private string tutorialScene = "Tutorial";
    [SerializeField] private string stage1Scene = "Stage1";
    [SerializeField] private string stage2Scene = "Stage2";
    [SerializeField] private string stage3Scene = "Stage3";
    [SerializeField] private bool resetTimeScaleOnLoad = true;

    bool busy; // 중복 클릭 방지

    // 내부 공용 로더(동기 로드: 단순/빠름)
    void Load(string sceneName)
    {
        if (busy) return;
        if (resetTimeScaleOnLoad) Time.timeScale = 1f;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[SceneChange] Build Settings에 '{sceneName}'가 없습니다.");
            return;
        }

        busy = true;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // 버튼용 공개 메서드
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
