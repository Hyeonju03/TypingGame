using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    [Header("�� �̸�")]
    [SerializeField] private string mainScene = "Main";
    [SerializeField] private string tutorialScene = "Tutorial";
    [SerializeField] private string stage1Scene = "Stage1";
    [SerializeField] private string stage2Scene = "Stage2";
    [SerializeField] private string stage3Scene = "Stage3";
    [SerializeField] private bool resetTimeScaleOnLoad = true;

    bool busy; // �ߺ� Ŭ�� ����

    // ���� ���� �δ�(���� �ε�: �ܼ�/����)
    void Load(string sceneName)
    {
        if (busy) return;
        if (resetTimeScaleOnLoad) Time.timeScale = 1f;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[SceneChange] Build Settings�� '{sceneName}'�� �����ϴ�.");
            return;
        }

        busy = true;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // ��ư�� ���� �޼���
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
