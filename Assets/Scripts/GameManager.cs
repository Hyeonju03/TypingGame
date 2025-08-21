using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using System.Runtime.InteropServices;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Refs")]
    public InputManager inputManager;
    public HealthManager healthManager;
    public ReWordSpawner reWordSpawner;
    public GameObject gameOverPanel;

    // ▼ 타이틀을 TMP_Text → GameObject(이미지)로 교체
    public GameObject gameOverTitle;   // GameOverPanel 하위의 Image 오브젝트
    public GameObject gameClearTitle;  // GameOverPanel 하위의 Image 오브젝트

    public TMP_Text pointsText;        // 점수 텍스트는 그대로 TMP
    public CanvasGroup fadeCanvasGroup;

    [Header("Point Settings")]
    public int wordsToClearGame = 15;
    public int pointsPerClear = 50;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    [HideInInspector] public int totalPoints = 0;

    private int wordsTyped = 0;
    private bool gameOver = false;

    // WebGL 포인트 전달
    [DllImport("__Internal")]
    private static extern void ReceivePointsFromUnity(int points);

    void Awake()
    {
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
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        SubscribeEvents();
    }

    private void OnWordTypedHandler()
    {
        if (gameOver) return;
        wordsTyped++;
        if (wordsTyped >= wordsToClearGame)
            StartCoroutine(HandleStageClear());
    }

    private void OnGameOverHandler()
    {
        if (gameOver) return;
        gameOver = true;
        StartCoroutine(HandleGameOver());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        inputManager = FindObjectOfType<InputManager>();
        healthManager = FindObjectOfType<HealthManager>();
        reWordSpawner = FindObjectOfType<ReWordSpawner>();

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            gameOverPanel = canvas.transform.Find("GameOverPanel")?.gameObject;

            if (gameOverPanel != null)
            {
                // ▼ 이미지 타이틀 찾기 (이름은 Hierarchy와 동일하게)
                gameClearTitle = gameOverPanel.transform.Find("GameClearTitle")?.gameObject;
                gameOverTitle = gameOverPanel.transform.Find("GameOverTitle")?.gameObject;

                // 점수 TMP 텍스트는 계속 사용
                pointsText = gameOverPanel.transform.Find("PointsBackground/PointsText")?.GetComponent<TMP_Text>();
            }

            // (Inspector에서 이미 연결해두었다면 아래 줄은 무시됨)
            if (fadeCanvasGroup == null)
                fadeCanvasGroup = canvas.GetComponentInChildren<CanvasGroup>(true);
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;

        wordsTyped = 0;
        gameOver = false;

        if (healthManager != null) healthManager.ResetHealth();
        if (inputManager != null) inputManager.ClearAllWords();

        SubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (InputManager.Instance != null) InputManager.Instance.OnWordTyped -= OnWordTypedHandler;
        if (healthManager != null) healthManager.OnGameOver -= OnGameOverHandler;

        healthManager = FindObjectOfType<HealthManager>();
        if (healthManager != null) healthManager.OnGameOver += OnGameOverHandler;
        if (InputManager.Instance != null) InputManager.Instance.OnWordTyped += OnWordTypedHandler;
    }

    private IEnumerator HandleStageClear()
    {
        gameOver = true;
        if (reWordSpawner != null) reWordSpawner.Pause();
        if (inputManager != null)
        {
            inputManager.ClearAllWords();
            inputManager.inputField.interactable = false;
        }

        // 클리어 시점에만 포인트 추가
        totalPoints += pointsPerClear;

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
        }
        else
        {
            // ▼ 이미지 토글
            if (gameClearTitle != null) gameClearTitle.SetActive(true);
            if (gameOverTitle != null) gameOverTitle.SetActive(false);

            if (pointsText != null) pointsText.text = $"포인트: {totalPoints}";
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            SendPointsToWeb();
        }
    }

    private IEnumerator HandleGameOver()
    {
        gameOver = true;
        if (reWordSpawner != null) reWordSpawner.Pause();
        if (inputManager != null)
        {
            inputManager.ClearAllWords();
            inputManager.inputField.interactable = false;
        }

        // ▼ 이미지 토글
        if (gameClearTitle != null) gameClearTitle.SetActive(false);
        if (gameOverTitle != null) gameOverTitle.SetActive(true);

        if (pointsText != null) pointsText.text = $"포인트: {totalPoints}";
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        SendPointsToWeb();
        yield return null;
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

    private void SendPointsToWeb()
    {
#if UNITY_WEBGL
        ReceivePointsFromUnity(this.totalPoints);
#endif
    }

    public void GoToMainScene()
    {
        SceneManager.LoadScene("Main");
    }
}
