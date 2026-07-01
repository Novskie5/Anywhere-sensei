using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    // 自分のアバターのPoseController（UIボタンから呼び出す用）
    private PoseController _localPoseController;

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
        GameObject avatar = PhotonNetwork.Instantiate("VRM1.0TestAv", Vector3.zero, Quaternion.identity);

        FaceSync faceSync = avatar.GetComponent<FaceSync>();
        FaceLandmarkerRunner runner = FindObjectOfType<FaceLandmarkerRunner>();

        if (runner != null && faceSync != null)
        {
            runner.SetFaceSync(faceSync);
        }

        _localPoseController = avatar.GetComponent<PoseController>();
    }

    // プリセットポーズ選択用UIボタンのOnClick()から直接呼べる窓口
    public void SetLocalPose(int index)
    {
        if (_localPoseController != null)
        {
            _localPoseController.RequestSetPose(index);
        }
    }
}
