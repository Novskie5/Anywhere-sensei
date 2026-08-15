using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Student_ARシーン用: 画面タップで検出した平面に、先生と他の生徒のアバターを並べて配置する
// (自分自身のアバターは一人称視点のため表示しない。StudentARManager側でレンダラーを非表示にする)
[RequireComponent(typeof(ARRaycastManager))]
public class ARAvatarPlacer : MonoBehaviour
{
    [SerializeField] private ARRaycastManager _raycastManager;
    [SerializeField] private float _avatarScale = 1f;
    [SerializeField] private float _otherStudentSpacing = 1.2f; // 他の生徒を横に並べる間隔

    private static readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();

    private Transform _anchor; // タップで置いた基準地点(先生の立ち位置)
    private bool _placed;
    private GameObject _hostAvatar;
    private readonly List<GameObject> _otherStudents = new List<GameObject>();

    private void Awake()
    {
        if (_raycastManager == null) _raycastManager = GetComponent<ARRaycastManager>();
        _anchor = new GameObject("ARAvatarAnchor").transform;
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began &&
                _raycastManager.Raycast(touch.position, _hits, TrackableType.PlaneWithinPolygon))
            {
                _anchor.position = _hits[0].pose.position;
                _placed = true;
            }
        }

        // 向きはタップした瞬間ではなく毎フレーム今のカメラ位置を見て計算する
        // (ユーザーが移動すると、置いた瞬間の向きのままだと背中を向けて見えてしまうため)
        if (_placed)
        {
            _anchor.rotation = FacingCameraRotation(_anchor.position);
            ApplyPlacement();
        }
    }

    // 平面のヒット姿勢は「上向き」しか保証されず正面はランダムなので、
    // 代わりにカメラ(=ユーザー)の方を向く水平回転を使う
    private static Quaternion FacingCameraRotation(Vector3 position)
    {
        Transform cam = Camera.main.transform;
        Vector3 toCamera = cam.position - position;
        toCamera.y = 0f;
        return toCamera.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(toCamera.normalized, Vector3.up)
            : Quaternion.identity;
    }

    public void SetHostAvatar(GameObject avatar)
    {
        _hostAvatar = avatar;
        ApplyPlacement();
    }

    public void AddOtherStudent(GameObject avatar)
    {
        if (_otherStudents.Contains(avatar)) return;
        _otherStudents.Add(avatar);
        ApplyPlacement();
    }

    private void ApplyPlacement()
    {
        if (_hostAvatar != null)
        {
            _hostAvatar.transform.SetPositionAndRotation(_anchor.position, _anchor.rotation);
            _hostAvatar.transform.localScale = Vector3.one * _avatarScale;
        }

        for (int i = 0; i < _otherStudents.Count; i++)
        {
            GameObject student = _otherStudents[i];
            if (student == null) continue;

            Vector3 offset = new Vector3(_otherStudentSpacing * (i + 1), 0f, 0f);
            student.transform.SetPositionAndRotation(_anchor.TransformPoint(offset), _anchor.rotation);
            student.transform.localScale = Vector3.one * _avatarScale;
        }
    }
}
