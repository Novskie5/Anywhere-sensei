using UnityEngine;

// Androidのノッチ・パンチホールやナビゲーションバーにUIが隠れないよう、このRectTransformをScreen.safeAreaに合わせる
// 画面いっぱいに広げたCanvas直下の空オブジェクトに付け、UIはその子に置く
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rect;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        Fit();
    }

    // 画面の回転などでセーフエリアが変わることがあるので毎フレーム確認する(変わったときだけ計算する)
    private void Update()
    {
        if (Screen.safeArea != _lastSafeArea || Screen.width != _lastScreenSize.x || Screen.height != _lastScreenSize.y)
        {
            Fit();
        }
    }

    private void Fit()
    {
        Rect safe = Screen.safeArea;
        _lastSafeArea = safe;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;

        _rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        _rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }
}
