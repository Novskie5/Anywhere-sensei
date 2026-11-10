using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// メニュー「AnywhereSensei > UI」から、先生(Main_Tracking)と生徒(Student_AR)の画面UIを組み立てるEditor拡張
// Canvasの中身を消してUIThemeに沿って作り直すので、何度実行しても同じ状態になる(デザインを直したらここを直して再実行する)
// ボタンのOnClickやVoiceMuteController等への参照もここで繋ぎ直し、最後にシーンを保存する
public static class UIBuilder
{
    private const string TeacherScenePath = "Assets/Scenes/Main_Tracking.unity";
    private const string StudentScenePath = "Assets/Scenes/Student_AR.unity";

    // 画面ごとの寸法。先生はPCのウィンドウ(基準1920x1080)をマウスで、生徒はスマホ縦持ち(基準幅1080)を指で操作する
    private struct Metrics
    {
        public float Margin;         // 画面端からの余白
        public float Gap;            // 並べたボタンの間隔
        public int DockPadding;      // パネルの内側の余白
        public float ButtonHeight;
        public float ButtonMinWidth;
        public float ButtonRadius;
        public int ButtonPaddingX;
        public float FontSize;
        public float IconSize;
        public float StatusHeight;
        public float StatusFontSize;
        public float DotSize;
    }

    private static readonly Metrics Teacher = new Metrics
    {
        Margin = 32, Gap = 8, DockPadding = 12,
        ButtonHeight = 64, ButtonMinWidth = 132, ButtonRadius = 16, ButtonPaddingX = 28, FontSize = 26, IconSize = 24,
        StatusHeight = 48, StatusFontSize = 22, DotSize = 12,
    };

    private static readonly Metrics Student = new Metrics
    {
        Margin = 40, Gap = 16, DockPadding = 0,
        ButtonHeight = 128, ButtonMinWidth = 360, ButtonRadius = 64, ButtonPaddingX = 48, FontSize = 40, IconSize = 40,
        StatusHeight = 72, StatusFontSize = 30, DotSize = 18,
    };

    private const float StudentHintFontSize = 34;

    [MenuItem("AnywhereSensei/UI/2. Build Teacher UI (Main_Tracking)")]
    private static void BuildTeacherUI()
    {
        if (!RequireScene(TeacherScenePath)) return;
        UITheme theme = UIThemeAssetGenerator.LoadOrGenerate();

        GestureUIController gestureUI = Object.FindObjectOfType<GestureUIController>();
        if (gestureUI == null)
        {
            EditorUtility.DisplayDialog("UI Builder", "GestureUIController がシーンに無い", "OK");
            return;
        }

        Transform canvas = PrepareCanvas("GestureCanvas", new Vector2(1920, 1080), 0.5f);
        Metrics m = Teacher;

        // 左上: 接続状態
        CreateStatusPill(canvas, theme, m, new Vector2(0, 1), new Vector2(m.Margin, -m.Margin));

        // 下中央: ジェスチャー。モーションの3つと、ポーズから抜けるIdle(控えめなGhost)を線で区切る
        RectTransform gestureDock = CreateDock(canvas, theme, m, "GestureDock");
        Place(gestureDock, new Vector2(0.5f, 0), new Vector2(0, m.Margin));

        string[] gestureLabels = { "Clap", "Bye", "Cute" }; // AnimatorController.gestureTriggersと同じ並び
        var gestureButtons = new ThemedButton[gestureLabels.Length];
        for (int i = 0; i < gestureLabels.Length; i++)
        {
            gestureButtons[i] = CreateButton(gestureDock, theme, m, "Btn_" + gestureLabels[i], gestureLabels[i], null, ThemedButton.Variant.Secondary);
            UnityEventTools.AddIntPersistentListener(gestureButtons[i].GetComponent<Button>().onClick, gestureUI.PlayGesture, i);
        }
        CreateDivider(gestureDock, theme, m.ButtonHeight * 0.6f);
        ThemedButton idle = CreateButton(gestureDock, theme, m, "Btn_Idle", "Idle", null, ThemedButton.Variant.Ghost);
        UnityEventTools.AddPersistentListener(idle.GetComponent<Button>().onClick, gestureUI.ReturnToIdle);
        SetArray(gestureUI, "_gestureButtons", gestureButtons);

        // 右下: マイク。ジェスチャーと役割が違うので別のパネルに分ける
        RectTransform micDock = CreateDock(canvas, theme, m, "MicDock");
        Place(micDock, new Vector2(1, 0), new Vector2(-m.Margin, m.Margin));
        CreateMuteButton(micDock, theme, m);

        SaveScene();
    }

