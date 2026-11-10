using TMPro;
using UnityEngine;

// ボタン以外の文字(接続状態・操作の案内など)にUIThemeのフォントと色を当てる
// ボタンの文字はThemedButtonが状態に合わせて色を変えるので、こちらは付けない
[RequireComponent(typeof(TMP_Text))]
public class ThemedText : MonoBehaviour
{
    public enum Role
    {
        Label,   // SemiBold・白。短い見出しや状態
        Body,    // Regular・白。案内文
        Caption, // Regular・灰色。補足
    }

    [SerializeField] private UITheme _theme;
    [SerializeField] private Role _role = Role.Body;

    private void Awake()
    {
        Apply();
    }

    [ContextMenu("Apply Theme")]
    public void Apply()
    {
        if (_theme == null) return;
        TMP_Text text = GetComponent<TMP_Text>();
        text.font = _role == Role.Label ? _theme.SemiBoldFont : _theme.RegularFont;
        text.color = _role == Role.Caption ? _theme.TextSecondary : _theme.TextPrimary;
    }
}
