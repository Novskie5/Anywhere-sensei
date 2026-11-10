using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// メニュー「AnywhereSensei > UI」から、UIThemeが使うアセット(角丸画像・TMPフォント・テーマ本体)を作るEditor拡張
// 画像はコードで描き、フォントはInter/Font AwesomeのTTFからSDFを焼くので、外部ツールやFont Asset Creatorの手作業が要らない
// 既にあるアセットは作り直さない(GUIDが変わるとシーンからの参照が切れるため)。作り直したいときは消してから実行する
public static class UIThemeAssetGenerator
{
    public const string ThemePath = "Assets/UI/UITheme.asset";
    private const string SpriteFolder = "Assets/UI/Sprites";
    private const string FontFolder = "Assets/UI/Fonts";

    // 英語表記のみなのでASCIIと、接続状態などで使う「·」「…」だけ焼く
    private const string ExtraCharacters = "·…";
    private const string IconCharacters = ""; // microphone / microphone-slash

    [MenuItem("AnywhereSensei/UI/1. Generate Theme Assets")]
    private static void GenerateMenu()
    {
        UITheme theme = Generate();
        Selection.activeObject = theme;
        Debug.Log($"[UI] テーマのアセットを {ThemePath} に用意した");
    }

    public static UITheme LoadOrGenerate()
    {
        UITheme theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        return theme != null ? theme : Generate();
    }

    public static UITheme Generate()
    {
        EnsureFolder(SpriteFolder);

        Sprite rounded = CreateSprite(SpriteFolder + "/RoundedRect.png", 128, UITheme.RoundedSpriteRadius, sliced: true);
        Sprite circle = CreateSprite(SpriteFolder + "/Circle.png", 64, 32f, sliced: false);

        string ascii = "";
        for (char c = ' '; c <= '~'; c++) ascii += c;
        TMP_FontAsset regular = CreateFontAsset(FontFolder + "/Inter-Regular.ttf", FontFolder + "/Inter-Regular SDF.asset", ascii + ExtraCharacters, 1024);
        TMP_FontAsset semiBold = CreateFontAsset(FontFolder + "/Inter-SemiBold.ttf", FontFolder + "/Inter-SemiBold SDF.asset", ascii + ExtraCharacters, 1024);
        TMP_FontAsset icon = CreateFontAsset(FontFolder + "/FontAwesome5-Solid.otf", FontFolder + "/FontAwesome5-Solid SDF.asset", IconCharacters, 256);

        UITheme theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<UITheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
        }
        theme.RoundedSprite = rounded;
        theme.CircleSprite = circle;
        theme.RegularFont = regular;
        theme.SemiBoldFont = semiBold;
        theme.IconFont = icon;
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        return theme;
    }

    // 白い角丸四角(radius = size/2 なら円)を描いてSpriteとして取り込む。色はImage.colorで付ける
    private static Sprite CreateSprite(string path, int size, float radius, bool sliced)
    {
        if (!File.Exists(path))
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 一番近い「角の円の中心」からの距離で縁をなめらかにする(1px分のアンチエイリアス)
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    Vector2 corner = new Vector2(Mathf.Clamp(p.x, radius, size - radius), Mathf.Clamp(p.y, radius, size - radius));
                    float alpha = Mathf.Clamp01(radius - Vector2.Distance(p, corner) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed; // 圧縮すると縁がガタつく。小さいので非圧縮で問題ない
            if (sliced) importer.spriteBorder = new Vector4(radius, radius, radius, radius);
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 必要な文字だけを焼いたStaticのSDFフォントを作る
    // Dynamicのままだと実行中に文字が足されるたびにアセットが書き換わってgitの差分になるので、焼いた後Staticにする
    private static TMP_FontAsset CreateFontAsset(string fontPath, string assetPath, string characters, int atlasSize)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null)
        {
            Debug.LogError($"[UI] フォントが見つからない: {fontPath}");
            return null;
        }

        const int samplingPointSize = 64;
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, samplingPointSize, samplingPointSize / 8, GlyphRenderMode.SDFAA,
            atlasSize, atlasSize, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);
        asset.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(asset, assetPath);

        // アトラスとマテリアルはメモリ上にしか無いので、フォントアセットのサブアセットとして一緒に保存する
        asset.atlasTexture.name = asset.name + " Atlas";
        AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
        asset.material.name = asset.name + " Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);

        if (!asset.TryAddCharacters(characters, out string missing))
        {
            Debug.LogWarning($"[UI] {asset.name} に入っていない文字がある: {missing}");
        }
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
}