    [MenuItem("AnywhereSensei/UI/3. Build Student UI (Student_AR)")]
    private static void BuildStudentUI()
    {
        if (!RequireScene(StudentScenePath)) return;
        UITheme theme = UIThemeAssetGenerator.LoadOrGenerate();
        ARAvatarPlacer placer = Object.FindObjectOfType<ARAvatarPlacer>();

        // 縦持ち専用なので横幅基準(match=0)にする。高さ基準が混ざると、縦長の端末ほど横幅が足りなくなって文字がはみ出す
        Transform canvas = PrepareCanvas("Canvas", new Vector2(1080, 1920), 0f);
        Metrics m = Student;

        RectTransform safeArea = CreateRect("SafeArea", canvas);
        safeArea.anchorMin = Vector2.zero;
        safeArea.anchorMax = Vector2.one;
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        safeArea.gameObject.AddComponent<SafeAreaFitter>();

        // 左上: 接続状態
        CreateStatusPill(safeArea, theme, m, new Vector2(0, 1), new Vector2(m.Margin, -m.Margin));

        // 上: 次にやる操作の案内(横幅いっぱい、文字量に合わせて高さが伸びる)
        RectTransform hint = CreateSurface(safeArea, theme, "PlacementHint", 32);
        hint.anchorMin = new Vector2(0, 1);
        hint.anchorMax = new Vector2(1, 1);
        hint.pivot = new Vector2(0.5f, 1);
        hint.offsetMin = new Vector2(m.Margin, 0);
        hint.offsetMax = new Vector2(-m.Margin, 0);
        hint.anchoredPosition = new Vector2(0, -(m.Margin + m.StatusHeight + m.Gap));
        var hintLayout = hint.gameObject.AddComponent<VerticalLayoutGroup>();
        hintLayout.padding = new RectOffset(36, 36, 24, 24);
        hintLayout.childAlignment = TextAnchor.MiddleCenter;
        ControlChildSize(hintLayout);
        hint.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        TextMeshProUGUI hintText = CreateText(hint, "Label", "Move your phone slowly to scan the floor", StudentHintFontSize);
        hintText.enableWordWrapping = true;
        Theme(hintText, theme, ThemedText.Role.Body);
        hint.gameObject.AddComponent<CanvasGroup>();
        PlacementHintView hintView = hint.gameObject.AddComponent<PlacementHintView>();
        SetRefs(hintView, ("_placer", placer), ("_label", hintText));

        // 下中央: マイク。親指で押しやすい大きさにする
        ThemedButton mute = CreateMuteButton(safeArea, theme, m);
        RectTransform muteRect = (RectTransform)mute.transform;
        Place(muteRect, new Vector2(0.5f, 0), new Vector2(0, m.Margin + 24));
        var fitter = mute.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        SaveScene();
    }

    // ---- 画面の部品 ----

