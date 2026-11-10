using UnityEngine;

// UIボタンのOnClickから叩く受け皿。
// 先生自身のアバターはPhotonNetwork.Instantiate後でないと実体が無くシーン上で参照できないため、
// NetworkManager.OnJoinedRoomからSetTargetで後から差し込んでもらう (FaceSync/FaceLandmarkerRunnerと同じパターン)
//
// 押したジェスチャーのボタンは再生が終わってIdleに戻るまでアクセント色にして、今どれが動いているか分かるようにする
public class GestureUIController : MonoBehaviour
{
    // トリガーからジェスチャーのステートへ遷移し終わるまではIsIdleがtrueのままなので、
    // 「一度Idle以外になってから戻った」で終了と判定する。遷移が始まらなかったときの保険の待ち時間
    private const float StartTimeout = 2f;

    [SerializeField] private ThemedButton[] _gestureButtons; // gestureIndexと同じ並び。任意

    private AnimatorController _target;
    private int _activeGesture = -1;
    private bool _gestureStarted;
    private float _pressedTime;

    public void SetTarget(AnimatorController target)
    {
        _target = target;
    }

    public void PlayGesture(int gestureIndex)
    {
        if (_target == null) return;
        _target.PlayGesture(gestureIndex);

        _activeGesture = gestureIndex;
        _gestureStarted = false;
        _pressedTime = Time.time;
        RefreshButtons();
    }

    public void ReturnToIdle()
    {
        if (_target == null) return;
        _target.ReturnToIdle();
        ClearActive();
    }

    private void Update()
    {
        if (_activeGesture < 0 || _target == null) return;

        if (!_target.IsIdle)
        {
            _gestureStarted = true;
        }
        else if (_gestureStarted || Time.time - _pressedTime > StartTimeout)
        {
            ClearActive();
        }
    }

    private void ClearActive()
    {
        _activeGesture = -1;
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (_gestureButtons == null) return;
        for (int i = 0; i < _gestureButtons.Length; i++)
        {
            if (_gestureButtons[i] == null) continue;
            _gestureButtons[i].SetVariant(i == _activeGesture ? ThemedButton.Variant.Primary : ThemedButton.Variant.Secondary);
        }
    }
}
