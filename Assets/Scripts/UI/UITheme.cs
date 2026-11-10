using TMPro;
using UnityEngine;

// UIの配色・フォント・画像を1か所にまとめたデザイントークン
// ThemedButton/ThemedText/ThemedImageがここを参照するので、色やフォントを変えたいときはこのアセットだけ直せば全画面に反映される
// (余白や文字サイズは先生PC 1920x1080 と生徒スマホ 1080x1920 で違うので、画面を組み立てるUIBuilder側で持つ)
// アセット本体はメニューの AnywhereSensei > UI > 1. Generate Theme Assets で作る
[CreateAssetMenu(menuName = "AnywhereSensei/UI Theme", fileName = "UITheme")]
public class UITheme : ScriptableObject
{
    [Header("Colors")]
    public Color Surface = Rgb(0x121826, 0.85f);   // パネル背景。カメラ映像やアバターの上でも文字が読めるよう濃い半透明にする
    public Color SurfaceRaised = Rgb(0x2A3346);    // パネルの上に乗る副ボタン
    public Color Divider = Rgb(0xFFFFFF, 0.14f);
    public Color Accent = Rgb(0x4F8CFF);           // 主ボタン・選択中
    public Color Danger = Rgb(0xFF5A5F);           // ミュート中など「止まっている」状態
    public Color Success = Rgb(0x3DD68C);
    public Color Warning = Rgb(0xFFB547);
    public Color TextPrimary = Rgb(0xFFFFFF);
    public Color TextSecondary = Rgb(0xA7B0C0);

    [Header("Fonts")]
    public TMP_FontAsset RegularFont;
    public TMP_FontAsset SemiBoldFont;
    public TMP_FontAsset IconFont; // Font Awesome 5 Solid (マイクのアイコンだけ入れている)

    [Header("Sprites")]
    public Sprite RoundedSprite; // 9スライスの角丸。角の半径はImage.pixelsPerUnitMultiplierで画面ごとに変える
    public Sprite CircleSprite;

    // 角丸画像の角の半径(px)。UIBuilderが「画面上で半径何px」からpixelsPerUnitMultiplierを逆算するのに使う
    public const float RoundedSpriteRadius = 32f;

    // 0xRRGGBB から作る。ScriptableObjectのフィールド初期化子で使うのでUnityのAPI(ColorUtility)は避けて自前で計算する
    private static Color Rgb(int hex, float alpha = 1f)
    {
        return new Color(((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f, alpha);
    }
}
