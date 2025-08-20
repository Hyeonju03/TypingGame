using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class HealthManager : MonoBehaviour
{
    [Header("UI Refs")]
    public List<Image> heartImages;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    [Header("Settings")]
    public int maxHealth = 3;
    public float vibrationDuration = 0.2f;
    public float vibrationMagnitude = 5f;

    private int currentHealth;
    public event Action OnGameOver; // ✅ 게임 오버 이벤트

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHeartsUI();
    }

    public void TakeDamage()
    {
        if (currentHealth <= 0) return;

        currentHealth--;
        UpdateHeartsUI();

        if (currentHealth < heartImages.Count)
        {
            StartCoroutine(VibrateHeart(heartImages[currentHealth].rectTransform));
        }

        if (currentHealth <= 0)
        {
            Debug.Log("게임 오버!");
            OnGameOver?.Invoke(); // ✅ GameManager에게 게임 오버를 알림
        }
    }

    private void UpdateHeartsUI()
    {
        for (int i = 0; i < heartImages.Count; i++)
        {
            if (i < currentHealth)
            {
                heartImages[i].sprite = fullHeartSprite;
            }
            else
            {
                heartImages[i].sprite = emptyHeartSprite;
            }
        }
    }

    private IEnumerator VibrateHeart(RectTransform heartRect)
    {
        Vector3 originalPos = heartRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < vibrationDuration)
        {
            float xOffset = UnityEngine.Random.Range(-vibrationMagnitude, vibrationMagnitude);
            float yOffset = UnityEngine.Random.Range(-vibrationMagnitude, vibrationMagnitude);
            heartRect.anchoredPosition = new Vector3(originalPos.x + xOffset, originalPos.y + yOffset, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        heartRect.anchoredPosition = originalPos;
    }

    // ✅ GameManager에서 호출할 체력 초기화 함수
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHeartsUI();
    }
}