using UnityEngine;
using UniVRM10;
using System.Collections.Generic;

public class HandSync : MonoBehaviour
{
    [SerializeField] private Vrm10Instance _instance;
    [Range(0.01f, 1.0f)]
    [SerializeField] private float _smoothSpeed = 0.3f;

    // 座標変換パターン：インスペクターで切り替えて試してね
    public enum CoordMode { A, B, C, D }
    [SerializeField] private CoordMode _coordMode = CoordMode.A;

    // ============================================================
    // MediaPipe HandLandmark インデックス定数
    // ============================================================
    private const int WRIST      = 0;
    private const int THUMB_CMC  = 1;
    private const int THUMB_MCP  = 2;
    private const int THUMB_IP   = 3;
    private const int THUMB_TIP  = 4;
    private const int INDEX_MCP  = 5;
    private const int INDEX_PIP  = 6;
    private const int INDEX_DIP  = 7;
    private const int INDEX_TIP  = 8;
    private const int MIDDLE_MCP = 9;
    private const int MIDDLE_PIP = 10;
    private const int MIDDLE_DIP = 11;
    private const int MIDDLE_TIP = 12;
    private const int RING_MCP   = 13;
    private const int RING_PIP   = 14;
    private const int RING_DIP   = 15;
    private const int RING_TIP   = 16;
    private const int PINKY_MCP  = 17;
    private const int PINKY_PIP  = 18;
    private const int PINKY_DIP  = 19;
    private const int PINKY_TIP  = 20;

    private Vector3[] _leftLandmarks  = null;
    private Vector3[] _rightLandmarks = null;
    private bool _leftDetected  = false;
    private bool _rightDetected = false;

    private Dictionary<HumanBodyBones, Transform> _boneCache = new Dictionary<HumanBodyBones, Transform>();

    private void Start()
    {
        if (_instance == null) return;
        var animator = _instance.GetComponent<Animator>();
        if (animator == null) return;

        var bones = new[]
        {
            HumanBodyBones.LeftHand,  HumanBodyBones.RightHand,
            HumanBodyBones.LeftThumbProximal,     HumanBodyBones.LeftThumbIntermediate,     HumanBodyBones.LeftThumbDistal,
            HumanBodyBones.LeftIndexProximal,     HumanBodyBones.LeftIndexIntermediate,     HumanBodyBones.LeftIndexDistal,
            HumanBodyBones.LeftMiddleProximal,    HumanBodyBones.LeftMiddleIntermediate,    HumanBodyBones.LeftMiddleDistal,
            HumanBodyBones.LeftRingProximal,      HumanBodyBones.LeftRingIntermediate,      HumanBodyBones.LeftRingDistal,
            HumanBodyBones.LeftLittleProximal,    HumanBodyBones.LeftLittleIntermediate,    HumanBodyBones.LeftLittleDistal,
            HumanBodyBones.RightThumbProximal,    HumanBodyBones.RightThumbIntermediate,    HumanBodyBones.RightThumbDistal,
            HumanBodyBones.RightIndexProximal,    HumanBodyBones.RightIndexIntermediate,    HumanBodyBones.RightIndexDistal,
            HumanBodyBones.RightMiddleProximal,   HumanBodyBones.RightMiddleIntermediate,   HumanBodyBones.RightMiddleDistal,
            HumanBodyBones.RightRingProximal,     HumanBodyBones.RightRingIntermediate,     HumanBodyBones.RightRingDistal,
            HumanBodyBones.RightLittleProximal,   HumanBodyBones.RightLittleIntermediate,   HumanBodyBones.RightLittleDistal,
        };

        foreach (var bone in bones)
        {
            var t = animator.GetBoneTransform(bone);
            if (t != null) _boneCache[bone] = t;
        }
    }

