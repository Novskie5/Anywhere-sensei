using UnityEngine;
using Photon.Pun;


[System.Serializable]
public class PoseData
{
    public string name;
    public Vector3 upperArmL;
    public Vector3 upperArmR;
    public Vector3 lowerArmL;
    public Vector3 lowerArmR;
}
public class PoseController : MonoBehaviourPun
{
    [SerializeField] private Transform _chest;
    private Quaternion _chestDefaultRot;

    [SerializeField] private Transform _upperArmL;
    [SerializeField] private Transform _upperArmR;
    [SerializeField] private Transform _lowerArmL;
    [SerializeField] private Transform _lowerArmR;

    [SerializeField] private PoseData[] _poses;

    // Editor上でボーンを手で回してポーズを作るときに使う：
    // このインデックスに現在のボーン角度を記録する（Inspectorで右クリック→Capture Current Pose）
    [SerializeField] private int _captureSlotIndex;

    private void Start()
    {
        if (_chest != null)
            _chestDefaultRot = _chest.localRotation;

        if (_poses != null && _poses.Length > 0)
        {
            SetPose(0);
        }
    }

    private void Update()
    {
        if (_chest == null) return;
        float breath = Mathf.Sin(Time.time * 1.2f) * 0.8f;
        _chest.localRotation = _chestDefaultRot * Quaternion.Euler(breath, 0, 0);
    }

    // UIボタンのOnClick()から呼ぶ想定。自分のアバターのときだけ全員に同期する。
    public void RequestSetPose(int index)
    {
        if (!photonView.IsMine) return;
        photonView.RPC(nameof(ApplyPoseRpc), RpcTarget.All, index);
    }

    [PunRPC]
    private void ApplyPoseRpc(int index)
    {
        SetPose(index);
    }

    public void SetPose(int index)
    {
        if (_poses == null || index < 0 || index >= _poses.Length) return;

        var p = _poses[index];
        _upperArmL.localRotation = Quaternion.Euler(p.upperArmL);
        _upperArmR.localRotation = Quaternion.Euler(p.upperArmR);
        _lowerArmL.localRotation = Quaternion.Euler(p.lowerArmL);
        _lowerArmR.localRotation = Quaternion.Euler(p.lowerArmR);
    }

    [ContextMenu("Capture Current Pose Into Slot")]
    private void CaptureCurrentPose()
    {
        if (_poses == null || _captureSlotIndex < 0 || _captureSlotIndex >= _poses.Length)
        {
            Debug.LogWarning("[PoseController] _poses配列とCaptureSlotIndexを先に用意してください。");
            return;
        }

        var p = _poses[_captureSlotIndex];
        p.upperArmL = _upperArmL.localRotation.eulerAngles;
        p.upperArmR = _upperArmR.localRotation.eulerAngles;
        p.lowerArmL = _lowerArmL.localRotation.eulerAngles;
        p.lowerArmR = _lowerArmR.localRotation.eulerAngles;

        Debug.Log($"[PoseController] スロット{_captureSlotIndex}に現在の腕の角度を記録しました。");
    }
}