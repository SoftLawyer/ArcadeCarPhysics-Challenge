using UnityEngine;

/// <summary>
/// ChallengeAIDriver:
/// 1. i6 German gerçek stüdyo kayıtlı motor seslerini (Idle, Low, Med, High, MaxRPM, Startup)
///    arabanın devrine ve hızına göre gerçek zamanlı cross-fade ve pitch ile çalar.
/// 2. Oyunu başlatınca arabayı tam gaz rampaya doğru sürer ve hedefe fırlatır.
/// </summary>
[RequireComponent(typeof(ArcadeCar))]
[RequireComponent(typeof(Rigidbody))]
public class ChallengeAIDriver : MonoBehaviour
{
    [Header("AI Sürüş Ayarları")]
    public float startDelay = 0.2f; // Anında kalkış
    public float targetTopSpeedKmh = 260f; // 2 kat daha yüksek maksimum hız
    public float airThresholdY = 2.5f;

    [Header("Gerçek Motor Sesleri (i6 German)")]
    public AudioClip startClip;
    public AudioClip idleClip;
    public AudioClip lowClip;
    public AudioClip medClip;
    public AudioClip highClip;
    public AudioClip maxRpmClip;

    [Range(0f, 1f)] public float masterVolume = 0.9f;

    private ArcadeCar arcadeCar;
    private Rigidbody rb;

    // Çok kanallı gerçek motor ses kaynakları (AudioSource Layering)
    private AudioSource srcStartup;
    private AudioSource srcIdle;
    private AudioSource srcLow;
    private AudioSource srcMed;
    private AudioSource srcHigh;
    private AudioSource srcMaxRpm;

    private enum State { Waiting, Driving, InAir, Crashed }
    private State currentState = State.Waiting;
    private float timer = 0f;
    private float virtualRpmRatio = 0f;

    void Awake()
    {
        arcadeCar = GetComponent<ArcadeCar>();
        rb = GetComponent<Rigidbody>();

        // Oyuncu klavye girişlerini kapat, AI sürsün
        if (arcadeCar != null)
        {
            arcadeCar.controllable = false;
        }

        SetupAudioSources();
    }

    void SetupAudioSources()
    {
        // Ses kaynaklarını oluştur
        srcStartup = CreateAudioSource("Audio_Startup", false);
        srcIdle = CreateAudioSource("Audio_Idle", true);
        srcLow = CreateAudioSource("Audio_Low", true);
        srcMed = CreateAudioSource("Audio_Med", true);
        srcHigh = CreateAudioSource("Audio_High", true);
        srcMaxRpm = CreateAudioSource("Audio_MaxRPM", true);

        // Klipleri ata
        if (srcStartup) srcStartup.clip = startClip;
        if (srcIdle) srcIdle.clip = idleClip;
        if (srcLow) srcLow.clip = lowClip;
        if (srcMed) srcMed.clip = medClip;
        if (srcHigh) srcHigh.clip = highClip;
        if (srcMaxRpm) srcMaxRpm.clip = maxRpmClip;

        // Döngüleri başlat (başta sessiz)
        if (srcIdle && idleClip) { srcIdle.volume = masterVolume; srcIdle.Play(); }
        if (srcLow && lowClip) { srcLow.volume = 0f; srcLow.Play(); }
        if (srcMed && medClip) { srcMed.volume = 0f; srcMed.Play(); }
        if (srcHigh && highClip) { srcHigh.volume = 0f; srcHigh.Play(); }
        if (srcMaxRpm && maxRpmClip) { srcMaxRpm.volume = 0f; srcMaxRpm.Play(); }

        if (srcStartup && startClip)
        {
            srcStartup.volume = masterVolume;
            srcStartup.Play();
        }
    }