    public void UpdateHand(bool isLeft, IList<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> landmarks)
    {
        if (landmarks == null || landmarks.Count < 21) return;

        var pts = new Vector3[21];
        for (int i = 0; i < 21; i++)
        {
            float x = landmarks[i].x;
            float y = landmarks[i].y;
            float z = landmarks[i].z;

            // インスペクターで切り替えて一番自然に見えるパターンを探してね
            pts[i] = _coordMode switch
            {
                CoordMode.A => new Vector3(-x, -y, -z),
                CoordMode.B => new Vector3(-x,  y,  z),
                CoordMode.C => new Vector3( x, -y,  z),
                CoordMode.D => new Vector3(-x, -y,  z),
                _           => new Vector3(-x, -y, -z),
            };
        }

        if (isLeft) { _leftLandmarks  = pts; _leftDetected  = true; }
        else        { _rightLandmarks = pts; _rightDetected = true; }
    }

    public void ResetHand(bool isLeft)
    {
        if (isLeft) _leftDetected  = false;
        else        _rightDetected = false;
    }

    private void LateUpdate()
    {
        if (_instance == null) return;

        if (_leftDetected && _leftLandmarks != null)
            ApplyHandToAvatar(true, _leftLandmarks);
        else
            ResetHandBones(true);

        if (_rightDetected && _rightLandmarks != null)
            ApplyHandToAvatar(false, _rightLandmarks);
        else
            ResetHandBones(false);
    }

    private void ApplyHandToAvatar(bool isLeft, Vector3[] pts)
    {
        ApplyWristRotation(isLeft, pts);
        ApplyFingerRotations(isLeft, pts);
    }

    private void ApplyWristRotation(bool isLeft, Vector3[] pts)
    {
        Vector3 forward     = (pts[MIDDLE_MCP] - pts[WRIST]).normalized;
        Vector3 indexToRing = (pts[INDEX_MCP]  - pts[RING_MCP]).normalized;
        Vector3 up          = Vector3.Cross(forward, indexToRing).normalized;
        if (forward == Vector3.zero || up == Vector3.zero) return;

        var bone = isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
        SetBoneRotationSmooth(bone, Quaternion.LookRotation(forward, up));
    }

