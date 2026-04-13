using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Text;

[RequireComponent(typeof(BoxCollider))]
public class SquareBorderTrigger : MonoBehaviour
{
    [Header("Border Settings")]
    public float squareSize = 5f;
    public float borderHeight = 2f;
    public float lineWidth = 0.1f;
    public Color defaultBorderColor = new Color(1, 0.5f, 0);
    public Color enterBorderColor = Color.green;

    [Header("未进入区域时显示 (TMP文本 + 呼吸缩放)")]
    public Canvas canvas_Initial;
    public TextMeshProUGUI text_Initial;
    public AudioSource audio_Initial;
    public AudioClip clip_Initial;

    [Header("呼吸动画参数")]
    public float scaleMin = 0.95f;
    public float scaleMax = 1.05f;
    public float scaleSpeed = 2f;

    [Header("进入区域后显示 (Text文本)")]
    public Canvas canvas_Target;
    public Text text_Target;
    public AudioSource audio_Target;
    public AudioClip clip_Target;

    [Header("打字速度 & 音频同步")]
    public bool autoSyncWithAudio = true;
    public float manualPrintSpeed = 0.05f;

    private BoxCollider areaCollider;
    private LineRenderer[] borderLines;
    private bool isPlayerInArea = false;
    private string targetFullText;
    private float actualPrintSpeed;

    void Start()
    {
        SetupTriggerArea();
        GeneratePerfectSquareBorder();

        if (canvas_Initial != null)
            canvas_Initial.gameObject.SetActive(true);

        if (canvas_Target != null)
            canvas_Target.gameObject.SetActive(false);

        if (text_Target != null)
        {
            targetFullText = text_Target.text;
            text_Target.text = "";
        }

        // 初始音频循环播放
        PlayInitialAudioLoop();

        // 开始呼吸动画
        StartCoroutine(TextBreathScale());

        borderLines = GetComponentsInChildren<LineRenderer>();
    }

    // 文本呼吸缩放效果（一直循环，直到隐藏）
    IEnumerator TextBreathScale()
    {
        if (text_Initial == null) yield break;

        while (canvas_Initial.gameObject.activeSelf)
        {
            // 放大
            for (float t = 0; t < 1; t += Time.deltaTime * scaleSpeed)
            {
                float scale = Mathf.Lerp(scaleMin, scaleMax, t);
                text_Initial.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            // 缩小
            for (float t = 0; t < 1; t += Time.deltaTime * scaleSpeed)
            {
                float scale = Mathf.Lerp(scaleMax, scaleMin, t);
                text_Initial.transform.localScale = Vector3.one * scale;
                yield return null;
            }
        }
    }

    // 初始音频循环播放
    void PlayInitialAudioLoop()
    {
        if (audio_Initial != null && clip_Initial != null)
        {
            audio_Initial.clip = clip_Initial;
            audio_Initial.loop = true;  // 开启循环
            audio_Initial.Play();
        }
    }

    void SetupTriggerArea()
    {
        areaCollider = GetComponent<BoxCollider>();
        areaCollider.isTrigger = true;
        areaCollider.center = Vector3.up * borderHeight / 2f;
        areaCollider.size = new Vector3(squareSize, borderHeight, squareSize);
    }

    void GeneratePerfectSquareBorder()
    {
        foreach (Transform child in transform)
        {
            if (child.name == "BorderLine")
                Destroy(child.gameObject);
        }

        float half = squareSize / 2f;

        Vector3[][] edges = new Vector3[][]
        {
            new[] { new Vector3(-half, 0, -half), new Vector3(half, 0, -half) },
            new[] { new Vector3(half, 0, -half), new Vector3(half, 0, half) },
            new[] { new Vector3(half, 0, half), new Vector3(-half, 0, half) },
            new[] { new Vector3(-half, 0, half), new Vector3(-half, 0, -half) },
        };

        foreach (var edge in edges)
        {
            GameObject line = new GameObject("BorderLine");
            line.transform.parent = transform;
            line.transform.localPosition = Vector3.zero;

            LineRenderer lr = line.AddComponent<LineRenderer>();
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = defaultBorderColor;
            lr.endColor = defaultBorderColor;

            lr.SetPosition(0, edge[0] + Vector3.up * borderHeight - Vector3.up * 1.82f);
            lr.SetPosition(1, edge[1] + Vector3.up * borderHeight - Vector3.up * 1.82f);
            lr.useWorldSpace = false;
            lr.numCapVertices = 8;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPlayerInArea)
        {
            isPlayerInArea = true;

            // 停止循环播放的初始音频
            if (audio_Initial != null)
            {
                audio_Initial.Stop();
                audio_Initial.loop = false;
            }

            if (canvas_Initial != null)
                canvas_Initial.gameObject.SetActive(false);

            if (canvas_Target != null)
                canvas_Target.gameObject.SetActive(true);

            SetBorderColor(enterBorderColor);
            StartCoroutine(PrintTargetText());
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInArea = false;
            StopAllCoroutines();

            if (audio_Target != null)
                audio_Target.Stop();

            if (canvas_Target != null)
                canvas_Target.gameObject.SetActive(false);

            if (text_Target != null)
                text_Target.text = "";

            SetBorderColor(defaultBorderColor);
        }
    }

    IEnumerator PrintTargetText()
    {
        StringBuilder sb = new StringBuilder();
        int dotCount = 0;
        int totalLen = targetFullText.Length;

        if (autoSyncWithAudio && clip_Target != null)
        {
            actualPrintSpeed = clip_Target.length / totalLen;
        }
        else
        {
            actualPrintSpeed = manualPrintSpeed;
        }

        if (audio_Target != null && clip_Target != null)
        {
            audio_Target.clip = clip_Target;
            audio_Target.loop = false;
            audio_Target.Play();
        }

        for (int i = 0; i < totalLen; i++)
        {
            if (!isPlayerInArea) yield break;

            char c = targetFullText[i];
            sb.Append(c);
            text_Target.text = sb.ToString();

            if (c == '。')
            {
                dotCount++;
                if (dotCount >= 2)
                {
                    sb.Clear();
                    dotCount = 0;
                }
            }

            yield return new WaitForSeconds(actualPrintSpeed);
        }

        while (audio_Target != null && audio_Target.isPlaying)
            yield return null;

        audio_Target?.Stop();
    }

    void SetBorderColor(Color color)
    {
        if (borderLines == null) return;
        foreach (var line in borderLines)
        {
            line.startColor = color;
            line.endColor = color;
        }
    }

    void OnValidate()
    {
        if (TryGetComponent(out BoxCollider col))
        {
            col.center = Vector3.up * borderHeight / 2f;
            col.size = new Vector3(squareSize, borderHeight, squareSize);
        }
    }
}