using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    public InputManager inputManager;
    public HealthManager healthManager;
    public ReWordSpawner reWordSpawner;
    public GameObject gameOverPanel; // ✅ 게임 종료 UI 패널
    public TMP_Text gameOverText;    // ✅ 게임 오버 문구 텍스트
    public TMP_Text gameClearText;   // ✅ 게임 클리어 문구 텍스트
    public TMP_Text pointsText;      // ✅ 최종 포인트를 표시할 텍스트

    [Header("Point Settings")]
    public int wordsToClearGame = 15;
    public int pointsPerClear = 150;

    private int currentPoints = 0;
    private int wordsTyped = 0;
    private bool gameOver = false;

    void Start()
    {
        if (inputManager != null)
        {
            inputManager.OnWordTyped += OnWordTypedHandler;
        }
        if (healthManager != null)
        {
            healthManager.OnGameOver += OnGameOverHandler;
        }

        // 게임 시작 시 패널 비활성화
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.OnWordTyped -= OnWordTypedHandler;
        }
        if (healthManager != null)
        {
            healthManager.OnGameOver -= OnGameOverHandler;
        }
    }

    private void OnWordTypedHandler()
    {
        if (gameOver) return;
        wordsTyped++;

        Debug.Log($"단어 입력 성공! 누적 단어 수: {wordsTyped}");

        if (wordsTyped >= wordsToClearGame)
        {
            currentPoints = pointsPerClear;
            HandleGameEnd(true);
        }
    }

    private void OnGameOverHandler()
    {
        if (gameOver) return;

        currentPoints = 0;
        HandleGameEnd(false);
    }

    private void HandleGameEnd(bool isGameClear)
    {
        gameOver = true;

        // 게임 진행 관련 오브젝트 정지
        if (reWordSpawner != null)
        {
            reWordSpawner.Pause();
        }
        if (inputManager != null)
        {
            inputManager.ClearAllWords();
            inputManager.inputField.interactable = false;
        }

        // UI 표시
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // 텍스트 활성화/비활성화
            if (isGameClear)
            {
                if (gameClearText != null) gameClearText.gameObject.SetActive(true);
                if (gameOverText != null) gameOverText.gameObject.SetActive(false);
                Debug.Log($"게임 클리어! 최종 포인트: {currentPoints}");
            }
            else
            {
                if (gameClearText != null) gameClearText.gameObject.SetActive(false);
                if (gameOverText != null) gameOverText.gameObject.SetActive(true);
                Debug.Log($"게임 오버! 최종 포인트: {currentPoints}");
            }

            // 포인트 텍스트 업데이트
            if (pointsText != null)
            {
                pointsText.text = $"포인트: {currentPoints}";
            }
        }

        string userId = "your_user_id";
        StartCoroutine(PostPointsToDb(userId, currentPoints));
    }

    private IEnumerator PostPointsToDb(string userId, int points)
    {
        string url = $"http://localhost:9001/api/users/updatePoints/{userId}";
        string jsonData = JsonUtility.ToJson(new PointData { points = points });

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