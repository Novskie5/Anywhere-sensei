using UnityEngine;
using UnityEngine.UI;

// パネル背景や区切り線にUIThemeの色と角丸画像を当てる
[RequireComponent(typeof(Image))]
public class ThemedImage : MonoBehaviour
{
    public enum Role
    {
        Surface, // 角丸の半透明パネル
        Divider, // ボタンのグループを分ける縦線
    }

    [SerializeField] private UITheme _theme;
    [SerializeField] private Role _role = Role.Surface;

    private void Awake()
    {
        Apply();
    }

    [ContextMenu("Apply Theme")]
    public void Apply()
    {
        if (_theme == null) return;
        Image image = GetComponent<Image>();
        if (_role == Role.Surface)
        {
            image.sprite = _theme.RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = _theme.Surface;
        }
        else
        {
            image.sprite = null;
            image.color = _theme.Divider;
        }
    }
}
