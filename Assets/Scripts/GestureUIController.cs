using UnityEngine;

// UIボタンのOnClickから叩く受け皿。
// 先生自身のアバターはPhotonNetwork.Instantiate後でないと実体が無くシーン上で参照できないため、
// NetworkManager.OnJoinedRoomからSetTargetで後から差し込んでもらう (FaceSync/FaceLandmarkerRunnerと同じパターン)
public class GestureUIController : MonoBehaviour
{
    private AnimatorController _target;

    public void SetTarget(AnimatorController target)
    {
        _target = target;
    }

    public void PlayGesture(int gestureIndex)
    {
        if (_target == null) return;
        _target.PlayGesture(gestureIndex);
    }

    public void ReturnToIdle()
    {
        if (_target == null) return;
        _target.ReturnToIdle();
    }
}
