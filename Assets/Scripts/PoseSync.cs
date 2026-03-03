using UnityEngine;
using UniVRM10;
using System.Collections.Generic;

/// <summary>
/// MediaPipeのPoseLandmarkerResultを受け取り、VRM1.0アバターの腕ボーンに反映するクラス。
/// 差分回転方式：Tポーズ基準からの変化量だけをボーンに適用する。
/// これによりアバターのボーン初期姿勢に依存しなくなる。
/// </summary>
public class PoseSync : MonoBehaviour
{
    [SerializeField] private Vrm10Instance _instance;
    [Range(0.01f, 1.0f)]
    [SerializeField] private float _smoothSpeed = 0.15f;

    // ============================================================
    // MediaPipe PoseLandmark インデックス定数
    // ============================================================
    private const int LEFT_SHOULDER  = 11;
    private const int RIGHT_SHOULDER = 12;
    private const int LEFT_ELBOW     = 13;
    private const int RIGHT_ELBOW    = 14;
    private const int LEFT_WRIST     = 15;
    private const int RIGHT_WRIST    = 16;
    private const int LEFT_HIP       = 23;
    private const int RIGHT_HIP      = 24;

    // ============================================================
    // コールバックスレッドから書き込まれるデータ
    // ============================================================
    private Vector3[] _poseLandmarks = null;
    private bool _poseDetected = false;

    // ボーンTransformキャッシュ
    private Dictionary<HumanBodyBones, Transform> _boneCache = new Dictionary<HumanBodyBones, Transform>();

    // ============================================================
    // 差分方式に必要な「基準状態」の記録
    // 最初のフレームでTポーズに近い状態を基準として保存する
    // ============================================================
    private bool _isCalibrated = false;

    // 基準フレームでのランドマーク方向ベクトル
    private Vector3 _restLeftUpperArmDir;
    private Vector3 _restLeftLowerArmDir;
    private Vector3 _restRightUpperArmDir;
    private Vector3 _restRightLowerArmDir;

    // 基準フレームでのボーン回転
    private Quaternion _restLeftUpperArmRot;
    private Quaternion _restLeftLowerArmRot;
    private Quaternion _restRightUpperArmRot;
    private Quaternion _restRightLowerArmRot;

    // ============================================================
    // 初期化
    // ============================================================
    private void Start()
    {
        if (_instance == null) return;
        var animator = _instance.GetComponent<Animator>();
        if (animator == null) return;

        var bones = new[]
        {
            HumanBodyBones.LeftUpperArm,  HumanBodyBones.LeftLowerArm,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
            HumanBodyBones.Spine,         HumanBodyBones.Chest,
        };

        foreach (var bone in bones)
        {
            var t = animator.GetBoneTransform(bone);
            if (t != null) _boneCache[bone] = t;
        }
    }

    // ============================================================
    // コールバックスレッドから呼ばれる（データ保存のみ！）
    // ============================================================
    public void UpdatePose(IList<Mediapipe.Tasks.Components.Containers.Landmark> worldLandmarks)
    {
        if (worldLandmarks == null || worldLandmarks.Count < 25) return;

        var pts = new Vector3[worldLandmarks.Count];
        for (int i = 0; i < worldLandmarks.Count; i++)
        {
            pts[i] = new Vector3(
                -worldLandmarks[i].x,
                -worldLandmarks[i].y,
                -worldLandmarks[i].z
            );
        }

        _poseLandmarks = pts;
        _poseDetected  = true;
    }

    public void ResetPose()
    {
        _poseDetected = false;
    }

    // キャリブレーションをリセット（インスペクターのボタンや任意のタイミングで呼べる）
    public void Recalibrate()
    {
        _isCalibrated = false;
    }

    // ============================================================
    // LateUpdate：差分回転をボーンに適用
    // ============================================================
    private void LateUpdate()
    {
        if (_instance == null) return;

        if (!_poseDetected || _poseLandmarks == null)
        {
            ResetArmBones();
            return;
        }

        // 最初のフレームで基準状態を記録（キャリブレーション）
        if (!_isCalibrated)
        {
            Calibrate(_poseLandmarks);
            return;
        }

        ApplyPoseToAvatar(_poseLandmarks);
    }

