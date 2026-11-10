using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class AnimatorController : MonoBehaviourPun
{
    Animator animator;
    string[] idleTriggers = {"Idle1", "Idle2", "Idle3"};
    string[] gestureTriggers = {"Clapping", "Bye", "Cute"};
    bool isIdle = true;

    // ジェスチャー再生中のボタンを光らせるのに使う(GestureUIController参照)
    public bool IsIdle => isIdle;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        // 立ち位置・向きはコード(AR配置やPhoton)側で決めるので、モーションのルート移動で動かさない
        animator.applyRootMotion = false;
        StartCoroutine(RandomIdleTrigger());
    }

    // 抽選は持ち主だけが行いRPCで配る。各端末で個別に乱数を引くと先生とAR視聴側でIdleの種類がずれるため
    IEnumerator RandomIdleTrigger()
    {
        while (true)
        {
            if (isIdle && photonView != null && photonView.IsMine)
            {
                int randomIndex = Random.Range(0, idleTriggers.Length);
                photonView.RPC(nameof(ApplyIdleRpc), RpcTarget.All, randomIndex);
            }
            yield return new WaitForSeconds(Random.Range(5f, 10f));
        }
    }

    [PunRPC]
    void ApplyIdleRpc(int idleIndex)
    {
        animator.SetTrigger(idleTriggers[idleIndex]);
    }

    // Idleトリガーの遷移はHas Exit Time付きなので、消費される前にジェスチャーが始まると
    // 残ったトリガーがジェスチャーの途中(Exit Time 0.75)で発火してIdleに引き戻してしまう。切り替え時に捨てておく
    void ResetIdleTriggers()
    {
        foreach (string trigger in idleTriggers)
        {
            animator.ResetTrigger(trigger);
        }
    }

    // UIのボタン等から呼ぶ想定 (gestureTriggersのインデックスを渡す)
    // Any Stateからの遷移なので、別のトリガー(Idleやほかのジェスチャー)が来れば
    // 今何を再生中でも自動的に切り替わる。Cuteはポーズなので、それ以外の終了処理は不要
    // 先生側の入力なので自分のアバターでだけ受け付け、AR視聴側にもRPCで反映する
    public void PlayGesture(int gestureIndex)
    {
        if (photonView == null || !photonView.IsMine) return;
        photonView.RPC(nameof(ApplyGestureRpc), RpcTarget.All, gestureIndex);
    }

    [PunRPC]
    void ApplyGestureRpc(int gestureIndex)
    {
        ResetIdleTriggers();
        animator.SetTrigger(gestureTriggers[gestureIndex]);
    }

    // Cuteなど戻り遷移の無いポーズから抜けるためのもの
    // Idleトリガーの遷移はHas Exit Time付きで即座に切り替わらないので、トリガーを使わずステートへ直接クロスフェードする
    public void ReturnToIdle()
    {
        if (photonView == null || !photonView.IsMine) return;
        photonView.RPC(nameof(ReturnToIdleRpc), RpcTarget.All);
    }

    [PunRPC]
    void ReturnToIdleRpc()
    {
        ResetIdleTriggers();
        animator.CrossFadeInFixedTime("Idle1", 0.5f, 1);
    }

    // Update is called once per frame
    void Update()
    {
        // Idle/ジェスチャーのステートは"Gesture Layer"(レイヤー1)にあるので、そこを見る
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(1);
        isIdle = stateInfo.IsTag("Idle");
    }
}
