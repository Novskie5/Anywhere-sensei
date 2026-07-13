using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// Student_ARシーン用: 自分は表情トラッキングを持たない簡易アバターとしてInstantiateしつつ、
// 先生(NetworkManager側でhostActorNrがマーキングされたアバター)を見つけてARAvatarPlacerへ渡す
public class StudentARManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private ARAvatarPlacer _placer;
    [SerializeField] private string _roomName = "TestRoom";

    private readonly Dictionary<int, FaceSync> _spawnedByActor = new Dictionary<int, FaceSync>();

    private void OnEnable()
    {
        FaceSync.OnAnySpawned += HandleAvatarSpawned;
    }

    private void OnDisable()
    {
        FaceSync.OnAnySpawned -= HandleAvatarSpawned;
    }

    private void Start()
    {
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

        // 自分用の簡易アバター。表情トラッキングは繋がないので、PoseControllerの
        // 呼吸ゆらぎ(UpdateBreathing)だけが自動でかかったidle状態になる
        GameObject ownAvatar = PhotonNetwork.Instantiate("VRM1.0TestAv", Vector3.zero, Quaternion.identity);
        _placer.SetOwnAvatar(ownAvatar);

        // 既に部屋にいる先生のアバターが拾えるか試す(先生が先に入室しているケース)
        TryResolveHostAvatar();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(RoomPropertyKeys.HostActorNr))
        {
            TryResolveHostAvatar();
        }
    }

    // FaceSyncを持つアバター(=先生・自分問わず全員)がスポーンする度に呼ばれる
    private void HandleAvatarSpawned(FaceSync faceSync)
    {
        _spawnedByActor[faceSync.photonView.OwnerActorNr] = faceSync;
        TryResolveHostAvatar();
    }

    private void TryResolveHostAvatar()
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomPropertyKeys.HostActorNr, out var hostActorNrObj)) return;

        int hostActorNr = (int)hostActorNrObj;
        if (_spawnedByActor.TryGetValue(hostActorNr, out var hostFaceSync) && hostFaceSync != null)
        {
            _placer.SetHostAvatar(hostFaceSync.gameObject);
        }
    }
}
