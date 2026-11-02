using Photon.Pun;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEditor;
using UnityEngine;

// メニュー「AnywhereSensei」から、アバターのプレハブを組み立てる/ボイス用の部品を足すEditor拡張。
// YAMLを手で書き換えたりInspectorで一個ずつ付けたりする代わりに、何度でも同じ状態で作り直せるようにしている
public static class AvatarPrefabBuilder
{
    private const string RobotPrefabPath = "Assets/Resources/StudentRobot.prefab";
    private const string TeacherPrefabPath = "Assets/Resources/VRM1.0TestAv.prefab";
    private const string MaterialFolder = "Assets/Materials/StudentRobot";

    // 生徒用の小さな浮遊ロボット。プリミティブの組み合わせだけで作るので外部モデル不要
    [MenuItem("AnywhereSensei/Build Student Robot Prefab")]
    public static void BuildStudentRobot()
    {
        Material bodyMat = GetOrCreateMaterial("RobotBody", "Universal Render Pipeline/Lit", Color.white);
        Material darkMat = GetOrCreateMaterial("RobotDark", "Universal Render Pipeline/Lit", new Color(0.12f, 0.12f, 0.15f));
        Material glowMat = GetOrCreateMaterial("RobotGlow", "Universal Render Pipeline/Unlit", Color.white);

        var root = new GameObject("StudentRobot");
        var body = new GameObject("Body").transform;
        body.SetParent(root.transform, false);

        // 頭(兼胴体)の箱。正面は+Z
        Renderer head = AddPart(PrimitiveType.Cube, "Head", body, new Vector3(0f, 0f, 0f), new Vector3(0.26f, 0.2f, 0.2f), bodyMat);
        AddPart(PrimitiveType.Cube, "FacePlate", body, new Vector3(0f, 0f, 0.1f), new Vector3(0.21f, 0.12f, 0.01f), darkMat);
        Renderer eyeL = AddPart(PrimitiveType.Sphere, "EyeL", body, new Vector3(-0.05f, 0.005f, 0.106f), Vector3.one * 0.045f, glowMat);
        Renderer eyeR = AddPart(PrimitiveType.Sphere, "EyeR", body, new Vector3(0.05f, 0.005f, 0.106f), Vector3.one * 0.045f, glowMat);
        // 耳(左右の円盤)
        Renderer earL = AddPart(PrimitiveType.Cylinder, "EarL", body, new Vector3(-0.14f, 0f, 0f), new Vector3(0.07f, 0.015f, 0.07f), bodyMat);
        earL.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Renderer earR = AddPart(PrimitiveType.Cylinder, "EarR", body, new Vector3(0.14f, 0f, 0f), new Vector3(0.07f, 0.015f, 0.07f), bodyMat);
        earR.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        // アンテナ
        AddPart(PrimitiveType.Cylinder, "AntennaStick", body, new Vector3(0f, 0.13f, 0f), new Vector3(0.012f, 0.03f, 0.012f), darkMat);
        Renderer antennaBall = AddPart(PrimitiveType.Sphere, "AntennaBall", body, new Vector3(0f, 0.175f, 0f), Vector3.one * 0.04f, glowMat);
        // 浮遊ユニット(下側のつぶれた球)
        AddPart(PrimitiveType.Sphere, "Thruster", body, new Vector3(0f, -0.12f, 0f), new Vector3(0.13f, 0.05f, 0.13f), darkMat);

        // ネットワーク・ボイス
        var photonView = root.AddComponent<PhotonView>();
        photonView.Synchronization = ViewSynchronization.Off; // 位置は各クライアントが席から計算するので送らない
        root.AddComponent<PhotonVoiceView>();
        AudioSource audio = root.AddComponent<AudioSource>();
        ConfigureSpatialAudio(audio);
        root.AddComponent<Speaker>();

        var robot = root.AddComponent<StudentRobot>();
        var so = new SerializedObject(robot);
        so.FindProperty("_body").objectReferenceValue = body;
        SetArray(so.FindProperty("_tintRenderers"), head, earL, earR);
        SetArray(so.FindProperty("_glowRenderers"), eyeL, eyeR, antennaBall);
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, RobotPrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[AvatarPrefabBuilder] {RobotPrefabPath} を作成しました");
    }

    // 先生のVRMプレハブに、声をアバターの位置から鳴らすための部品(PhotonVoiceView/Speaker/AudioSource)を足す
    [MenuItem("AnywhereSensei/Add Voice To Teacher Prefab")]
    public static void AddVoiceToTeacher()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(TeacherPrefabPath);
        try
        {
            if (root.GetComponent<PhotonVoiceView>() == null) root.AddComponent<PhotonVoiceView>();
            AudioSource audio = root.GetComponent<AudioSource>();
            if (audio == null) audio = root.AddComponent<AudioSource>();
            ConfigureSpatialAudio(audio);
            if (root.GetComponent<Speaker>() == null) root.AddComponent<Speaker>();

            PrefabUtility.SaveAsPrefabAsset(root, TeacherPrefabPath);
            Debug.Log($"[AvatarPrefabBuilder] {TeacherPrefabPath} にボイス用コンポーネントを追加しました");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ARで声が「そのアバターの方向から」聞こえるよう3Dにする
    private static void ConfigureSpatialAudio(AudioSource audio)
    {
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;
        audio.minDistance = 1f;
        audio.maxDistance = 20f;
    }

    private static Renderer AddPart(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>()); // 当たり判定は不要(ARのタップ配置のRaycastにも引っかからないように)
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        return renderer;
    }

    private static Material GetOrCreateMaterial(string name, string shaderName, Color color)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Materials", "StudentRobot");

        material = new Material(Shader.Find(shaderName));
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void SetArray(SerializedProperty property, params Renderer[] renderers)
    {
        property.arraySize = renderers.Length;
        for (int i = 0; i < renderers.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        }
    }
}
