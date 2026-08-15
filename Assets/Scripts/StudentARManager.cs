using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// Student_ARシーン用: 自分は表情トラッキングを持たない簡易アバターとしてInstantiateしつつ、
// (一人称視点のため自分自身のアバターは非表示にする)
// 先生(NetworkManager側でhostActorNrがマーキングされたアバター)と他の生徒をARAvatarPlacerへ渡す
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
        // 呼吸ゆらぎ(UpdateBreathing)だけが自動でかかったidle状態になる。
        // 自分は一人称視点で見るので、自分のアバター自体は自分には見せない(他の生徒からは見える)
        // 全員がVector3.zeroに出すと他クライアントのアバターと重なるので、ActorNumberでずらす
        // (AR側では見えないが、他クライアント側では実座標として残るため)
        Vector3 spawnPosition = new Vector3(PhotonNetwork.LocalPlayer.ActorNumber * 2f, 0f, 0f);
        GameObject ownAvatar = PhotonNetwork.Instantiate("VRM1.0TestAv", spawnPosition, Quaternion.identity);
        HideOwnAvatarRenderers(ownAvatar);

        // 既に部屋にいる先生・他の生徒のアバターが拾えるか試す(自分が後から入室したケース)
        TryResolveAvatars();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(RoomPropertyKeys.HostActorNr))
        {
            TryResolveAvatars();
        }
    }

    // FaceSyncを持つアバター(=先生・生徒・自分問わず全員)がスポーンする度に呼ばれる
    private void HandleAvatarSpawned(FaceSync faceSync)
    {
        if (faceSync.photonView.IsMine) return; // 自分は表示対象にしない

        _spawnedByActor[faceSync.photonView.OwnerActorNr] = faceSync;
        TryResolveAvatars();
    }

    // hostActorNrが分かっているアバターは先生として、それ以外(自分以外)は生徒として配置する
    private void TryResolveAvatars()
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomPropertyKeys.HostActorNr, out var hostActorNrObj)) return;

        int hostActorNr = (int)hostActorNrObj;
        foreach (var pair in _spawnedByActor)
        {
            if (pair.Value == null) continue;

            if (pair.Key == hostActorNr)
            {
                _placer.SetHostAvatar(pair.Value.gameObject);
            }
            else
            {
                _placer.AddOtherStudent(pair.Value.gameObject);
            }
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
