using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

/// WebGL 자동 반복 재생(버튼 없음):
/// - 페이지 로드 후 자동재생(처음엔 무음: 브라우저 정책 통과)
/// - 사용자가 키/클릭/터치하면 즉시 언뮤트
/// - VideoPlayer → RenderTexture → RawImage 경로, 기존 기능과 충돌 없음
public class WebGLVideoOverlay : MonoBehaviour
{
    [Header("UI")]
    public RawImage targetImage;          // WebGL용 RawImage(오버레이)
    public RenderTexture targetRT;        // 예: TutorialvRT

    [Header("Source")]
    public string fileName = "tutorial.mp4";   // Assets/StreamingAssets/ 안의 mp4
    public VideoClip editorPreviewClip;        // (선택) 에디터 미리보기용

    [Header("옵션")]
    public bool showOnStart = true;            // 시작 시 영상 레이어 보이기
    public bool autoUnmuteOnFirstInput = true; // 첫 입력에 언뮤트

    private VideoPlayer vp;
    private AudioSource audioSrc;
    private bool unmuted = false;

    void Awake()
    {
        // 컴포넌트 (있으면 재사용)
        vp = GetComponent<VideoPlayer>(); if (!vp) vp = gameObject.AddComponent<VideoPlayer>();
        audioSrc = GetComponent<AudioSource>(); if (!audioSrc) audioSrc = gameObject.AddComponent<AudioSource>();

        vp.playOnAwake = false;
        audioSrc.playOnAwake = false;

        // 출력 경로
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = targetRT;
        vp.waitForFirstFrame = true;
        vp.skipOnDrop = true;
        vp.isLooping = true; // ★ 반복 재생

        // 오디오 라우팅
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.EnableAudioTrack(0, true);
        vp.SetTargetAudioSource(0, audioSrc);

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: URL 사용 + 무음으로 시작(자동재생 정책 통과)
        vp.source = VideoSource.Url;
        vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, fileName).Replace("\\", "/");
        audioSrc.mute = true;
        audioSrc.volume = 1f;
#else
        // 에디터/스탠드얼론: VideoClip 우선, 없으면 URL
        if (editorPreviewClip)
        {
            vp.source = VideoSource.VideoClip;
            vp.clip = editorPreviewClip;
        }
        else
        {
            vp.source = VideoSource.Url;
            vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, fileName).Replace("\\", "/");
        }
#endif
        vp.errorReceived += (v, msg) => Debug.LogError("[VideoPlayer] " + msg);
        vp.prepareCompleted += _ => { if (targetImage) targetImage.texture = targetRT; };
    }

    IEnumerator Start()
    {
        // 오버레이 표시/비표시
        if (targetImage) targetImage.gameObject.SetActive(showOnStart);

        // 준비 후 자동재생(무음)
        vp.Prepare();
        while (!vp.isPrepared) yield return null;
        vp.Play();

#if UNITY_WEBGL && !UNITY_EDITOR
        // Safari 안정화용 한 프레임 대기
        yield return null;
#endif
    }

    void Update()
    {
        // 첫 사용자 입력이 들어오면 언뮤트(영상은 이미 재생 중)
        if (!unmuted && autoUnmuteOnFirstInput)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                StartCoroutine(CoUnmuteNextFrame());
                unmuted = true;
            }
        }
    }

    IEnumerator CoUnmuteNextFrame()
    {
        yield return null; // 한 프레임 뒤 언뮤트(모바일 Safari 대응)
        audioSrc.mute = false;
        audioSrc.volume = 1f;
    }

    // 외부에서 끄고 싶을 때 호출(옵션)
    public void StopAndHide()
    {
        if (vp.isPlaying) vp.Stop();
        if (targetImage) targetImage.gameObject.SetActive(false);
    }
}
