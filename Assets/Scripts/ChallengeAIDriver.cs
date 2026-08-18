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
    public float startDelay = 0.1f; // Anında kalkış
    public float targetTopSpeedKmh = 400f; // 400 km/h Hiper Hız!
    public float airThresholdY = 2.5f;

    [Header("Gerçek Motor Sesleri (i6 German)")]
    public AudioClip startClip;
    public AudioClip idleClip;
    public AudioClip lowClip;
    public AudioClip medClip;
    public AudioClip highClip;
    public AudioClip maxRpmClip;

    [Range(0f, 1f)] public float masterVolume = 0.95f;

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

        if (rb != null)
        {
            // Unity'nin varsayılan hız limitini kaldır (350 m/s = 1260 km/h kapasite)
            rb.maxLinearVelocity = 350f;
            rb.linearDamping = 0f;
        }

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
                    Debug.Log("🚀 [i6 German AI] 400 KM/H HİPER HIZ LANSMANI BAŞLADI!");
                }
                break;

            case State.Driving:
                // Sadece rampanın en tepesinden (Z >= 95) fırladıktan sonra uçuş moduna geç
                if (transform.position.z >= 95f)
                {
                    currentState = State.InAir;
                    Debug.Log($"🦅 [i6 German AI] 400 KM/H İLE UÇUŞ BAŞLADI! Hız: {speedKmh:F1} km/h");
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
            float targetSpeedMs = targetTopSpeedKmh / 3.6f; // 111.11 m/s = 400 km/h
            float accelRate = 75f; // m/s^2 hiper ivmelenme
            float curZ = transform.position.z;

            // 1. DÜZ OTOBANDA (Rampadan Önce: Z < 40) -> ASLA HAVALANMAZ, YERE YAPIŞIK
            if (curZ < 40f)
            {
                float curFwdSpeed = rb.linearVelocity.z;
                float newFwdSpeed = Mathf.MoveTowards(curFwdSpeed, targetSpeedMs, accelRate * Time.fixedDeltaTime);

                // Yön kesinlikle düz ileri, yere çarpıp takılmaması için fizik motorunun Y hızını koru
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, newFwdSpeed);

                // Arabanın burnunu düz tut (asla havalanmasın)
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                // Şeridi tam ortala
                Vector3 pos = transform.position;
                pos.x = Mathf.Lerp(pos.x, 0f, Time.fixedDeltaTime * 10f);
                transform.position = pos;
            }
            // 2. RAMPADA (40 <= Z < 95) -> TAM 18 DERECE EĞİMLE TIRMAN
            else
            {
                Vector3 rampDir = Quaternion.Euler(-18f, 0f, 0f) * Vector3.forward;
                float curRampSpeed = Vector3.Dot(rb.linearVelocity, rampDir);
                float newRampSpeed = Mathf.MoveTowards(curRampSpeed, targetSpeedMs, accelRate * Time.fixedDeltaTime);

                rb.linearVelocity = rampDir * newRampSpeed;
                transform.rotation = Quaternion.Euler(-18f, 0f, 0f);
            }
        }
        else if (currentState == State.InAir)
        {
            // Havadayken füze gibi burnunu hedefe kitle ve aerodinamik olarak stabil tut
            Vector3 currentUp = transform.up;
            Vector3 desiredUp = Vector3.up;
            Vector3 rotAxis = Vector3.Cross(currentUp, desiredUp);
            rb.AddTorque(rotAxis * rb.mass * 45f, ForceMode.Force);
        }
    }

    void OnGUI()
    {
        // Ekranda büyük ve net dijital hız göstergesi
        float speedKmh = rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
        GUIStyle speedStyle = new GUIStyle();
        speedStyle.fontSize = 38;
        speedStyle.fontStyle = FontStyle.Bold;
        speedStyle.normal.textColor = (speedKmh >= 380f) ? Color.red : Color.green;

        GUI.Label(new Rect(40, Screen.height - 90, 400, 70), $"HIZ: {speedKmh:F0} KM/H", speedStyle);
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
