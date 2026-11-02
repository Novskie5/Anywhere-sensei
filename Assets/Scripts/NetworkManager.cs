using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;

public static class RoomPropertyKeys
{
    // 接続先のPhotonリージョン。空(自動)だと端末ごとに一番Pingの近いサーバーを選ぶので、
    // 日本だとPCはjp・スマホはrue(ロシア東部)のように分かれて同じ部屋名でも別の部屋に入ってしまう。先生と生徒で必ず揃える
    public const string Region = "jp";

    // ARクライアント側が「先生(トラッキングされてる方)」のアバターを見分けるためのキー
    public const string HostActorNr = "hostActorNr";
}

public class NetworkManager : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        // 上限なしで回るとWebカメラの読み取りが詰まり、カメラのLEDが点滅してトラッキングがカクつくため60fpsで固定する
        // (Quality SettingsのVSyncはEditorに上書きされて戻ったことがあるのでコードで指定。targetFrameRateはVSyncが有効だと無視されるので切る)
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = RoomPropertyKeys.Region;
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
        // (生徒は小さなロボットとして先生の周りの席に自分で並ぶ。StudentRobot.cs参照)
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

        AnimatorController gestureController = avatar.GetComponent<AnimatorController>();
        GestureUIController gestureUI = FindObjectOfType<GestureUIController>();

        if (gestureUI != null && gestureController != null)
        {
            gestureUI.SetTarget(gestureController);
        }
    }
}
