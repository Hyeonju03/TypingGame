using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Refs")]
    public InputManager inputManager;
    public HealthManager healthManager;
    public ReWordSpawner reWordSpawner;
    public GameObject gameOverPanel;
    public TMP_Text gameOverText;
    public TMP_Text gameClearText;
    public TMP_Text pointsText;
    public CanvasGroup fadeCanvasGroup; // 페이드 아웃용

    [Header("Point Settings")]
    public int wordsToClearGame = 15;
    public int pointsPerClear = 150;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    [Header("DB Settings")]
    public string userId = "your_user_id";
    public string updatePointUrl = "http://localhost:9001/api/users/updatePoints/{0}";

    [HideInInspector] public int totalPoints = 0; // 누적 포인트

    private int wordsTyped = 0;
    private bool gameOver = false;

    void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 씬 로드 이벤트 구독 (씬 전환 시 호출)
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene '{scene.name}' loaded. Re-linking objects.");

        // 새로운 씬에서 필요한 오브젝트를 다시 찾아서 연결
        healthManager = FindObjectOfType<HealthManager>();
        inputManager = FindObjectOfType<InputManager>();
        reWordSpawner = FindObjectOfType<ReWordSpawner>();

        // 이벤트 재구독
        if (inputManager != null) inputManager.OnWordTyped += OnWordTypedHandler;
        if (healthManager != null) healthManager.OnGameOver += OnGameOverHandler;

        // 씬 전환 시 게임 상태 초기화
        wordsTyped = 0;
        gameOver = false;

        // UI 초기화
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
    }

    private void OnWordTypedHandler()
    {
        if (gameOver) return;
        wordsTyped++;

        if (wordsTyped >= wordsToClearGame)
        {
            totalPoints += pointsPerClear;
            StartCoroutine(HandleStageClear());
        }
    }

    private void OnGameOverHandler()
    {
        if (gameOver) return;

        gameOver = true;
        StartCoroutine(HandleGameOver());
    }

    private IEnumerator HandleStageClear()
    {
        gameOver = true;

        // 게임 진행 관련 오브젝트 정지
        if (reWordSpawner != null) reWordSpawner.Pause();
        if (inputManager != null)
        {
            inputManager.ClearAllWords();
            inputManager.inputField.interactable = false;
        }

        // Stage1~2는 FadeOut 후 다음 Stage
        string currentScene = SceneManager.GetActiveScene().name;
        string nextScene = currentScene switch
        {
            "Stage1" => "Stage2",
            "Stage2" => "Stage3",
            _ => null
        };

        if (!string.IsNullOrEmpty(nextScene))
        {
            yield return StartCoroutine(FadeOut());
            SceneManager.LoadScene(nextScene);
            yield break;
        }

        // Stage3 클리어
        if (gameClearText != null) gameClearText.gameObject.SetActive(true);
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (pointsText != null) pointsText.text = $"포인트: {totalPoints}";
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        yield return StartCoroutine(PostPointsToDb());
    }

    private IEnumerator HandleGameOver()
    {
        // 게임 진행 관련 오브젝트 정지
        if (reWordSpawner != null) reWordSpawner.Pause();
        if (inputManager != null)
        {
            inputManager.ClearAllWords();
            inputManager.inputField.interactable = false;
        }

        // UI 표시
        if (gameClearText != null) gameClearText.gameObject.SetActive(false);
        if (gameOverText != null) gameOverText.gameObject.SetActive(true);
        if (pointsText != null) pointsText.text = $"포인트: {totalPoints}";
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // DB 전송
        yield return StartCoroutine(PostPointsToDb());
    }

    private IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator PostPointsToDb()
    {
        string url = string.Format(updatePointUrl, userId);
        string jsonData = JsonUtility.ToJson(new PointData { points = totalPoints });

        using (UnityWebRequest www = new UnityWebRequest(url, "PUT"))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[API Error] {www.error}");
            }
            else
            {
                Debug.Log("[API Success] 포인트가 성공적으로 저장되었습니다.");
            }
        }
    }

    [System.Serializable]
    public class PointData
    {
        public int points;
    }

    public void GoToMainScene()
    {
        SceneManager.LoadScene("scene0");
    }
}