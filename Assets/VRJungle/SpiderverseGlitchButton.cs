using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class SpiderverseGlitchButton : MonoBehaviour
{
    [Header("基础设置")]
    public float hoverScale = 1.1f;

    [Header("文字设置")]
    public TextMeshProUGUI mainText;
    public TextMeshProUGUI glitchLayer1;
    public TextMeshProUGUI glitchLayer2;

    private Button _button;
    private RectTransform _buttonRect;

    private bool _alwaysOn = true;

    private float _timer;
    private float _glitchTimer1;
    private float _glitchTimer2;

    private Vector3 _originalScale;
    private Vector2 _originalPosition;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _buttonRect = GetComponent<RectTransform>();

        _originalScale = _buttonRect.localScale;
        _originalPosition = _buttonRect.anchoredPosition;

        // 常驻开启 glitch 层
        glitchLayer1.gameObject.SetActive(true);
        glitchLayer2.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!_alwaysOn) return;

        _timer += Time.deltaTime;

        AnimateButton();
        AnimateTextGlitch();
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
}