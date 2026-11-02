using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
using UnityEngine;

// 生徒のアバター(小さな浮遊ロボット)。先生アバターの周りの席にふわふわ浮かび、見ている人(カメラ)の方を向く。
// 席は入室者リストから各クライアントがローカルに計算する(全員が同じリストを持っているので同じ席になり、位置の同期は不要)
// ARAvatarPlacer.LateUpdateで先生が置かれた後に追従したいので、実行順を後ろにしている
[DefaultExecutionOrder(100)]
public class StudentRobot : MonoBehaviourPun
{
    // 席の角度(先生の正面=0°)。先生の体やPC側カメラの視界を塞がないよう、正面は避けて左右から埋めていく
    private static readonly float[] SeatAngles = { 70f, -70f, 105f, -105f, 140f, -140f, 40f, -40f };
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] private Transform _body; // 揺れ・ジャンプさせる見た目部分(ルートは席の位置に固定)
    [SerializeField] private Renderer[] _tintRenderers; // 生徒ごとの色に染める部分
    [SerializeField] private Renderer[] _glowRenderers; // 目など、喋っているときに光る部分
    [SerializeField] private float _seatRadius = 0.8f;
    [SerializeField] private float _hoverHeight = 0.55f;

    private PhotonVoiceView _voiceView;
    private MaterialPropertyBlock _block;
    private Transform _teacher;
    private float _nextTeacherSearchTime;
    private Color _color;
    private float _phase;
    private float _speaking; // 0〜1。喋り始め/終わりで見た目がパキッと切り替わらないよう補間する

    private void Start()
    {
        _voiceView = GetComponent<PhotonVoiceView>();
        _block = new MaterialPropertyBlock();

        int actorNr = photonView.OwnerActorNr;
        // 色相を黄金比でずらすと、連番で入室した生徒同士でも似た色になりにくい
        _color = Color.HSVToRGB(Mathf.Repeat(actorNr * 0.618f, 1f), 0.55f, 1f);
        _phase = actorNr * 1.7f;

        foreach (Renderer r in _tintRenderers) SetColor(r, _color);
    }

    private void LateUpdate()
    {
        if (_teacher == null)
        {
            FindTeacher();
            if (_teacher == null) return;
        }

        PlaceAtSeat();
        Animate();
    }

    // 先生 = Room Custom PropertyのhostActorNrが持ち主のアバター(FaceSyncを持つのは先生のVRMだけ)
    private void FindTeacher()
    {
        if (Time.time < _nextTeacherSearchTime) return;
        _nextTeacherSearchTime = Time.time + 1f;

        int hostActorNr = GetHostActorNr();
        if (hostActorNr < 0) return;

        foreach (FaceSync faceSync in FindObjectsOfType<FaceSync>())
        {
            if (faceSync.photonView.OwnerActorNr == hostActorNr)
            {
                _teacher = faceSync.transform;
                return;
            }
        }
    }

    private static int GetHostActorNr()
    {
        Room room = PhotonNetwork.CurrentRoom;
        if (room == null || !room.CustomProperties.TryGetValue(RoomPropertyKeys.HostActorNr, out object value)) return -1;
        return (int)value;
    }

    // 自分より若いActorNumberの生徒が何人いるか = 自分の席番号。誰かが抜けると自動で詰まる
    private int GetSeatIndex()
    {
        int hostActorNr = GetHostActorNr();
        int myActorNr = photonView.OwnerActorNr;
        int index = 0;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber != hostActorNr && player.ActorNumber < myActorNr) index++;
        }
        return index;
    }

    private void PlaceAtSeat()
    {
        int seat = GetSeatIndex();
        int ring = seat / SeatAngles.Length; // 席が埋まったら一回り外側の輪へ
        float angle = SeatAngles[seat % SeatAngles.Length];
        float radius = _seatRadius + ring * 0.35f;

        // 先生のローカル座標で置くので、ARで先生を拡大縮小・回転してもロボットごと付いてくる
        Vector3 local = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius + Vector3.up * _hoverHeight;
        transform.position = _teacher.TransformPoint(local);
        transform.localScale = Vector3.one * _teacher.lossyScale.y;

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 toCamera = cam.transform.position - transform.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }

    private void Animate()
    {
        bool isSpeaking = _voiceView != null && _voiceView.IsSpeaking;
        _speaking = Mathf.MoveTowards(_speaking, isSpeaking ? 1f : 0f, Time.deltaTime * 6f);

        float t = Time.time;
        // 常時: ゆっくり上下＋左右にゆらゆら(phaseをずらして全員が同期して揺れないようにする)
        float bob = Mathf.Sin(t * 2f + _phase) * 0.03f;
        float wobble = Mathf.Sin(t * 1.3f + _phase) * 6f;
        // 喋っている間: 小刻みに跳ねる
        float hop = Mathf.Abs(Mathf.Sin(t * 14f)) * 0.03f * _speaking;

        if (_body != null)
        {
            _body.localPosition = Vector3.up * (bob + hop);
            _body.localRotation = Quaternion.Euler(0f, 0f, wobble);
        }

        Color glow = Color.Lerp(_color, Color.white, 0.8f * _speaking);
        foreach (Renderer r in _glowRenderers) SetColor(r, glow);
    }

    private void SetColor(Renderer r, Color color)
    {
        if (r == null) return;
        r.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, color);
        r.SetPropertyBlock(_block);
    }
}
