using Photon.Voice.PUN;
using TMPro;
using UnityEngine;

// UIボタンのOnClickから叩く、自分のマイクのミュート切り替え。
// 録音(RecordingEnabled)は止めずに送信(TransmitEnabled)だけ止めるので、解除したときにマイクの再初期化待ちが発生しない
// Main_Tracking(先生)とStudent_AR(生徒)の両方で同じものを使う
public class VoiceMuteController : MonoBehaviour
{
    [SerializeField] private TMP_Text _label; // 任意。ボタンの文字を状態に合わせて書き換える

    private bool _muted;

    private void Start()
    {
        UpdateLabel();
    }

    public void ToggleMute()
    {
        SetMuted(!_muted);
    }

    public void SetMuted(bool muted)
    {
        PunVoiceClient voiceClient = PunVoiceClient.Instance;
        if (voiceClient == null || voiceClient.PrimaryRecorder == null) return;

        _muted = muted;
        voiceClient.PrimaryRecorder.TransmitEnabled = !muted;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (_label == null) return;
        // TMPのデフォルトフォント(LiberationSans)は日本語グリフを持たないので英語表記にしている
        _label.text = _muted ? "Mic OFF" : "Mic ON";
    }
}