    private static void CreateStatusPill(Transform parent, UITheme theme, Metrics m, Vector2 corner, Vector2 position)
    {
        RectTransform pill = CreateSurface(parent, theme, "ConnectionStatus", m.StatusHeight * 0.5f);
        Place(pill, corner, position);

        var layout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
        int padX = Mathf.RoundToInt(m.StatusHeight * 0.4f);
        layout.padding = new RectOffset(padX, padX + 4, 0, 0);
        layout.spacing = m.DotSize;
        layout.childAlignment = TextAnchor.MiddleLeft;
        ControlChildSize(layout);
        var pillSize = pill.gameObject.AddComponent<LayoutElement>(); // 高さは文字に合わせず固定する(幅は文字に合わせる)
        pillSize.layoutPriority = 0;
        pillSize.preferredHeight = m.StatusHeight;
        var fitter = pill.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform dotRect = CreateRect("Dot", pill);
        Image dot = dotRect.gameObject.AddComponent<Image>();
        dot.sprite = theme.CircleSprite;
        dot.color = theme.Warning;
        dot.raycastTarget = false;
        var dotSize = dotRect.gameObject.AddComponent<LayoutElement>();
        dotSize.minWidth = dotSize.preferredWidth = m.DotSize;
        dotSize.minHeight = dotSize.preferredHeight = m.DotSize;

        TextMeshProUGUI label = CreateText(pill, "Label", "Connecting…", m.StatusFontSize);
        Theme(label, theme, ThemedText.Role.Label);

        ConnectionStatusView view = pill.gameObject.AddComponent<ConnectionStatusView>();
        SetRefs(view, ("_theme", theme), ("_label", label), ("_dot", dot));
    }

    // マイクのボタン。VoiceMuteControllerは先生だとCanvasの外(GestureUIControllerと同じオブジェクト)に既にあるのでそれを使い、
    // 無ければ(生徒は作り直しで一緒に消えるので)ボタン自身に付ける
    private static ThemedButton CreateMuteButton(Transform parent, UITheme theme, Metrics m)
    {
        ThemedButton button = CreateButton(parent, theme, m, "Btn_Mute", "Mic On", "", ThemedButton.Variant.Secondary);
        VoiceMuteController mute = Object.FindObjectOfType<VoiceMuteController>();
        if (mute == null) mute = button.gameObject.AddComponent<VoiceMuteController>();

        TMP_Text label = button.transform.Find("Label").GetComponent<TMP_Text>();
        TMP_Text icon = button.transform.Find("Icon").GetComponent<TMP_Text>();
        SetRefs(mute, ("_label", label), ("_icon", icon), ("_button", button));
        UnityEventTools.AddPersistentListener(button.GetComponent<Button>().onClick, mute.ToggleMute);
        return button;
    }

    private static RectTransform CreateDock(Transform parent, UITheme theme, Metrics m, string name)
    {
        RectTransform dock = CreateSurface(parent, theme, name, m.ButtonRadius + m.DockPadding);
        var layout = dock.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(m.DockPadding, m.DockPadding, m.DockPadding, m.DockPadding);
        layout.spacing = m.Gap;
        layout.childAlignment = TextAnchor.MiddleCenter;
        ControlChildSize(layout);
        var fitter = dock.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return dock;
    }

    private static ThemedButton CreateButton(Transform parent, UITheme theme, Metrics m, string name, string label, string icon, ThemedButton.Variant variant)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>(); // 画像と色はThemedButtonが当てる
        image.pixelsPerUnitMultiplier = UITheme.RoundedSpriteRadius / m.ButtonRadius;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        // キーボード/パッドの選択移動は使わない(クリック後にフォーカス枠が残る原因になる)
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(m.ButtonPaddingX, m.ButtonPaddingX, 0, 0);
        layout.spacing = m.IconSize * 0.5f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        ControlChildSize(layout);
        // 優先度をLayoutGroupと同じ0にして「最小幅」と「文字が収まる幅」の大きい方を取らせる
        // (既定の1だとLayoutElementの値だけが使われ、長いラベルがはみ出す)
        var size = rect.gameObject.AddComponent<LayoutElement>();
        size.layoutPriority = 0;
        size.minWidth = m.ButtonMinWidth;
        size.minHeight = size.preferredHeight = m.ButtonHeight;

