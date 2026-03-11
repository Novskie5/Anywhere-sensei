using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;


public class NetworkManager : MonoBehaviourPunCallbacks
{
    void Start()
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
    GameObject avatar = PhotonNetwork.Instantiate("VRM1.0TestAv", Vector3.zero, Quaternion.identity);
    
    FaceSync faceSync = avatar.GetComponent<FaceSync>();
    FaceLandmarkerRunner runner = FindObjectOfType<FaceLandmarkerRunner>();
    
    if (runner != null && faceSync != null)
    {
        runner.SetFaceSync(faceSync);
    }
}
}