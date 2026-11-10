using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 画面の隅に出すPhotonの接続状態。今までDebug.Logにしか出ておらず、繋がっているのか画面から分からなかった
// 先生(Main_Tracking)と生徒(Student_AR)の両方で使う
// 状態の変化はコールバックが複数に分かれるので、毎フレーム状態を見て変わったときだけ書き換える
public class ConnectionStatusView : MonoBehaviour
{
    [SerializeField] private UITheme _theme;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Image _dot;

    private ClientState _lastState;
    private int _lastPlayerCount = -1;

    private void Start()
    {
        Refresh(force: true);
    }

    private void Update()
    {
        Refresh(force: false);
    }

    private void Refresh(bool force)
    {
        ClientState state = PhotonNetwork.NetworkClientState;
        int playerCount = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
        if (!force && state == _lastState && playerCount == _lastPlayerCount) return;
        _lastState = state;
        _lastPlayerCount = playerCount;

        switch (state)
        {
            case ClientState.Joined:
                Show($"Live  ·  {playerCount} in room", _theme.Success);
                break;
            case ClientState.Disconnected:
                Show("Offline", _theme.Danger);
                break;
            default:
                // ネームサーバー→マスター→入室の途中の細かい状態はまとめて「接続中」にする
                // (起動直後のPeerCreatedも、NetworkManager/StudentARManagerがStartで接続を始めるのでこちらに含める)
                Show("Connecting…", _theme.Warning);
                break;
        }
    }

    private void Show(string text, Color dotColor)
    {
        if (_label != null) _label.text = text;
        if (_dot != null) _dot.color = dotColor;
    }
}
