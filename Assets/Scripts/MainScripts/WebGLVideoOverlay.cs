using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

/// WebGL �ڵ� �ݺ� ���(��ư ����):
/// - ������ �ε� �� �ڵ����(ó���� ����: ������ ��å ���)
/// - ����ڰ� Ű/Ŭ��/��ġ�ϸ� ��� ���Ʈ
/// - VideoPlayer �� RenderTexture �� RawImage ���, ���� ��ɰ� �浹 ����
public class WebGLVideoOverlay : MonoBehaviour
{
    [Header("UI")]
    public RawImage targetImage;          // WebGL�� RawImage(��������)
    public RenderTexture targetRT;        // ��: TutorialvRT

    [Header("Source")]
    public string fileName = "tutorial.mp4";   // Assets/StreamingAssets/ ���� mp4
    public VideoClip editorPreviewClip;        // (����) ������ �̸������

    [Header("�ɼ�")]
    public bool showOnStart = true;            // ���� �� ���� ���̾� ���̱�
    public bool autoUnmuteOnFirstInput = true; // ù �Է¿� ���Ʈ

    private VideoPlayer vp;
    private AudioSource audioSrc;
    private bool unmuted = false;

    void Awake()
    {
        // ������Ʈ (������ ����)
        vp = GetComponent<VideoPlayer>(); if (!vp) vp = gameObject.AddComponent<VideoPlayer>();
        audioSrc = GetComponent<AudioSource>(); if (!audioSrc) audioSrc = gameObject.AddComponent<AudioSource>();

        vp.playOnAwake = false;
        audioSrc.playOnAwake = false;

        // ��� ���
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = targetRT;
        vp.waitForFirstFrame = true;
        vp.skipOnDrop = true;
        vp.isLooping = true; // �� �ݺ� ���

        // ����� �����
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.EnableAudioTrack(0, true);
        vp.SetTargetAudioSource(0, audioSrc);

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: URL ��� + �������� ����(�ڵ���� ��å ���)
        vp.source = VideoSource.Url;
        vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, fileName).Replace("\\", "/");
        audioSrc.mute = true;
        audioSrc.volume = 1f;
#else
        // ������/���ĵ���: VideoClip �켱, ������ URL
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
        // �������� ǥ��/��ǥ��
        if (targetImage) targetImage.gameObject.SetActive(showOnStart);

        // �غ� �� �ڵ����(����)
        vp.Prepare();
        while (!vp.isPrepared) yield return null;
        vp.Play();

#if UNITY_WEBGL && !UNITY_EDITOR
        // Safari ����ȭ�� �� ������ ���
        yield return null;
#endif
    }

    void Update()
    {
        // ù ����� �Է��� ������ ���Ʈ(������ �̹� ��� ��)
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
        yield return null; // �� ������ �� ���Ʈ(����� Safari ����)
        audioSrc.mute = false;
        audioSrc.volume = 1f;
    }

    // �ܺο��� ���� ���� �� ȣ��(�ɼ�)
    public void StopAndHide()
    {
        if (vp.isPlaying) vp.Stop();
        if (targetImage) targetImage.gameObject.SetActive(false);
    }
}
