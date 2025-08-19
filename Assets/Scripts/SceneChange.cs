// SceneChange.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChange : MonoBehaviour
{
    [Header("�⺻ �ɼ�")]
    [SerializeField] private string mainMenuScene = "Main"; // ���� �޴� �� �̸�
    [SerializeField] private float minShowTime = 0f;        // �ε� �ּ� ǥ�� �ð�(�ɼ�)
    [SerializeField] private bool resetTimeScaleOnLoad = true;

    bool busy = false; // �ߺ� Ŭ�� ����

    // ========== ���� �δ� ==========
    public void LoadByName(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[SceneChange] Build Settings�� '{sceneName}'�� �����ϴ�.");
            return;
        }
        TryLoad(() => SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single));
    }

    public void LoadByIndex(int buildIndex)
    {
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"[SceneChange] ��ȿ���� ���� �ε���: {buildIndex}");
            return;
        }
        TryLoad(() => SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single));
    }

    // ========== ���� ���� �ٷΰ��� ==========
    public void StartStage1() => LoadByName("Stage1");
    public void StartStage2() => LoadByName("Stage2");
    public void GoMain() => LoadByName(mainMenuScene);
    public void ReloadCurrent() => LoadByIndex(SceneManager.GetActiveScene().buildIndex);

    public void LoadNext()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings) LoadByIndex(next);
        else Debug.LogWarning("[SceneChange] ������ ���Դϴ�.");
    }

    public void LoadPrev()
    {
        int prev = SceneManager.GetActiveScene().buildIndex - 1;
        if (prev >= 0) LoadByIndex(prev);
        else Debug.LogWarning("[SceneChange] ù ��° ���Դϴ�.");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ========== ���� ���� ==========
    void TryLoad(System.Func<AsyncOperation> loader)
    {
        if (busy) return;
        if (resetTimeScaleOnLoad) Time.timeScale = 1f;
        StartCoroutine(CoLoad(loader));
    }

    IEnumerator CoLoad(System.Func<AsyncOperation> loader)
    {
        busy = true;
        yield return null; // UI �ݿ� 1������
        var op = loader();
        if (op == null) { busy = false; yield break; }

        op.allowSceneActivation = false;
        float start = Time.realtimeSinceStartup;

        while (op.progress < 0.9f) yield return null; // �ε� ����(0.9 �� �غ� �Ϸ�)

        // �ε� ȭ�� ������ �ʿ��ϸ� �ּ� ǥ�� �ð� ����
        while (Time.realtimeSinceStartup - start < minShowTime) yield return null;

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
        busy = false;
    }

    // ��ƿ(����)
    public bool CanLoad(string sceneName) => Application.CanStreamedLevelBeLoaded(sceneName);
}
