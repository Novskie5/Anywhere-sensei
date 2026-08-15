using System;
using UnityEngine;
using UniVRM10;
using Photon.Pun;

public class FaceSync : MonoBehaviourPun, IPunObservable, IPunInstantiateMagicCallback
{
    // AR視聴側などが「誰かのアバターがスポーンした」を検知するためのイベント
    // 遅れて入室したクライアントでも、Photonが既存ネットワークオブジェクトを
    // 再生成する際にOnPhotonInstantiateが呼ばれるので、参加タイミングに依存しない
    public static event Action<FaceSync> OnAnySpawned;

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        OnAnySpawned?.Invoke(this);
    }

    [SerializeField] private Vrm10Instance _instance;
    [SerializeField] private Transform _headBone;

    private Quaternion _targetRotation = Quaternion.identity;
    private float _blinkL, _blinkR;
    private float _aa, _uu, _ee, _oo; //あいうえお いを実装したときにfloat _iiも追加する　いは笑顔になっちゃうので実装するかは未定
    private float _smile, _surprised, _angry;

    private bool _isMine;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // 自分→送信
        {
            stream.SendNext(_blinkL);
            stream.SendNext(_blinkR);
            stream.SendNext(_aa);
            stream.SendNext(_uu);
            stream.SendNext(_ee);
            stream.SendNext(_oo);
            stream.SendNext(_smile);
            stream.SendNext(_surprised);
            stream.SendNext(_angry);
            stream.SendNext(_targetRotation);
        }
        else // 相手→受信
        {
            _blinkL        = (float)stream.ReceiveNext();
            _blinkR        = (float)stream.ReceiveNext();
            _aa            = (float)stream.ReceiveNext();
            _uu            = (float)stream.ReceiveNext();
            _ee            = (float)stream.ReceiveNext();
            _oo            = (float)stream.ReceiveNext();
            _smile         = (float)stream.ReceiveNext();
            _surprised     = (float)stream.ReceiveNext();
            _angry         = (float)stream.ReceiveNext();
            _targetRotation = (Quaternion)stream.ReceiveNext();
        }
    }

    private void Start()
    {
        if (photonView == null) return;
        _isMine = photonView.IsMine;
    }

    private void Update()
    {
        if (_headBone == null) return;
        _headBone.localRotation = Quaternion.Slerp(_headBone.localRotation, _targetRotation, 0.1f);

        if (_instance == null) return;
        var expression = _instance.Runtime.Expression;
        expression.SetWeight(ExpressionKey.BlinkLeft, _blinkL);
        expression.SetWeight(ExpressionKey.BlinkRight, _blinkR);
        expression.SetWeight(ExpressionKey.Aa, _aa);
        expression.SetWeight(ExpressionKey.Ou, _uu);
        expression.SetWeight(ExpressionKey.Ee, _ee);
        expression.SetWeight(ExpressionKey.Oh, _oo);
        expression.SetWeight(ExpressionKey.Happy, _smile);
        expression.SetWeight(ExpressionKey.Surprised, _surprised);
        expression.SetWeight(ExpressionKey.Angry, _angry);
    }

    // 各種アップデート窓口 (Runnerから呼ばれる)
    public void UpdateRotation(Quaternion rotation)
    {
        if (!_isMine) return;
        _targetRotation = rotation;
    }

    public void UpdateMouth(float a, float u, float e, float o) //いを実装したときに引数にfloat iを追加してね
    {
        if (!_isMine) return;
        _aa = a;
        _uu = u;
        _ee = e;
        _oo = o;
    }

    public void UpdateBlink(float left, float right)
    {
        if (!_isMine) return;
        _blinkL = left;
        _blinkR = right;
    }

    public void UpdateExpression(float smile, float surprised, float angry)
    {
        if (!_isMine) return;
        _smile = smile;
        _surprised = surprised;
        _angry = angry;
    }
}
