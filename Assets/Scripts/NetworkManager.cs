using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;

public static class RoomPropertyKeys
{
    // ARクライアント側が「先生(トラッキングされてる方)」のアバターを見分けるためのキー
    public const string HostActorNr = "hostActorNr";
}

public class NetworkManager : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        PhotonNetwork.ConnectUsingSettings(); // サーバーに繋ぐ
    }

    // 繋がったら呼ばれる
    public override void OnConnectedToMaster()
    {
        Debug.Log("サーバーに繋がった！");
        PhotonNetwork.JoinOrCreateRoom("TestRoom", new RoomOptions { MaxPlayers = 6 }, TypedLobby.Default);
    }

    // 部屋に入ったら呼ばれる
    public override void OnJoinedRoom()
    {
        Debug.Log("部屋に入った！");
        // 先生は自分のトラッキング確認用カメラに写る位置(原点)に固定で出す
        // (ずらすのは生徒側のアバターだけでよい。StudentARManager.cs参照)
        GameObject avatar = PhotonNetwork.Instantiate("VRM1.0TestAv", Vector3.zero, Quaternion.identity);

        // 自分がトラッキングを持つ「先生」であることをroomに宣言する
        // (最初に部屋を作った人=先生とは限らないため、明示的にマーキングする)
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            { RoomPropertyKeys.HostActorNr, PhotonNetwork.LocalPlayer.ActorNumber }
        });

        FaceSync faceSync = avatar.GetComponent<FaceSync>();
        FaceLandmarkerRunner runner = FindObjectOfType<FaceLandmarkerRunner>();

        if (runner != null && faceSync != null)
        {
            runner.SetFaceSync(faceSync);
        }
    }
}
