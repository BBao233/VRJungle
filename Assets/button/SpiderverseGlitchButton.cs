using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class SpiderverseGlitchButton : MonoBehaviour
{
    [Header("基础设置")]
    public float hoverScale = 1.1f;

    [Header("文字设置")]
    public TextMeshProUGUI mainText;
    public TextMeshProUGUI glitchLayer1;
    public TextMeshProUGUI glitchLayer2;

    [Header("特效")]
    public RawImage noiseImage;
    public Image sliceLine;

    [Header("噪声")]
    [Range(0.01f, 0.3f)] public float noiseDensity = 0.1f;
    [Range(0.1f, 1f)] public float noiseAlpha = 0.3f;
    public Color noiseColor = Color.white;

    private Button _button;
    private RectTransform _buttonRect;
    private RectTransform _noiseRect;

    private bool _alwaysOn = true;

    private float _timer;
    private float _glitchTimer1;
    private float _glitchTimer2;

    private float _sliceDelay = 0.1f;

    private Vector3 _originalScale;
    private Vector2 _originalPosition;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _buttonRect = GetComponent<RectTransform>();
        _noiseRect = noiseImage.GetComponent<RectTransform>();

        _originalScale = _buttonRect.localScale;
        _originalPosition = _buttonRect.anchoredPosition;

        _noiseRect.sizeDelta = _buttonRect.sizeDelta;

        glitchLayer1.gameObject.SetActive(true);
        glitchLayer2.gameObject.SetActive(true);
        noiseImage.gameObject.SetActive(true);
        sliceLine.gameObject.SetActive(true);

        GenerateNoise();
    }

    private void Update()
    {
        // ✅ 新增：永远运行
        if (!_alwaysOn) return;

        _timer += Time.deltaTime;

        AnimateButton();
        AnimateTextGlitch();
        AnimateNoise();
        AnimateSlice();
    }

    private void AnimateButton()
    {
        float t = _timer * 3.5f;
        float x = Mathf.Lerp(-3, 3, Mathf.PingPong(t * 0.7f, 1));
        float y = Mathf.Lerp(-2, 2, Mathf.PingPong(t * 0.5f, 1));
        float scale = Mathf.Lerp(1.08f, 1.12f, Mathf.PingPong(t * 0.3f, 1));

        _buttonRect.anchoredPosition = _originalPosition + new Vector2(x, y);
        _buttonRect.localScale = _originalScale * scale;
    }

    private void AnimateTextGlitch()
    {
        _glitchTimer1 += Time.deltaTime * 5f;
        _glitchTimer2 += Time.deltaTime * 4f;

        glitchLayer1.rectTransform.anchoredPosition = new Vector2(
            Mathf.Lerp(-2, 2, Mathf.PingPong(_glitchTimer1, 1)),
            Mathf.Lerp(-1, 1, Mathf.PingPong(_glitchTimer1, 1))
        );

        glitchLayer2.rectTransform.anchoredPosition = new Vector2(
            Mathf.Lerp(2, -2, Mathf.PingPong(_glitchTimer2, 1)),
            Mathf.Lerp(1, -1, Mathf.PingPong(_glitchTimer2, 1))
        );
    }

    private void AnimateNoise()
    {
        float ox = Mathf.Lerp(-0.2f, 0.2f, Mathf.PingPong(_timer * 20, 1));
        float oy = Mathf.Lerp(-0.2f, 0.2f, Mathf.PingPong(_timer * 18, 1));
        noiseImage.uvRect = new Rect(ox, oy, 1, 1);
    }

    private void AnimateSlice()
    {
        float t = Mathf.PingPong(_timer * 0.4f, 1);

        float h = _noiseRect.sizeDelta.y;
        float y = Mathf.Lerp(-h / 2 - 10, h / 2 + 10, t);

        sliceLine.rectTransform.anchoredPosition = new Vector2(0, y);

        sliceLine.color = (t > 0.48f && t < 0.52f)
            ? new Color(1, 1, 1, 0.6f)
            : Color.clear;
    }

    private void GenerateNoise()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = (Random.value < noiseDensity)
                ? noiseColor
                : Color.clear;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        noiseImage.texture = tex;
        noiseImage.color = new Color(1, 1, 1, noiseAlpha);
    }
}