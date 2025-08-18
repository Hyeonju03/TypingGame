
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text.RegularExpressions;

[System.Serializable]
public class OneDTO
{
    public string type;
    public string text;
    public string mean;
}

public enum SpawnMode
{
    Mixed,
    WordOnly,
    SentenceOnly
}

public class ReWordSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private FallingWordMaker maker;

    [Header("Spawn Loop")]
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private bool preventImmediateDup = true;

    [Header("Spawn Mode")]
    [SerializeField] private SpawnMode currentMode = SpawnMode.Mixed;

    [Header("API")]
    [SerializeField] private string bothUrl = "http://localhost:9001/api/learn/both";
    [SerializeField] private string oneUrl = "http://localhost:9001/api/learn/one?kind=";
    [SerializeField] private float retryDelay = 0.6f;

    private Coroutine loop;
    private string lastWord;


    private void Awake()
    {
        if (maker == null)
        {
            maker = FindObjectOfType<FallingWordMaker>();
            Debug.LogWarning($"[ReWordSpawner] maker가 비어 있어 FindObjectOfType로 주입: {maker}");
        }
    }

    private void Start()
    {
        loop = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            yield return wait;

            Debug.Log($"[ReWordSpawner] 루프 틱: 스폰 모드 {currentMode} 시작");
            string token = null;

            switch (currentMode)
            {
                case SpawnMode.WordOnly:
                    yield return StartCoroutine(GetOne("word", r => token = r));
                    break;
                case SpawnMode.SentenceOnly:
                    yield return StartCoroutine(GetOne("sentence", r => token = r));
                    break;
                case SpawnMode.Mixed:
                    BothDTO dto = null;
                    yield return StartCoroutine(GetBoth(r => dto = r));
                    if (dto != null)
                    {
                        string word = (dto.vocabulary ?? "").Trim();
                        string sent = (dto.munjang ?? "").Trim();
                        bool useSentence = !string.IsNullOrWhiteSpace(sent) && UnityEngine.Random.value < 0.5f;
                        token = useSentence ? sent : word;
                    }
                    break;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                Debug.LogWarning("[ReWordSpawner] 토큰을 가져오지 못했습니다. (네트워크/파싱 실패 또는 빈 응답)");
                continue;
            }

            if (preventImmediateDup && token == lastWord)
            {
                Debug.Log($"[ReWordSpawner] 직전 텍스트 '{token}'와 동일 → 재시도");
                continue;
            }
            lastWord = token;

            if (maker == null)
            {
                Debug.LogError("[ReWordSpawner] maker가 null이라 스폰 불가");
                continue;
            }

            Debug.Log($"[ReWordSpawner] MakeFallingWord 호출: 토큰='{token}'");
            maker.MakeFallingWord(token);
        }
    }

    private IEnumerator GetBoth(System.Action<BothDTO> done)
    {
        using (var req = UnityWebRequest.Get(bothUrl))
        {
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                yield return new WaitForSeconds(retryDelay);
                done(null); yield break;
            }

            string raw = (req.downloadHandler.text ?? "")
                         .TrimStart('\uFEFF', '\u200B', '\u0000').Trim();

            BothDTO dto = null;
            try { dto = JsonUtility.FromJson<BothDTO>(raw); }
            catch (System.Exception ex)
            {
                Debug.LogError("[ReWordSpawner] JSON 파싱 실패: " + ex.Message);
            }

            done(dto);
        }
    }

    private IEnumerator GetOne(string kind, System.Action<string> done)
    {
        string fullUrl = oneUrl + kind;
        using (var req = UnityWebRequest.Get(fullUrl))
        {
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ReWordSpawner] HTTP 실패 ({fullUrl}): " + req.error);
                yield return new WaitForSeconds(retryDelay);
                done(null); yield break;
            }

            string raw = (req.downloadHandler.text ?? "")
                .TrimStart('\uFEFF', '\u200B', '\u0000').Trim();

            try
            {
                // JsonUtility.FromJson을 사용해 OneDTO로 변환
                OneDTO dto = JsonUtility.FromJson<OneDTO>(raw);
                if (dto != null && !string.IsNullOrEmpty(dto.text))
                {
                    done(dto.text);
                    yield break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ReWordSpawner] JSON 파싱 실패 ({fullUrl}): " + ex.Message);
            }

            done(null);
        }
    }

    public void Pause() { if (loop != null) { StopCoroutine(loop); loop = null; Debug.Log("[ReWordSpawner] Pause"); } }
    public void Resume() { if (loop == null) { loop = StartCoroutine(SpawnLoop()); Debug.Log("[ReWordSpawner] Resume"); } }
}
