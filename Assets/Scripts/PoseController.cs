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

    [Header("Breathing (chest必須、shoulderは任意)")]
    [SerializeField] private float _breathSpeed = 0.25f;      // Perlinノイズを進める速さ
    [SerializeField] private float _breathAmplitude = 2.5f;   // 度数、揺れの最大幅
    [SerializeField] private Transform _shoulderL;
    [SerializeField] private Transform _shoulderR;
    private Quaternion _shoulderLDefaultRot;
    private Quaternion _shoulderRDefaultRot;

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
        if (_shoulderL != null)
            _shoulderLDefaultRot = _shoulderL.localRotation;
        if (_shoulderR != null)
            _shoulderRDefaultRot = _shoulderR.localRotation;

        if (_poses != null && _poses.Length > 0)
        {
            SetPose(0);
        }
    }

    private void Update()
    {
        UpdateBreathing();
    }

    // Sin波だけだと毎周期まったく同じ動きになるので、Perlinノイズで速さ・深さに揺らぎを持たせている
    private void UpdateBreathing()
    {
        float t = Time.time * _breathSpeed;

        if (_chest != null)
        {
            float noise = Mathf.PerlinNoise(t, 0f) - 0.5f; // -0.5〜0.5
            float breath = noise * _breathAmplitude * 2f;
            _chest.localRotation = _chestDefaultRot * Quaternion.Euler(breath, 0, 0);
        }

        // 肩は左右で違う種(seed)を使い、chestと同じ動きにならないようにしている
        if (_shoulderL != null)
        {
            float noiseL = Mathf.PerlinNoise(t, 10f) - 0.5f;
            _shoulderL.localRotation = _shoulderLDefaultRot * Quaternion.Euler(0, 0, noiseL * _breathAmplitude);
        }
        if (_shoulderR != null)
        {
            float noiseR = Mathf.PerlinNoise(t, 20f) - 0.5f;
            _shoulderR.localRotation = _shoulderRDefaultRot * Quaternion.Euler(0, 0, -noiseR * _breathAmplitude);
        }
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