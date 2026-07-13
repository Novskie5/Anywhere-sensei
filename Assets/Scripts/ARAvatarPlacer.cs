using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Student_ARシーン用: 画面タップで検出した平面に、先生のアバターと自分の簡易アバターを並べて配置する
[RequireComponent(typeof(ARRaycastManager))]
public class ARAvatarPlacer : MonoBehaviour
{
    [SerializeField] private ARRaycastManager _raycastManager;
    [SerializeField] private float _avatarScale = 1f;
    [SerializeField] private Vector3 _ownAvatarOffset = new Vector3(1f, 0f, 0f); // 先生の隣に自分のアバターを置くオフセット

    private static readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();

    private Transform _anchor; // タップで置いた基準地点
    private GameObject _hostAvatar;
    private GameObject _ownAvatar;

    private void Awake()
    {
        if (_raycastManager == null) _raycastManager = GetComponent<ARRaycastManager>();
        _anchor = new GameObject("ARAvatarAnchor").transform;
    }

    private void Update()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        if (_raycastManager.Raycast(touch.position, _hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = _hits[0].pose;
            _anchor.SetPositionAndRotation(hitPose.position, hitPose.rotation);
            ApplyPlacement();
        }
    }

    public void SetHostAvatar(GameObject avatar)
    {
        _hostAvatar = avatar;
        ApplyPlacement();
    }

    public void SetOwnAvatar(GameObject avatar)
    {
        _ownAvatar = avatar;
        ApplyPlacement();
    }

    private void ApplyPlacement()
    {
        if (_hostAvatar != null)
        {
            _hostAvatar.transform.SetPositionAndRotation(_anchor.position, _anchor.rotation);
            _hostAvatar.transform.localScale = Vector3.one * _avatarScale;
        }

        if (_ownAvatar != null)
        {
            _ownAvatar.transform.SetPositionAndRotation(_anchor.TransformPoint(_ownAvatarOffset), _anchor.rotation);
            _ownAvatar.transform.localScale = Vector3.one * _avatarScale;
        }
    }
}
