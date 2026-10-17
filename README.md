# Anywhere Sensei

Webカメラだけで先生の表情・姿勢をリアルタイムにトラッキングし、ネットワーク越しに同期したVRMアバターとして、生徒側はスマホのARでその場にいるかのように見られる — というUnity製の遠隔授業向けプロトタイプです。

## デモ

<!-- TODO: ホスト側(PC)とAR側(スマホ)を並べたGIFまたは動画をここに貼る -->

## できること

- **PC(先生役)**: Webカメラ映像から [MediaPipeUnityPlugin](https://github.com/homuler/MediaPipeUnityPlugin) で顔のランドマークを抽出し、表情・まばたき・頭部の回転をVRMアバターにリアルタイム反映
- **ネットワーク同期**: [Photon PUN 2](https://www.photonengine.com/pun) を使い、トラッキング結果を他クライアントへ配信
- **スマホ(生徒役)**: ARCore対応Android端末で `Student_AR` シーンを起動すると、タップした位置に先生のアバターをAR配置。常にカメラの方を向くビルボード処理付きで、その場に立っているように見える
- **ジェスチャー**: Mixamoから取り込んだHumanoidモーションをAnimator Controller + Avatar Maskで腕/手だけに適用し、顔トラッキング(頭部)や呼吸ゆらぎ(胸/肩)と競合しないように分離

## アーキテクチャ

```
[PC / Webcam]                         [Android / ARCore]
Main_Tracking.unity                   Student_AR.unity
  MediaPipe (顔ランドマーク)                 │
       │                                     │
       ▼                                     ▼
   FaceSync.cs  ──────── Photon Room ────────  StudentARManager.cs
   (表情/まばたき/頭部回転)   (hostActorNrで      (アバター生成 + AR配置 +
       │                     教師を特定)         ビルボード追従)
       ▼
  PoseController.cs (呼吸ゆらぎ)
  GestureController (Animator, 腕/手のモーション)
```

## 技術的な見どころ

- **回転同期の隠れバグ**: `FaceSync.cs` の `OnPhotonSerializeView` が送受信していたフィールドと、実際に `Update()` で参照しているフィールドが一致しておらず、頭部回転がリモートクライアントに一切反映されない状態が長く続いていた。フィールド名の一致だけでは検知できない典型的な同期バグ。
- **PUNコールバックの罠**: `MonoBehaviourPunCallbacks` を継承したクラスが `override` なしで独自の `OnEnable`/`OnDisable` を定義すると、PUNのコールバック登録自体が黙って上書きされ、接続は成功しているのに `OnJoinedRoom` などが一切発火しなくなる。原因特定にIL2CPPビルドキャッシュ破損を疑うなど遠回りしたが、最終的にコード側の問題と判明。
- **AnimatorレイヤーとFaceSyncの競合**: Animatorの評価は `Update()` の後・`LateUpdate()` の前に走るため、フルボディのMixamoモーションをそのまま流すとMediaPipe由来の頭部回転を毎フレーム上書きしてしまう。Avatar Maskで腕/手/指のみに範囲を絞ることでコード変更なしに解決。
- **「教師は誰か」問題**: `PhotonNetwork.MasterClient` ではなく Room の Custom Property (`hostActorNr`) で教師を判定。ARの生徒がホストより先にルームへ参加/作成し得るケースを考慮した設計。

## 技術スタック

- Unity 2022.3.62f3
- [MediaPipeUnityPlugin](https://github.com/homuler/MediaPipeUnityPlugin)(顔トラッキング)
- Photon PUN 2 (v2.52)(ネットワーク同期)
- AR Foundation / ARCore(Android AR)
- UniVRM(VRMアバターの読み込み・制御)

## セットアップ

1. Unity 2022.3.62f3 でプロジェクトを開く
2. **ホスト側**: `Assets/Scenes/Main_Tracking.unity` を実行し、Webカメラで顔トラッキングを開始
3. **生徒側**: `Assets/Scenes/Student_AR.unity` をAndroid(ARCore対応端末)向けにビルドし、同じPhotonルームに参加

## 今後やりたいこと

- Mixamoモーションの追加によるジェスチャーの拡充
- 生徒側アバターの見た目を教師と差別化
- 複数生徒が参加した際のAR配置レイアウト調整