    private AudioSource CreateAudioSource(string name, bool loop)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform);
        child.transform.localPosition = Vector3.zero;
        AudioSource src = child.AddComponent<AudioSource>();
        src.loop = loop;
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D net ses
        return src;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        switch (currentState)
        {
            case State.Waiting:
                if (timer >= startDelay)
                {
                    currentState = State.Driving;
                    Debug.Log("🚀 [i6 German AI] ROKET KALKIŞ! 2X Hızlanma Başladı...");
                }
                break;

            case State.Driving:
                if (transform.position.y > airThresholdY)
                {
                    currentState = State.InAir;
                    Debug.Log($"🦅 [i6 German AI] Uçuş Başladı! Hız: {speedKmh:F1} km/h");
                }
                break;

            case State.InAir:
                break;

            case State.Crashed:
                break;
        }

        UpdateEngineAudio(speedKmh);
    }

    void FixedUpdate()
    {
        if (currentState == State.Driving)
        {
            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            fwd.Normalize();

            float speed = rb.linearVelocity.magnitude;
            float targetSpeedMs = targetTopSpeedKmh / 3.6f;

            // 2 Kat Güçlü ve Patlayıcı İvmelenme (Twin Turbo Roket Hızı)
            float speedDiff = targetSpeedMs - speed;
            if (speedDiff > 0)
            {
                float accelForce = Mathf.Clamp(speedDiff * rb.mass * 24f, 0f, rb.mass * 320f);
                rb.AddForce(fwd * accelForce, ForceMode.Force);
            }

            // Şeritten sağa sola sapmayı önle (Rampayı tam ortala)
            float xOffset = transform.position.x;
            rb.AddForce(new Vector3(-xOffset * rb.mass * 15f, 0f, 0f), ForceMode.Force);
        }
        else if (currentState == State.InAir)
        {
            // Havadayken burnunu hedef doğrultusunda tut
            Vector3 currentUp = transform.up;
            Vector3 desiredUp = Vector3.up;
            Vector3 rotAxis = Vector3.Cross(currentUp, desiredUp);
            rb.AddTorque(rotAxis * rb.mass * 30f, ForceMode.Force);
        }
    }

    void UpdateEngineAudio(float speedKmh)
    {
        float targetRatio = Mathf.Clamp01(speedKmh / targetTopSpeedKmh);
        
        // Vites geçişi hissi veren dinamik devir eğrisi
        float gearFactor = (targetRatio * 4f) % 1f;
        float combinedRpm = Mathf.Lerp(targetRatio, gearFactor, 0.35f);
        virtualRpmRatio = Mathf.Lerp(virtualRpmRatio, (currentState == State.Waiting) ? 0f : combinedRpm, Time.deltaTime * 5f);

        float pitchModifier = Mathf.Lerp(0.85f, 1.35f, virtualRpmRatio);

        // Katmanlı ses harmanlama (Cross-Fade)
        // 0.0 - 0.25: Idle -> Low
        // 0.25 - 0.55: Low -> Med
        // 0.55 - 0.85: Med -> High
        // 0.85 - 1.00: High -> MaxRPM

        float wIdle = Mathf.Clamp01(1f - (targetRatio * 3.5f));
        float wLow = Mathf.Clamp01(1f - Mathf.Abs(targetRatio - 0.25f) * 4f);
        float wMed = Mathf.Clamp01(1f - Mathf.Abs(targetRatio - 0.55f) * 3.5f);
        float wHigh = Mathf.Clamp01(1f - Mathf.Abs(targetRatio - 0.80f) * 3.5f);
        float wMax = Mathf.Clamp01((targetRatio - 0.75f) * 4f);

        if (srcIdle) { srcIdle.volume = wIdle * masterVolume; srcIdle.pitch = Mathf.Lerp(0.9f, 1.15f, targetRatio); }
        if (srcLow) { srcLow.volume = wLow * masterVolume; srcLow.pitch = pitchModifier; }
        if (srcMed) { srcMed.volume = wMed * masterVolume; srcMed.pitch = pitchModifier; }
        if (srcHigh) { srcHigh.volume = wHigh * masterVolume; srcHigh.pitch = pitchModifier; }
        if (srcMaxRpm) { srcMaxRpm.volume = wMax * masterVolume; srcMaxRpm.pitch = Mathf.Lerp(1.0f, 1.25f, targetRatio); }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.relativeVelocity.magnitude > 6f && currentState == State.InAir)
        {
            currentState = State.Crashed;
            Debug.Log($"💥 [i6 German AI] HEDEFE VURULDU! Çarpışma Hızı: {col.relativeVelocity.magnitude * 3.6f:F1} km/h");
        }
    }
}
