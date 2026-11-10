using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Student_ARシーン用: 認識した床に先生のアバターを配置する
// (他の生徒のロボットはStudentRobotが先生の周りに自分で並ぶ。自分のロボットはStudentARManager側で非表示にする)
//
// 置く場所の精度を上げるため:
// - 画面中央の床に「ここに立つ」目安の輪(レティクル)を出し、タップするとその輪の位置に置く
//   (輪が出ている = その場所を床として認識済み、が目で分かる。輪の大きさで等身大の目安にもなる)
// - 上向きの水平面(床・地面)だけを対象にする(壁や天井には置かない)
// - トラッキングが安定する前(起動直後で床の高さの推定が甘い時期)は置かせない
// - ARAnchorで床に固定し、ARCoreが後から床の推定を修正したらアバターも一緒に追従させる
//
// 置いた後の操作: 1本指ドラッグで床の上を移動 / 2本指ピンチで拡大縮小 / ダブルタップで等身大に戻す
// (先生の大きさを変えると、StudentRobotは先生のスケール基準で並ぶので周りのロボットも一緒に縮む)
[RequireComponent(typeof(ARRaycastManager))]
public class ARAvatarPlacer : MonoBehaviour
{
    [SerializeField] private ARRaycastManager _raycastManager;
    [SerializeField] private float _avatarScale = 1f;
    [SerializeField] private float _reticleRadius = 0.3f; // 大人が立っている足元くらいの大きさ
    [SerializeField] private Color _reticleColor = new Color(0.3f, 0.9f, 1f);
    [SerializeField] private float _minScale = 0.2f;
    [SerializeField] private float _maxScale = 2f;
    [SerializeField] private float _doubleTapInterval = 0.3f;

    private static readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();

    private ARPlaneManager _planeManager;
    private ARAnchorManager _anchorManager;
    private Transform _anchor; // 先生の立ち位置(向きは毎フレームカメラに合わせる)
    private ARAnchor _arAnchor;
    private bool _placed;
    private GameObject _hostAvatar;

    private LineRenderer _reticle;
    private bool _hasCandidate;
    private Pose _candidatePose;
    private ARPlane _candidatePlane;

    // ジェスチャー判定用
    private bool _gestureOverUI;   // UIの上から始まった操作は最後まで無視する
    private bool _pinching;        // 一度2本指になったら、指が全部離れるまでドラッグ/タップ扱いしない
    private float _pinchStartDistance;
    private float _pinchStartScale;
    private bool _dragging;
    private Vector2 _touchStartPosition;
    private Pose _dragPose;
    private ARPlane _dragPlane;
    private float _lastTapTime = -1f;

    // 画面の案内文(PlacementHintView)が今どの段階かを見るためのもの
    public bool HasFloorCandidate => _hasCandidate;
    public bool IsPlaced => _placed;
    public bool HasHostAvatar => _hostAvatar != null;

    private void Awake()
    {
        if (_raycastManager == null) _raycastManager = GetComponent<ARRaycastManager>();
        _planeManager = GetComponent<ARPlaneManager>();
        if (_planeManager != null) _planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        // アンカー用のマネージャーはシーンに置いていないので、無ければ実行時に足す
        _anchorManager = GetComponent<ARAnchorManager>();
        if (_anchorManager == null) _anchorManager = gameObject.AddComponent<ARAnchorManager>();

        _anchor = new GameObject("ARAvatarAnchor").transform;
        _reticle = CreateReticle();
    }

    private void Update()
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        _hasCandidate = ARSession.state == ARSessionState.SessionTracking &&
                        TryFindFloor(screenCenter, out _candidatePose, out _candidatePlane);

        // 置いた後は輪を消す(アバターの足元と重なって邪魔なので)
        _reticle.enabled = _hasCandidate && !_placed;
        if (_hasCandidate)
        {
            _reticle.transform.position = _candidatePose.position + Vector3.up * 0.005f; // 床とのZファイティング防止
        }

