using TMPro;
using UnityEngine;

// Student_AR用: 今やるべき操作を画面上部に1行で出す
// 初めて使う人は「床を映す→輪をタップ→ドラッグ/ピンチで調整」の流れが分からないので、ARAvatarPlacerの状態から段階を判断する
// 置き終わった後の操作説明はしばらく出したら消す(ずっと出ているとARの画面を隠して邪魔なので)
[RequireComponent(typeof(CanvasGroup))]
public class PlacementHintView : MonoBehaviour
{
    private const float ManipulationHintDuration = 6f;
    private const float FadeSpeed = 6f;

    [SerializeField] private ARAvatarPlacer _placer;
    [SerializeField] private TMP_Text _label;

    private CanvasGroup _group;
    private float _placedTime = -1f;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.blocksRaycasts = false; // 案内の上をタップしてもARの配置操作が効くようにする
        _group.interactable = false;
    }

    private void Update()
    {
        string hint = CurrentHint();
        bool visible = hint != null;
        if (visible) _label.text = hint;

        float target = visible ? 1f : 0f;
        _group.alpha = Mathf.MoveTowards(_group.alpha, target, FadeSpeed * Time.unscaledDeltaTime);
    }

    private string CurrentHint()
    {
        if (_placer == null) return null;

        if (!_placer.IsPlaced)
        {
            _placedTime = -1f;
            return _placer.HasFloorCandidate
                ? "Tap to place your teacher on the ring"
                : "Move your phone slowly to scan the floor";
        }

        if (!_placer.HasHostAvatar) return "Waiting for the teacher to join…";

        // 先生が現れてから数秒だけ操作方法を出す
        if (_placedTime < 0f) _placedTime = Time.time;
        return Time.time - _placedTime < ManipulationHintDuration
            ? "Drag to move  ·  Pinch to resize  ·  Double-tap to reset"
            : null;
    }
}
