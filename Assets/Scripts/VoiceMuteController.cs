using Photon.Voice.PUN;
using TMPro;
using UnityEngine;

// UIボタンのOnClickから叩く、自分のマイクのミュート切り替え。
// 録音(RecordingEnabled)は止めずに送信(TransmitEnabled)だけ止めるので、解除したときにマイクの再初期化待ちが発生しない
// Main_Tracking(先生)とStudent_AR(生徒)の両方で同じものを使う
public class VoiceMuteController : MonoBehaviour
{
    // Font Awesome 5 Solid の microphone / microphone-slash
    private const string MicOnIcon = "";
    private const string MicOffIcon = "";

    [SerializeField] private TMP_Text _label;       // 任意。ボタンの文字を状態に合わせて書き換える
    [SerializeField] private TMP_Text _icon;        // 任意。マイクのアイコン
    [SerializeField] private ThemedButton _button;  // 任意。ミュート中は赤にして、文字を読まなくても状態が分かるようにする

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

    // 文字は「押すとどうなるか」ではなく「今どうなっているか」を出す(色・アイコンと意味を揃える)
    private void UpdateLabel()
    {
        if (_label != null) _label.text = _muted ? "Muted" : "Mic On";
        if (_icon != null) _icon.text = _muted ? MicOffIcon : MicOnIcon;
        if (_button != null) _button.SetVariant(_muted ? ThemedButton.Variant.Danger : ThemedButton.Variant.Secondary);
    }
}