        if (_placed) HandleManipulation();
        else HandleFirstPlacement();
    }

    // 最初の1回: 輪が出ている状態でタップすると輪の位置に置く
    private void HandleFirstPlacement()
    {
        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began && !IsOverUI(touch) && _hasCandidate)
        {
            Place(_candidatePose, _candidatePlane);
        }
    }

    // 置いた後: ドラッグで移動 / ピンチで拡大縮小 / ダブルタップで等身大に戻す
    private void HandleManipulation()
    {
        if (Input.touchCount == 0)
        {
            _pinching = false;
            return;
        }

        Touch first = Input.GetTouch(0);
        if (first.phase == TouchPhase.Began) _gestureOverUI = IsOverUI(first);
        if (_gestureOverUI) return;

        if (Input.touchCount >= 2)
        {
            HandlePinch(first, Input.GetTouch(1));
            return;
        }
        if (_pinching) return; // ピンチ後に片方の指だけ残っていてもドラッグ扱いしない

        HandleDragOrTap(first);
    }

    private void HandlePinch(Touch a, Touch b)
    {
        float distance = Vector2.Distance(a.position, b.position);
        if (!_pinching || a.phase == TouchPhase.Began || b.phase == TouchPhase.Began)
        {
            // ピンチ開始時の指の間隔と大きさを基準にする(毎フレームの差分を掛け続けると誤差が溜まるため)
            _pinching = true;
            _pinchStartDistance = Mathf.Max(distance, 1f);
            _pinchStartScale = _avatarScale;
            CancelDrag();
            return;
        }
        _avatarScale = Mathf.Clamp(_pinchStartScale * distance / _pinchStartDistance, _minScale, _maxScale);
    }

    private void HandleDragOrTap(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                _touchStartPosition = touch.position;
                _dragging = false;
                break;

            case TouchPhase.Moved:
                // 指が少し動いただけ(タップのブレ)ではドラッグにしない。約2.5mm
                float threshold = Screen.dpi > 0f ? Screen.dpi * 0.1f : 30f;
                if (!_dragging && Vector2.Distance(touch.position, _touchStartPosition) > threshold)
                {
                    _dragging = true;
                    _dragPose = new Pose(_anchor.position, Quaternion.identity);
                    _dragPlane = null;
                }
                // 指の下の床へ動かす。床が無い所(壁や空中)を指している間は最後の位置で止める
                if (_dragging && TryFindFloor(touch.position, out Pose pose, out ARPlane plane))
                {
                    _dragPose = pose;
                    _dragPlane = plane;
                    _anchor.position = pose.position;
                }
                break;

            case TouchPhase.Ended:
                if (_dragging)
                {
                    // 移動し終わった場所の床にアンカーを付け直す
                    if (_dragPlane != null) Place(_dragPose, _dragPlane);
                    _dragging = false;
                }
                else
                {
                    if (_lastTapTime >= 0f && Time.time - _lastTapTime < _doubleTapInterval)
                    {
                        _avatarScale = 1f;
                        _lastTapTime = -1f;
                    }
                    else
                    {
                        _lastTapTime = Time.time;
                    }
                }
                break;

            case TouchPhase.Canceled:
                CancelDrag();
                break;
        }
    }

    // ドラッグを中断したら元のアンカー位置に戻す(LateUpdateが_arAnchorへ追従し直す)
    private void CancelDrag()
    {
        _dragging = false;
    }

    private static bool IsOverUI(Touch touch)
    {
        // ミュートボタン等のUIをタップしたときにアバターまで動かないようにする
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId);
    }

    // Animatorの評価(Update後)で位置・向きが上書きされても最終的にこちらが勝つよう、LateUpdateで配置する
    private void LateUpdate()
    {
        if (!_placed) return;

        // ARCoreが床の推定を修正するとアンカーが動くので、毎フレームそれに合わせる(ドラッグ中は指の位置を優先)
        if (_arAnchor != null && !_dragging) _anchor.position = _arAnchor.transform.position;

        // 向きはタップした瞬間ではなく毎フレーム今のカメラ位置を見て計算する
        // (ユーザーが移動すると、置いた瞬間の向きのままだと背中を向けて見えてしまうため)
        _anchor.rotation = FacingCameraRotation(_anchor.position);
        ApplyPlacement();
    }

    private bool TryFindFloor(Vector2 screenPosition, out Pose pose, out ARPlane plane)
    {
        pose = default;
        plane = null;
        if (!_raycastManager.Raycast(screenPosition, _hits, TrackableType.PlaneWithinPolygon)) return false;

        // 手前から順に見て、最初の上向き水平面を採用する
        foreach (ARRaycastHit hit in _hits)
        {
            ARPlane hitPlane = _planeManager != null ? _planeManager.GetPlane(hit.trackableId) : null;
            if (hitPlane == null || hitPlane.alignment != PlaneAlignment.HorizontalUp) continue;
            pose = hit.pose;
            plane = hitPlane;
            return true;
        }
        return false;
    }

    private void Place(Pose pose, ARPlane plane)
    {
        if (_arAnchor != null) Destroy(_arAnchor.gameObject);
        _arAnchor = _anchorManager.AttachAnchor(plane, pose); // 失敗するとnull。その場合はタップ位置で固定のまま使う

        _anchor.position = pose.position;
        _placed = true;
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

    private void ApplyPlacement()
    {
        if (_hostAvatar == null) return;
        _hostAvatar.transform.SetPositionAndRotation(_anchor.position, _anchor.rotation);
        _hostAvatar.transform.localScale = Vector3.one * _avatarScale;
    }

    // 床に置く目安の輪。素材を用意しなくて済むよう、LineRendererで円を描く
    private LineRenderer CreateReticle()
    {
        const int segments = 48;
        var line = new GameObject("PlacementReticle").AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;
        line.widthMultiplier = 0.012f;
        line.alignment = LineAlignment.TransformZ; // 線の面を床と平行にする(カメラ向きだと斜めから見ると細くなる)
        line.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            // 90°倒しているのでローカルXY平面 = ワールドの床平面
            line.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * _reticleRadius);
        }

        // URP/Unlitは生徒ロボットのマテリアルでも使っているのでビルドに含まれている
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", _reticleColor);
        line.sharedMaterial = material;
        line.enabled = false;
        return line;
    }
}
