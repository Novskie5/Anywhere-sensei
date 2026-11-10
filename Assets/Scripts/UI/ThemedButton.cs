using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// UIThemeの色でボタンを塗り分ける。押している間は少し縮めて「押した」手応えを出す
// 種類(Variant)は実行時にも変えられる(ミュート中は赤にする、再生中のジェスチャーは青にする、など)
[RequireComponent(typeof(Button), typeof(Image))]
public class ThemedButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum Variant
    {
        Primary,   // アクセント色で塗る。選択中・一番押してほしいボタン
        Secondary, // パネルより一段明るい色。普段のボタン
        Danger,    // 赤。止まっている・取り消し系
        Ghost,     // 背景なし。副次的な操作
    }

    private const float PressedScale = 0.96f;
    private const float ScaleSpeed = 20f;

    [SerializeField] private UITheme _theme;
    [SerializeField] private Variant _variant = Variant.Secondary;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private TMP_Text _icon; // 任意。Font Awesomeの1文字

    private Button _button;
    private Image _image;
    private bool _pressed;

    public Variant CurrentVariant => _variant;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image = GetComponent<Image>();
        Apply();
    }

    private void OnDisable()
    {
        _pressed = false;
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        float target = _pressed && _button.interactable ? PressedScale : 1f;
        float scale = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-ScaleSpeed * Time.unscaledDeltaTime));
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    public void SetVariant(Variant variant)
    {
        if (_variant == variant) return;
        _variant = variant;
        Apply();
    }

    [ContextMenu("Apply Theme")]
    public void Apply()
    {
        if (_theme == null) return;
        if (_button == null) _button = GetComponent<Button>();
        if (_image == null) _image = GetComponent<Image>();

        // ColorTintはImage.colorに掛け算されるので、Image側は白にしておき色はColorBlockだけで決める
        _image.sprite = _theme.RoundedSprite;
        _image.type = Image.Type.Sliced;
        _image.color = Color.white;
        Color fill = FillColor();
        ColorBlock colors = _button.colors;
        colors.normalColor = fill;
        colors.highlightedColor = Overlay(fill, Color.white, 0.08f);
        colors.pressedColor = Overlay(fill, Color.black, 0.18f);
        colors.selectedColor = fill; // クリック後に選択状態の色が残ると「押しっぱなし」に見えるので通常色と同じにする
        colors.disabledColor = new Color(fill.r, fill.g, fill.b, fill.a * 0.4f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        _button.colors = colors;
        _button.transition = Selectable.Transition.ColorTint;

        Color textColor = _variant == Variant.Ghost ? _theme.TextSecondary : _theme.TextPrimary;
        if (_label != null)
        {
            _label.font = _theme.SemiBoldFont;
            _label.color = textColor;
        }
        if (_icon != null)
        {
            _icon.font = _theme.IconFont;
            _icon.color = textColor;
        }
    }

    private Color FillColor()
    {
        switch (_variant)
        {
            case Variant.Primary: return _theme.Accent;
            case Variant.Danger: return _theme.Danger;
            case Variant.Ghost: return new Color(1f, 1f, 1f, 0f);
            default: return _theme.SurfaceRaised;
        }
    }

    // 色を少し明るく/暗くする。Ghost(透明)でも重ねた色がうっすら見えるよう、不透明度は重ねる量以上にする
    private static Color Overlay(Color baseColor, Color over, float amount)
    {
        Color c = Color.Lerp(baseColor, over, amount);
        c.a = Mathf.Max(baseColor.a, amount);
        return c;
    }

    public void OnPointerDown(PointerEventData eventData) => _pressed = true;
    public void OnPointerUp(PointerEventData eventData) => _pressed = false;
    public void OnPointerExit(PointerEventData eventData) => _pressed = false;
}
