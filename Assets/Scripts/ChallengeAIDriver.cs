using UnityEngine;

/// <summary>
/// ChallengeAIDriver:
/// 1. Oyunu başlatınca arabayı tam gaz rampaya doğru sürer.
/// 2. Havadayken ve yoldayken harici ses dosyasına ihtiyaç duymadan
///    matematiksel/prosedürel motor sesi (RPM tabanlı), rüzgar ve çarpma sesi üretir.
/// </summary>
[RequireComponent(typeof(ArcadeCar))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class ChallengeAIDriver : MonoBehaviour
{
    [Header("AI Sürüş Ayarları")]
    public float startDelay = 0.5f;
    public float targetTopSpeedKmh = 190f;
    public float airThresholdY = 2.5f;

    [Header("Motor Sesi Ayarları")]
    [Range(0f, 1f)] public float masterVolume = 0.8f;
    public float idleRpmFreq = 55f;
    public float maxRpmFreq = 260f;

    private ArcadeCar arcadeCar;
    private Rigidbody rb;
    private AudioSource audioSource;

    private enum State { Waiting, Driving, InAir, Crashed }
    private State currentState = State.Waiting;
    private float timer = 0f;

    // Prosedürel ses sentezi değişkenleri
    private float sampleRate = 48000f;
    private float phaseMain = 0f;
    private float phaseSub = 0f;
    private float phaseExhaust = 0f;
    private float currentFreq = 50f;
    private float crashNoiseTimer = 0f;
    private float windNoiseLevel = 0f;

    void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
    }

    void Start()
    {
        arcadeCar = GetComponent<ArcadeCar>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Oyuncu klavye girişlerini kapat, AI sürsün
        arcadeCar.controllable = false;

        // AudioSource ayarları
        if (audioSource != null)
        {
            audioSource.playOnAwake = true;
            audioSource.spatialBlend = 0f; // 2D net ses
            audioSource.volume = 1f;
            audioSource.Play();
        }

        Debug.Log("🏎️ [ChallengeAIDriver] Yapay Zeka Sürücü ve Prosedürel Ses Motoru Devrede!");
    }

    void Update()
    {
        timer += Time.deltaTime;
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        // State machine
        switch (currentState)
        {
            case State.Waiting:
                if (timer >= startDelay)
                {
                    currentState = State.Driving;
                    Debug.Log("🚀 [AI Sürücü] Gaz Kökleniyor! Hedef: Rampa ve Dev Karakter!");
                }
                break;

            case State.Driving:
                if (transform.position.y > airThresholdY)
                {
                    currentState = State.InAir;
                    Debug.Log($"🦅 [AI Sürücü] Araç Havalandı! Hız: {speedKmh:F1} km/h");
                }
                break;

            case State.InAir:
                windNoiseLevel = Mathf.Lerp(windNoiseLevel, 0.4f, Time.deltaTime * 3f);
                break;

            case State.Crashed:
                windNoiseLevel = Mathf.Lerp(windNoiseLevel, 0f, Time.deltaTime * 5f);
                break;
        }

        // Ses frekansı (RPM) hesapla
        float speedRatio = Mathf.Clamp01(speedKmh / targetTopSpeedKmh);
        float targetFreq = (currentState == State.Waiting) 
            ? idleRpmFreq 
            : Mathf.Lerp(idleRpmFreq, maxRpmFreq, Mathf.Pow(speedRatio, 0.8f));
        
        currentFreq = Mathf.Lerp(currentFreq, targetFreq, Time.deltaTime * 6f);

        if (crashNoiseTimer > 0f)
        {
            crashNoiseTimer -= Time.deltaTime;
        }
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

            // Güçlü ivmelenme kuvveti
            float speedDiff = targetSpeedMs - speed;
            if (speedDiff > 0)
            {
                float accelForce = Mathf.Clamp(speedDiff * rb.mass * 8f, 0f, rb.mass * 120f);
                rb.AddForce(fwd * accelForce, ForceMode.Force);
            }

            // Şeritten sapmayı engelle (tam merkezde rampaya girmesini sağla)
            float xOffset = transform.position.x;
            rb.AddForce(new Vector3(-xOffset * rb.mass * 10f, 0f, 0f), ForceMode.Force);
        }
        else if (currentState == State.InAir)
        {
            // Havadayken araba aerodinamik olarak burnunu korusun
            Vector3 currentUp = transform.up;
            Vector3 desiredUp = Vector3.up;
            Vector3 rotAxis = Vector3.Cross(currentUp, desiredUp);
            rb.AddTorque(rotAxis * rb.mass * 20f, ForceMode.Force);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.relativeVelocity.magnitude > 6f)
        {
            crashNoiseTimer = 1.2f;
            if (currentState == State.InAir)
            {
                currentState = State.Crashed;
                Debug.Log($"💥 [AI Sürücü] ÇARPIŞMA! Çarpışma Hızı: {col.relativeVelocity.magnitude * 3.6f:F1} km/h");
            }
        }
    }

    // Unity DSP Sentezleyici: Harici MP3 dosyası olmadan saf motor/egzoz/rüzgar/patlama sesi üretir
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (sampleRate <= 0f) sampleRate = 48000f;
        float mainIncrement = currentFreq * 2f * Mathf.PI / sampleRate;
        float subIncrement = (currentFreq * 0.5f) * 2f * Mathf.PI / sampleRate;
        float exhaustIncrement = (currentFreq * 2.5f) * 2f * Mathf.PI / sampleRate;

        for (int i = 0; i < data.Length; i += channels)
        {
            phaseMain += mainIncrement;
            phaseSub += subIncrement;
            phaseExhaust += exhaustIncrement;

            if (phaseMain > 2f * Mathf.PI) phaseMain -= 2f * Mathf.PI;
            if (phaseSub > 2f * Mathf.PI) phaseSub -= 2f * Mathf.PI;
            if (phaseExhaust > 2f * Mathf.PI) phaseExhaust -= 2f * Mathf.PI;

            // 1. Motor Harmonikleri (Sawtooth & Sine karması)
            float mainWave = Mathf.Sin(phaseMain) * 0.4f;
            float subWave = Mathf.Sin(phaseSub) * 0.3f;
            float saw = ((phaseMain / Mathf.PI) - 1f) * 0.2f;
            float exhaust = Mathf.Sin(phaseExhaust) * 0.15f;

            float engineSignal = (mainWave + subWave + saw + exhaust) * 0.6f;

            // 2. Rüzgar / Hız Uçuş Sesi (Thread-safe saf matematiksel White Noise)
            noiseSeed = (noiseSeed * 196314165 + 907633515);
            float whiteNoise = ((float)(noiseSeed & 0x7FFFFFFF) / 2147483647f) * 2f - 1f;
            float windSignal = whiteNoise * windNoiseLevel;

            // 3. Çarpışma / Patlama Sesi
            float crashSignal = 0f;
            if (crashNoiseTimer > 0f)
            {
                crashSignal = whiteNoise * (crashNoiseTimer / 1.2f) * 0.9f;
            }

            float finalSample = (engineSignal + windSignal + crashSignal) * masterVolume;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = finalSample;
            }
        }
    }
    private uint noiseSeed = 123456789;
}
