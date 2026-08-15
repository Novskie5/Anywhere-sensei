using UnityEngine;
using Photon.Pun;

// 腕から先のポーズ/ジェスチャーはAnimator Controller側に移行したため、
// ここでは胸・肩の呼吸ゆらぎのみを担当する
public class PoseController : MonoBehaviourPun
{
    [SerializeField] private Transform _chest;
    private Quaternion _chestDefaultRot;

    [Header("Breathing (chest必須、shoulderは任意)")]
    [SerializeField] private float _breathSpeed = 0.25f;      // Perlinノイズを進める速さ
    [SerializeField] private float _breathAmplitude = 2.5f;   // 度数、揺れの最大幅
    [SerializeField] private Transform _shoulderL;
    [SerializeField] private Transform _shoulderR;
    private Quaternion _shoulderLDefaultRot;
    private Quaternion _shoulderRDefaultRot;

    private void Start()
    {
        if (_chest != null)
            _chestDefaultRot = _chest.localRotation;
        if (_shoulderL != null)
            _shoulderLDefaultRot = _shoulderL.localRotation;
        if (_shoulderR != null)
            _shoulderRDefaultRot = _shoulderR.localRotation;
    }

    private void Update()
    {
        UpdateBreathing();
    }

    // Sin波だけだと毎周期まったく同じ動きになるので、Perlinノイズで速さ・深さに揺らぎを持たせている
    private void UpdateBreathing()
    {
        float t = Time.time * _breathSpeed;

        if (_chest != null)
        {
            float noise = Mathf.PerlinNoise(t, 0f) - 0.5f; // -0.5〜0.5
            float breath = noise * _breathAmplitude * 2f;
            _chest.localRotation = _chestDefaultRot * Quaternion.Euler(breath, 0, 0);
        }

        // 肩は左右で違う種(seed)を使い、chestと同じ動きにならないようにしている
        if (_shoulderL != null)
        {
            float noiseL = Mathf.PerlinNoise(t, 10f) - 0.5f;
            _shoulderL.localRotation = _shoulderLDefaultRot * Quaternion.Euler(0, 0, noiseL * _breathAmplitude);
        }
        if (_shoulderR != null)
        {
            float noiseR = Mathf.PerlinNoise(t, 20f) - 0.5f;
            _shoulderR.localRotation = _shoulderRDefaultRot * Quaternion.Euler(0, 0, -noiseR * _breathAmplitude);
        }
    }
}