        TextMeshProUGUI iconText = icon != null ? CreateText(rect, "Icon", icon, m.IconSize) : null;
        TextMeshProUGUI labelText = CreateText(rect, "Label", label, m.FontSize);

        ThemedButton themed = rect.gameObject.AddComponent<ThemedButton>();
        var so = new SerializedObject(themed);
        so.FindProperty("_theme").objectReferenceValue = theme;
        so.FindProperty("_variant").enumValueIndex = (int)variant;
        so.FindProperty("_label").objectReferenceValue = labelText;
        so.FindProperty("_icon").objectReferenceValue = iconText;
        so.ApplyModifiedPropertiesWithoutUndo();
        themed.Apply();

        // ColorBlockを書き換えただけではシーンビューの色が白のままなので、有効化し直して色を反映させる
        button.enabled = false;
        button.enabled = true;
        return themed;
    }

    private static void CreateDivider(Transform parent, UITheme theme, float height)
    {
        RectTransform rect = CreateRect("Divider", parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        var size = rect.gameObject.AddComponent<LayoutElement>();
        size.minWidth = size.preferredWidth = 2;
        size.preferredHeight = height;
        ThemedImage themed = rect.gameObject.AddComponent<ThemedImage>();
        SetRefs(themed, ("_theme", theme));
        SetEnum(themed, "_role", (int)ThemedImage.Role.Divider);
        themed.Apply();
    }

    // 角丸の半透明パネル。下にあるARの画面やボタン以外へのタップを邪魔しないよう、パネル自体はクリックを受けない
    private static RectTransform CreateSurface(Transform parent, UITheme theme, string name, float radius)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.pixelsPerUnitMultiplier = UITheme.RoundedSpriteRadius / radius;
        ThemedImage themed = rect.gameObject.AddComponent<ThemedImage>();
        SetRefs(themed, ("_theme", theme));
        SetEnum(themed, "_role", (int)ThemedImage.Role.Surface);
        themed.Apply();
        return rect;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize)
    {
        RectTransform rect = CreateRect(name, parent);
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false; // クリックはボタン本体で受ける
        return tmp;
    }

    private static void Theme(TMP_Text text, UITheme theme, ThemedText.Role role)
    {
        ThemedText themed = text.gameObject.AddComponent<ThemedText>();
        SetRefs(themed, ("_theme", theme));
        SetEnum(themed, "_role", (int)role);
        themed.Apply();
    }

    // ---- シーン・レイアウトの下回り ----

    private static bool RequireScene(string path)
    {
        if (SceneManager.GetActiveScene().path == path) return true;
        EditorUtility.DisplayDialog("UI Builder", $"{path} を開いてから実行してください", "OK");
        return false;
    }

    // Canvasの中身を全部消して、基準解像度を揃える
    private static Transform PrepareCanvas(string name, Vector2 referenceResolution, float match)
    {
        GameObject canvasObject = GameObject.Find(name);
        if (canvasObject == null || canvasObject.GetComponent<Canvas>() == null)
        {
            throw new System.InvalidOperationException($"Canvas '{name}' がシーンに無い");
        }

        Transform canvas = canvasObject.transform;
        for (int i = canvas.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(canvas.GetChild(i).gameObject);
        }

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = match;
        return canvas;
    }

    private static void SaveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[UI] {scene.name} のUIを組み立てて保存した");
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    // 画面の角や辺の中央に、その位置を基準(pivot)にして置く
    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
    }

    // 子の大きさは子自身の推奨サイズに任せ、引き伸ばさない
    private static void ControlChildSize(HorizontalOrVerticalLayoutGroup layout)
    {
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    // privateな[SerializeField]へ参照を入れる
    private static void SetRefs(Object target, params (string field, Object value)[] refs)
    {
        var so = new SerializedObject(target);
        foreach ((string field, Object value) in refs)
        {
            so.FindProperty(field).objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(Object target, string field, int value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).enumValueIndex = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty array = so.FindProperty(field);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