    private void ApplyFingerRotations(bool isLeft, Vector3[] pts)
    {
        // 親指
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftThumbProximal      : HumanBodyBones.RightThumbProximal,      pts[THUMB_CMC],  pts[THUMB_MCP],  pts[THUMB_IP]);
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftThumbIntermediate  : HumanBodyBones.RightThumbIntermediate,  pts[THUMB_MCP],  pts[THUMB_IP],   pts[THUMB_TIP]);
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftThumbDistal        : HumanBodyBones.RightThumbDistal,        pts[THUMB_IP],   pts[THUMB_TIP],  pts[THUMB_TIP]  + (pts[THUMB_TIP]  - pts[THUMB_IP]));
        // 人差し指
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftIndexProximal      : HumanBodyBones.RightIndexProximal,      pts[INDEX_MCP],  pts[INDEX_PIP],  pts[INDEX_DIP]);
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftIndexIntermediate  : HumanBodyBones.RightIndexIntermediate,  pts[INDEX_PIP],  pts[INDEX_DIP],  pts[INDEX_TIP]);
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftIndexDistal        : HumanBodyBones.RightIndexDistal,        pts[INDEX_DIP],  pts[INDEX_TIP],  pts[INDEX_TIP]  + (pts[INDEX_TIP]  - pts[INDEX_DIP]));
        // 中指
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftMiddleProximal     : HumanBodyBones.RightMiddleProximal,     pts[MIDDLE_MCP], pts[MIDDLE_PIP], pts[MIDDLE_DIP]);
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftMiddleIntermediate : HumanBodyBones.RightMiddleIntermediate, pts[MIDDLE_PIP], pts[MIDDLE_DIP], pts[MIDDLE_TIP]);
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftMiddleDistal       : HumanBodyBones.RightMiddleDistal,       pts[MIDDLE_DIP], pts[MIDDLE_TIP], pts[MIDDLE_TIP] + (pts[MIDDLE_TIP] - pts[MIDDLE_DIP]));
        // 薬指
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftRingProximal       : HumanBodyBones.RightRingProximal,       pts[RING_MCP],   pts[RING_PIP],   pts[RING_DIP]);
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftRingIntermediate   : HumanBodyBones.RightRingIntermediate,   pts[RING_PIP],   pts[RING_DIP],   pts[RING_TIP]);
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftRingDistal         : HumanBodyBones.RightRingDistal,         pts[RING_DIP],   pts[RING_TIP],   pts[RING_TIP]   + (pts[RING_TIP]   - pts[RING_DIP]));
        // 小指
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftLittleProximal     : HumanBodyBones.RightLittleProximal,     pts[PINKY_MCP],  pts[PINKY_PIP],  pts[PINKY_DIP]);
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftLittleIntermediate : HumanBodyBones.RightLittleIntermediate, pts[PINKY_PIP],  pts[PINKY_DIP],  pts[PINKY_TIP]);
        ApplyJointRotation(isLeft ? HumanBodyBones.LeftLittleDistal       : HumanBodyBones.RightLittleDistal,       pts[PINKY_DIP],  pts[PINKY_TIP],  pts[PINKY_TIP]  + (pts[PINKY_TIP]  - pts[PINKY_DIP]));
    }

    private void ApplyJointRotation(HumanBodyBones bone, Vector3 prev, Vector3 curr, Vector3 next)
    {
        Vector3 boneDir  = (curr - prev).normalized;
        Vector3 childDir = (next - curr).normalized;
        if (boneDir == Vector3.zero) return;

        Vector3 up = Vector3.Cross(boneDir, childDir);
        if (up == Vector3.zero) up = Vector3.up;

        SetBoneRotationSmooth(bone, Quaternion.LookRotation(boneDir, up.normalized));
    }

    private void SetBoneRotationSmooth(HumanBodyBones bone, Quaternion target)
    {
        if (!_boneCache.TryGetValue(bone, out var t) || t == null) return;
        t.localRotation = Quaternion.Slerp(t.localRotation, target, _smoothSpeed);
    }

    private void ResetHandBones(bool isLeft)
    {
        var bones = isLeft ? new[]
        {
            HumanBodyBones.LeftHand,
            HumanBodyBones.LeftThumbProximal,     HumanBodyBones.LeftThumbIntermediate,     HumanBodyBones.LeftThumbDistal,
            HumanBodyBones.LeftIndexProximal,     HumanBodyBones.LeftIndexIntermediate,     HumanBodyBones.LeftIndexDistal,
            HumanBodyBones.LeftMiddleProximal,    HumanBodyBones.LeftMiddleIntermediate,    HumanBodyBones.LeftMiddleDistal,
            HumanBodyBones.LeftRingProximal,      HumanBodyBones.LeftRingIntermediate,      HumanBodyBones.LeftRingDistal,
            HumanBodyBones.LeftLittleProximal,    HumanBodyBones.LeftLittleIntermediate,    HumanBodyBones.LeftLittleDistal,
        }
        : new[]
        {
            HumanBodyBones.RightHand,
            HumanBodyBones.RightThumbProximal,    HumanBodyBones.RightThumbIntermediate,    HumanBodyBones.RightThumbDistal,
            HumanBodyBones.RightIndexProximal,    HumanBodyBones.RightIndexIntermediate,    HumanBodyBones.RightIndexDistal,
            HumanBodyBones.RightMiddleProximal,   HumanBodyBones.RightMiddleIntermediate,   HumanBodyBones.RightMiddleDistal,
            HumanBodyBones.RightRingProximal,     HumanBodyBones.RightRingIntermediate,     HumanBodyBones.RightRingDistal,
            HumanBodyBones.RightLittleProximal,   HumanBodyBones.RightLittleIntermediate,   HumanBodyBones.RightLittleDistal,
        };

        foreach (var bone in bones)
            SetBoneRotationSmooth(bone, Quaternion.identity);
    }
}