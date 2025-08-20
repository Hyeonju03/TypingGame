using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using System;
using System.Runtime.InteropServices;

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
    public CanvasGroup fadeCanvasGroup;

    [Header("Point Settings")]
    public int wordsToClearGame = 15;
    public int pointsPerClear = 50;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    [HideInInspector] public int totalPoints = 0;

    private int wordsTyped = 0;
    private bool gameOver = false;

    // JavaScript 함수를 호출하기 위한 DLL Import
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
        Debug.Log($"wordsTyped: {wordsTyped}");
        if (wordsTyped >= wordsToClearGame)
        {
            // 중복 포인트 증가를 막기 위해 이 코드는 삭제
            // totalPoints += pointsPerClear; 
            StartCoroutine(HandleStageClear());
        }
    }

    private void OnGameOverHandler()
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log("Game over handler called.");
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
                gameClearText = gameOverPanel.transform.Find("GameClearText")?.GetComponent<TMP_Text>();
                gameOverText = gameOverPanel.transform.Find("GameOverText")?.GetComponent<TMP_Text>();
                pointsText = gameOverPanel.transform.Find("PointsBackground/PointsText")?.GetComponent<TMP_Text>();
            }
            fadeCanvasGroup = canvas.GetComponent<CanvasGroup>();
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

        // 클리어 시점에만 포인트를 추가
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
            if (gameClearText != null) gameClearText.gameObject.SetActive(true);
            if (gameOverText != null) gameOverText.gameObject.SetActive(false);
            if (pointsText != null) pointsText.text = $"포인트: {totalPoints}";
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            SendPointsToWeb();
        }

        yield return null;
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

        if (gameClearText != null) gameClearText.gameObject.SetActive(false);
        if (gameOverText != null) gameOverText.gameObject.SetActive(true);
        if (pointsText != null) pointsText.text = $"포인트: {totalPoints}";
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // 게임오버 시에는 추가 포인트 없이 현재 totalPoints 전송
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
        Debug.Log("[GameManager] Sending points to WebGL page.");
        ReceivePointsFromUnity(this.totalPoints);
#endif
    }

    public void GoToMainScene()
    {
        SceneManager.LoadScene("Main");
    }
}