    // ============================================================
    // キャリブレーション：現在の姿勢を基準として記録
    // できればTポーズで呼ばれるのが理想だけど、
    // 起動直後の姿勢でも差分方式なので大きく破綻しない
    // ============================================================
    private void Calibrate(Vector3[] pts)
    {
        _restLeftUpperArmDir  = (pts[LEFT_ELBOW]    - pts[LEFT_SHOULDER]).normalized;
        _restLeftLowerArmDir  = (pts[LEFT_WRIST]    - pts[LEFT_ELBOW]).normalized;
        _restRightUpperArmDir = (pts[RIGHT_ELBOW]   - pts[RIGHT_SHOULDER]).normalized;
        _restRightLowerArmDir = (pts[RIGHT_WRIST]   - pts[RIGHT_ELBOW]).normalized;

        // 基準フレームでのボーン回転を保存
        if (_boneCache.TryGetValue(HumanBodyBones.LeftUpperArm,  out var t)) _restLeftUpperArmRot  = t.rotation;
        if (_boneCache.TryGetValue(HumanBodyBones.LeftLowerArm,  out t))     _restLeftLowerArmRot  = t.rotation;
        if (_boneCache.TryGetValue(HumanBodyBones.RightUpperArm, out t))     _restRightUpperArmRot = t.rotation;
        if (_boneCache.TryGetValue(HumanBodyBones.RightLowerArm, out t))     _restRightLowerArmRot = t.rotation;

        _isCalibrated = true;
        Debug.Log("[PoseSync] キャリブレーション完了！");
    }

    // ============================================================
    // 差分回転をボーンに適用
    // ============================================================
    private void ApplyPoseToAvatar(Vector3[] pts)
    {
        // 左上腕
        ApplyDeltaRotation(
            HumanBodyBones.LeftUpperArm,
            _restLeftUpperArmDir,
            (pts[LEFT_ELBOW] - pts[LEFT_SHOULDER]).normalized,
            _restLeftUpperArmRot
        );

        // 左前腕
        ApplyDeltaRotation(
            HumanBodyBones.LeftLowerArm,
            _restLeftLowerArmDir,
            (pts[LEFT_WRIST] - pts[LEFT_ELBOW]).normalized,
            _restLeftLowerArmRot
        );

        // 右上腕
        ApplyDeltaRotation(
            HumanBodyBones.RightUpperArm,
            _restRightUpperArmDir,
            (pts[RIGHT_ELBOW] - pts[RIGHT_SHOULDER]).normalized,
            _restRightUpperArmRot
        );

        // 右前腕
        ApplyDeltaRotation(
            HumanBodyBones.RightLowerArm,
            _restRightLowerArmDir,
            (pts[RIGHT_WRIST] - pts[RIGHT_ELBOW]).normalized,
            _restRightLowerArmRot
        );
    }

    /// <summary>
    /// 差分回転を計算してボーンに適用する核心部分
    /// 基準方向 → 現在方向 への回転を、基準ボーン回転に掛け合わせる
    /// </summary>
    private void ApplyDeltaRotation(HumanBodyBones bone, Vector3 restDir, Vector3 currentDir, Quaternion restRot)
    {
        if (currentDir == Vector3.zero || restDir == Vector3.zero) return;
        if (!_boneCache.TryGetValue(bone, out var t) || t == null) return;

        // 基準方向から現在方向への差分回転
        var delta = Quaternion.FromToRotation(restDir, currentDir);

        // 基準ボーン回転 × 差分 = 現在のボーン回転
        var target = delta * restRot;

        // スムージングして適用
        t.rotation = Quaternion.Slerp(t.rotation, target, _smoothSpeed);
    }

    private void SetBoneRotationSmooth(HumanBodyBones bone, Quaternion target)
    {
        if (!_boneCache.TryGetValue(bone, out var t) || t == null) return;
        t.localRotation = Quaternion.Slerp(t.localRotation, target, _smoothSpeed);
    }

    private void ResetArmBones()
    {
        foreach (var bone in new[]
        {
            HumanBodyBones.LeftUpperArm,  HumanBodyBones.LeftLowerArm,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
        })
        SetBoneRotationSmooth(bone, Quaternion.identity);
    }
}