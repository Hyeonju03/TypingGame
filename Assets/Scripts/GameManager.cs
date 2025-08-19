using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;

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
    public int pointsPerClear = 150;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    [Header("DB Settings")]
    public string userId = "your_user_id";
    public string updatePointUrl = "http://localhost:9001/api/users/updatePoints/{0}";

    [HideInInspector] public int totalPoints = 0;

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

        // 게임 시작 시에도 이벤트 구독을 시도합니다.
        SubscribeEvents();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene '{scene.name}' loaded. Re-linking objects.");

        // 새로운 씬에서 ReWordSpawner를 찾아서 연결합니다.
        reWordSpawner = FindObjectOfType<ReWordSpawner>();

        // 씬이 로드될 때마다 이벤트를 다시 구독합니다.
        SubscribeEvents();

        // 씬 전환 시 게임 상태를 초기화합니다.
        wordsTyped = 0;
        gameOver = false;

        // UI를 초기화합니다.
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;

        // 씬 로드 시 체력을 리셋합니다.
        if (healthManager != null) healthManager.ResetHealth();
    }

    private void SubscribeEvents()
    {
        // 이전 구독 해지 (혹시 모를 중복 구독 방지)
        if (InputManager.Instance != null) InputManager.Instance.OnWordTyped -= OnWordTypedHandler;
        // HealthManager는 씬마다 다르므로 FindObjectOfType으로 찾은 후 해지합니다.
        healthManager = FindObjectOfType<HealthManager>();
        if (healthManager != null) healthManager.OnGameOver -= OnGameOverHandler;

        // 새로운 씬의 오브젝트를 찾아서 연결하고 이벤트를 구독합니다.
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnWordTyped += OnWordTypedHandler;
        }

        if (healthManager != null)
        {
            healthManager.OnGameOver += OnGameOverHandler;
        }

        Debug.Log("[GameManager] Events subscribed.");
    }

    private void OnWordTypedHandler()
    {
        if (gameOver) return;
        wordsTyped++;
        Debug.Log($"wordsTyped>>>>>>>>>>>>>>>>>>>>>>>>>>>> '{wordsTyped}'");

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
        Debug.Log("Game over handler called.");
        StartCoroutine(HandleGameOver());
    }

    private IEnumerator HandleStageClear()
    {
        gameOver = true;

        if (reWordSpawner != null) reWordSpawner.Pause();
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ClearAllWords();
            InputManager.Instance.inputField.interactable = false;
        }

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

        if (gameClearText != null) gameClearText.gameObject.SetActive(true);
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (pointsText != null) pointsText.text = $"포인트: {totalPoints}";
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        yield return StartCoroutine(PostPointsToDb());
    }

    private IEnumerator HandleGameOver()
    {
        if (reWordSpawner != null) reWordSpawner.Pause();
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ClearAllWords();
            InputManager.Instance.inputField.interactable = false;
        }

        if (gameClearText != null) gameClearText.gameObject.SetActive(false);
        if (gameOverText != null) gameOverText.gameObject.SetActive(true);
        if (pointsText != null) pointsText.text = $"포인트: {totalPoints}";
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

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