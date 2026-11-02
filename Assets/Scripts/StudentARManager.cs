using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// Student_ARシーン用: 自分は生徒ロボット(StudentRobot)としてInstantiateしつつ、
// (一人称視点のため自分自身のロボットは非表示にする)
// 先生(NetworkManager側でhostActorNrがマーキングされたアバター)をARAvatarPlacerへ渡す
public class StudentARManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private ARAvatarPlacer _placer;
    [SerializeField] private string _roomName = "TestRoom";

    private readonly Dictionary<int, FaceSync> _spawnedByActor = new Dictionary<int, FaceSync>();

    public override void OnEnable()
    {
        base.OnEnable();
        FaceSync.OnAnySpawned += HandleAvatarSpawned;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        FaceSync.OnAnySpawned -= HandleAvatarSpawned;
    }

    private void Start()
    {
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = RoomPropertyKeys.Region; // 先生と同じリージョンに揃える
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[StudentAR] サーバーに繋がった！");
        PhotonNetwork.JoinOrCreateRoom(_roomName, new RoomOptions { MaxPlayers = 6 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[StudentAR] 部屋に入った！");

        // 自分用のアバター(小さな浮遊ロボット)。先生や他の生徒の画面に「出席している生徒」として表示され、
        // ボイスチャットの送信元(PhotonVoiceView)も兼ねる。位置はStudentRobot自身が先生の周りの席に合わせるので原点でよい
        // 自分は一人称視点で見るので、自分のアバター自体は自分には見せない
        GameObject ownAvatar = PhotonNetwork.Instantiate("StudentRobot", Vector3.zero, Quaternion.identity);
        HideOwnAvatarRenderers(ownAvatar);

        // 既に部屋にいる先生のアバターが拾えるか試す(自分が後から入室したケース)
        TryResolveAvatars();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(RoomPropertyKeys.HostActorNr))
        {
            TryResolveAvatars();
        }
    }

    // FaceSyncを持つアバター(=先生のVRM)がスポーンする度に呼ばれる
    private void HandleAvatarSpawned(FaceSync faceSync)
    {
        if (faceSync.photonView.IsMine) return; // 自分は表示対象にしない

        _spawnedByActor[faceSync.photonView.OwnerActorNr] = faceSync;
        TryResolveAvatars();
    }

    // hostActorNrが持ち主のアバターを先生としてARの配置対象にする
    // (他の生徒のロボットはStudentRobotが先生の周りに自分で並ぶので、ここでは扱わない)
    private void TryResolveAvatars()
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomPropertyKeys.HostActorNr, out var hostActorNrObj)) return;

        int hostActorNr = (int)hostActorNrObj;
        if (_spawnedByActor.TryGetValue(hostActorNr, out FaceSync teacher) && teacher != null)
        {
            _placer.SetHostAvatar(teacher.gameObject);
        }
    }

    private static void HideOwnAvatarRenderers(GameObject avatar)
    {
        foreach (Renderer renderer in avatar.GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = false;
        }
    }
}
