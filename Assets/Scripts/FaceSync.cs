using Unity.VisualScripting;
using UnityEngine;
using UniVRM10;

public class FaceSync : MonoBehaviour
{
    [SerializeField] private Vrm10Instance _instance;
    [SerializeField] private Transform _headBone; 
    
    private Quaternion _targetRotation = Quaternion.identity;

    // メインスレッドでの実行 (Update)
    private void Update()
    {
        if (_headBone == null) return;
        // メインスレッドで滑らかに回転を適用する
        _headBone.localRotation = Quaternion.Slerp(_headBone.localRotation, _targetRotation, 0.1f);
    }

    // 各種アップデート窓口 (Runnerから呼ばれる)
    public void UpdateRotation(Quaternion rotation)
    {
        // ここでは変数に代入するだけ
        _targetRotation = rotation;
    }

    public void UpdateMouth(float a,  float u, float e, float o)//いを実装したときに引数にfloat iを追加してね
{
    if (_instance == null) return;
    
    // VRM1.0のExpression管理クラスを取得
    var expression = _instance.Runtime.Expression;

    // それぞれの母音にウェイトを設定
    expression.SetWeight(ExpressionKey.Aa, a);
    //expression.SetWeight(ExpressionKey.Ih, i); いは笑顔になっちゃうから後でロジック考えてくれ
    expression.SetWeight(ExpressionKey.Ou, u);
    expression.SetWeight(ExpressionKey.Ee, e);
    expression.SetWeight(ExpressionKey.Oh, o);
}

    public void UpdateBlink(float left, float right)
    {
        if (_instance == null) return;
        _instance.Runtime.Expression.SetWeight(ExpressionKey.BlinkLeft, left);
        _instance.Runtime.Expression.SetWeight(ExpressionKey.BlinkRight, right);
    }

    public void UpdateExpression(float smile, float surprised, float angry)
    {
        if (_instance == null) return;
        _instance.Runtime.Expression.SetWeight(ExpressionKey.Happy, smile);
        _instance.Runtime.Expression.SetWeight(ExpressionKey.Surprised, surprised);
        _instance.Runtime.Expression.SetWeight(ExpressionKey.Angry, angry);
    }
}