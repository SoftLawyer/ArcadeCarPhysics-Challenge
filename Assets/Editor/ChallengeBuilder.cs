using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ChallengeBuilder v4
/// - Challenge.unity'yi DOKUNMADAN açar (SimplePoly City haritası içinde)
/// - Sahneye rampa, asfalt pist, hedef ve araba/kamera EKLER
/// - Sahneyi kaydeder
/// </summary>
public class ChallengeBuilder : EditorWindow
{
    const float ROAD_W      = 20f;
    const float RAMP_ANGLE  = 18f;
    const float RAMP_LEN    = 60f;
    const float RAMP_W      = 18f;
    const float RAMP_THICK  = 0.15f;
    const float RAMP_START_Z = 40f;

    [MenuItem("Tools/Build Challenge Level")]
    public static void BuildChallengeLevel()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hata!", "Lutfen once Play modundan cikin.", "Tamam");
            return;
        }

        string challengePath = "Assets/Scenes/Challenge.unity";

        // Challenge.unity mevcut mu kontrol et
        if (!System.IO.File.Exists(challengePath))
        {
            EditorUtility.DisplayDialog(
                "Sahne Bulunamadi!",
                "Assets/Scenes/Challenge.unity bulunamadi.\n\n" +
                "Lutfen:\n" +
                "1. File > Open Scene > SimplePoly City Demo Scene\n" +
                "2. File > Save As > Assets/Scenes/Challenge.unity\n" +
                "3. Bu butona tekrar basin.",
                "Tamam");
            return;
        }

        Debug.Log("=================================================================");
        Debug.Log("[CHALLENGE BUILDER v4] SimplePoly City haritasi uzerine stunt track ekleniyor...");

        // Challenge.unity'yi ac (SimplePoly City haritasi icinde)
        Scene challengeScene = EditorSceneManager.OpenScene(challengePath, OpenSceneMode.Single);
        Debug.Log("[1/5] Challenge.unity acildi. SimplePoly City haritasi yuklu.");

        // Sahne nesnelerini logla
        GameObject[] roots = challengeScene.GetRootGameObjects();
        Debug.Log("[1/5] Sahnedeki kok nesne sayisi: " + roots.Length);
        foreach (var r in roots)
            Debug.Log("   - " + r.name);

        // Varsa eski stunt nesnelerini temizle (tekrar calistirildiginda)
        string[] stuntNames = { "Runway_Asphalt", "Sidewalk", "RoadStripes", "Ramp", "GiantTarget",
                                 "LandingArea", "PlayerCar", "ChallengeCamera", "StuntSun" };
        foreach (string sn in stuntNames)
        {
            GameObject old = GameObject.Find(sn);
            if (old != null) Object.DestroyImmediate(old);
        }

        // Varsa eski Challenge kamerasini temizle
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.gameObject.name == "ChallengeCamera")
                Object.DestroyImmediate(cam.gameObject);
        }

        // Gunes: Sahnenin kendi gunes/isigi varsa DOKUNMA, yoksa ekle
        bool hasSun = false;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { hasSun = true; break; }

        if (!hasSun)
        {
            GameObject sunObj = new GameObject("StuntSun");
            Light sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.shadows = LightShadows.Soft;
            sunObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Debug.Log("[2/5] Gunes isigi eklendi (sahne icinde yoktu).");
        }
        else
        {
            Debug.Log("[2/5] SimplePoly City'nin kendi gunesi kullaniliyor.");
        }

        // ─── MATERYALLER ─────────────────────────────────────────────────────
        Material asphaltMat = new Material(Shader.Find("Standard"));
        asphaltMat.color = new Color(0.13f, 0.13f, 0.14f);
        asphaltMat.SetFloat("_Glossiness", 0.15f);

        Material stripeMat = new Material(Shader.Find("Standard"));
        stripeMat.color = new Color(0.95f, 0.95f, 0.80f);
        stripeMat.SetFloat("_Glossiness", 0.1f);

        Material yellowMat = new Material(Shader.Find("Standard"));
        yellowMat.color = new Color(0.95f, 0.78f, 0.05f);
        yellowMat.SetFloat("_Glossiness", 0f);

        Material rampMat = new Material(Shader.Find("Standard"));
        rampMat.color = new Color(0.13f, 0.13f, 0.14f);
        rampMat.SetFloat("_Glossiness", 0.25f);

        Material grassMat = new Material(Shader.Find("Standard"));
        grassMat.color = new Color(0.22f, 0.48f, 0.18f);
        grassMat.SetFloat("_Glossiness", 0.05f);

        Material sidewalkMat = new Material(Shader.Find("Standard"));
        sidewalkMat.color = new Color(0.58f, 0.58f, 0.60f);
        sidewalkMat.SetFloat("_Glossiness", 0.08f);

        Material targetMat = new Material(Shader.Find("Standard"));
        targetMat.color = new Color(0.08f, 0.30f, 0.80f);
        targetMat.SetFloat("_Glossiness", 0.6f);
        targetMat.SetFloat("_Metallic", 0.2f);

        // ─── MATEMATIK ───────────────────────────────────────────────────────
        float rampRad  = RAMP_ANGLE * Mathf.Deg2Rad;
        float rampDZ   = RAMP_LEN * Mathf.Cos(rampRad);   // 57.06
        float rampDY   = RAMP_LEN * Mathf.Sin(rampRad);   // 18.54
        float rampTopZ = RAMP_START_Z + rampDZ;            // 97.06
        float rampTopY = rampDY;
        float targetZ  = rampTopZ + 70f;                   // 167.06
        float targetY  = 15f;

        Debug.Log("[3/5] Pist ve rampa olusturuluyor...");

        // ─── ANA ASFALT PİST ─────────────────────────────────────────────────
        // SimplePoly City'nin kendi yolunun UZERINDE, onu kapatmayacak sekilde
        // Pist Y=-0.01 ile hafifce altta (veya uzerinde)
        GameObject runway = GameObject.CreatePrimitive(PrimitiveType.Cube);
        runway.name = "Runway_Asphalt";
        runway.transform.position = new Vector3(0f, 0f, -20f);
        runway.transform.localScale = new Vector3(ROAD_W, 0.08f, 200f);
        runway.GetComponent<Renderer>().sharedMaterial = asphaltMat;

        // Kaldirımlar
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sw.name = "Sidewalk";
            sw.transform.position = new Vector3(side * (ROAD_W / 2f + 2.2f), 0.02f, -20f);
            sw.transform.localScale = new Vector3(4f, 0.1f, 200f);
            sw.GetComponent<Renderer>().sharedMaterial = sidewalkMat;
        }

        // Orta beyaz kesik cizgiler
        GameObject stripeParent = new GameObject("RoadStripes");
        for (float z = -110f; z < RAMP_START_Z; z += 5f)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            s.transform.SetParent(stripeParent.transform);
            s.transform.position = new Vector3(0f, 0.05f, z);
            s.transform.localScale = new Vector3(0.3f, 0.02f, 2.5f);
            s.GetComponent<Renderer>().sharedMaterial = stripeMat;
            Object.DestroyImmediate(s.GetComponent<Collider>());
        }

        // Kenar sari cizgiler
        for (int side = -1; side <= 1; side += 2)
        {
            for (float z = -110f; z < RAMP_START_Z; z += 5f)
            {
                GameObject ys = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ys.transform.SetParent(stripeParent.transform);
                ys.transform.position = new Vector3(side * (ROAD_W / 2f - 0.4f), 0.05f, z);
                ys.transform.localScale = new Vector3(0.22f, 0.02f, 5f);
                ys.GetComponent<Renderer>().sharedMaterial = yellowMat;
                Object.DestroyImmediate(ys.GetComponent<Collider>());
            }
        }

        // ─── RAMPA ───────────────────────────────────────────────────────────
        float lipY = (RAMP_THICK / 2f) * Mathf.Cos(rampRad);
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp";
        ramp.transform.position = new Vector3(0f, rampDY / 2f - lipY, RAMP_START_Z + rampDZ / 2f);
        ramp.transform.localScale = new Vector3(RAMP_W, RAMP_THICK, RAMP_LEN);
        ramp.transform.rotation = Quaternion.Euler(-RAMP_ANGLE, 0f, 0f);
        ramp.GetComponent<Renderer>().sharedMaterial = rampMat;

        // Rampa seritleri
        GameObject rampStripes = new GameObject("RampStripes");
        rampStripes.transform.SetParent(ramp.transform);
        rampStripes.transform.localPosition = Vector3.zero;
        rampStripes.transform.localRotation = Quaternion.identity;
        for (float rz = -27f; rz < 28f; rz += 5f)
        {
            GameObject rs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rs.transform.SetParent(rampStripes.transform);
            rs.transform.localPosition = new Vector3(0f, 0.52f, rz);
            rs.transform.localRotation = Quaternion.identity;
            rs.transform.localScale = new Vector3(0.3f / RAMP_W, 0.04f / RAMP_THICK, 2.5f / RAMP_LEN);
            rs.GetComponent<Renderer>().sharedMaterial = stripeMat;
            Object.DestroyImmediate(rs.GetComponent<Collider>());
        }

        Debug.Log("[3/5] Pist ve rampa tamamlandi. Rampa tepesi Z=" + rampTopZ.ToString("F2") + " Y=" + rampTopY.ToString("F2"));

        // ─── HEDEF ───────────────────────────────────────────────────────────
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        target.name = "GiantTarget";
        target.transform.position = new Vector3(0f, targetY, targetZ);
        target.transform.localScale = new Vector3(16f, 28f, 16f);
        target.GetComponent<Renderer>().sharedMaterial = targetMat;
        Rigidbody rb = target.AddComponent<Rigidbody>();
        rb.mass = 8000f;
        rb.linearDamping = 1f;

        GameObject landing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        landing.name = "LandingArea";
        landing.transform.position = new Vector3(0f, 0f, targetZ + 30f);
        landing.transform.localScale = new Vector3(80f, 0.08f, 160f);
        landing.GetComponent<Renderer>().sharedMaterial = grassMat;

        Debug.Log("[4/5] Hedef ve inis alani eklendi. Hedef Z=" + targetZ.ToString("F2"));

        // ─── ARABA + KAMERA ──────────────────────────────────────────────────
        Scene sandboxScene = EditorSceneManager.OpenScene("Assets/Scenes/Sandbox.unity", OpenSceneMode.Additive);
        GameObject originalCar = GameObject.Find("Car");

        if (originalCar != null)
        {
            GameObject newCar = PrefabUtility.InstantiatePrefab(originalCar) as GameObject;
            if (newCar == null) newCar = Object.Instantiate(originalCar);
            newCar.name = "PlayerCar";
            newCar.transform.position = new Vector3(0f, 0.5f, -80f);
            newCar.transform.rotation = Quaternion.identity;
            SceneManager.MoveGameObjectToScene(newCar, challengeScene);

            // Yapay Zeka Sürücü ve Gerçek Motor Seslerini Ekle
            ChallengeAIDriver aiDriver = newCar.GetComponent<ChallengeAIDriver>();
            if (aiDriver == null) aiDriver = newCar.AddComponent<ChallengeAIDriver>();

            string audioPath = "Assets/Car Engine Sound - i6 German Free/Assets/Audio/i6_german_free/";
            aiDriver.startClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath + "startup.wav");
            aiDriver.idleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath + "idle.wav");
            aiDriver.lowClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath + "low_on.wav");
            aiDriver.medClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath + "med_on.wav");
            aiDriver.highClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath + "high_on.wav");
            aiDriver.maxRpmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath + "maxRPM.wav");

            Debug.Log("[5/5] Araba ve i6 German Gerçek Motor Sesi (AI + Audio) eklendi: " + newCar.name);

            // Kamera
            GameObject origCam = GameObject.Find("Main Camera");
            if (origCam != null)
            {
                // Sahnede halihazirda SimplePoly Camera varsa devre disi birak
                foreach (var existCam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if (existCam.gameObject.scene == challengeScene)
                        existCam.gameObject.SetActive(false);
                }

                GameObject newCam = Object.Instantiate(origCam);
                newCam.name = "ChallengeCamera";
                newCam.tag = "MainCamera";
                newCam.SetActive(true);
                SceneManager.MoveGameObjectToScene(newCam, challengeScene);

                if (newCam.GetComponent<AudioListener>() == null)
                {
                    newCam.AddComponent<AudioListener>();
                }

                CameraCar camScript = newCam.GetComponent<CameraCar>();
                if (camScript != null)
                {
                    camScript.target             = newCar;
                    camScript.targetHeightOffset = 1.2f;
                    camScript.distance           = 7f;
                    camScript.cameraHeightOffset = 2.8f;
                }
                Debug.Log("[5/5] Kamera eklendi: ChallengeCamera");
            }
        }
        else
        {
            Debug.LogWarning("[5/5] Sandbox.unity icinde 'Car' bulunamadi!");
        }

        EditorSceneManager.CloseScene(sandboxScene, true);

        // ─── KAYDET ──────────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(challengeScene, challengePath);

        string mainMenuPath = "Assets/Scenes/MainMenu.unity";
        if (System.IO.File.Exists(mainMenuPath))
        {
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(mainMenuPath, true),
                new EditorBuildSettingsScene(challengePath, true),
            };
        }
        else
        {
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(challengePath, true),
            };
        }

        Debug.Log("=================================================================");
        Debug.Log("[MATEMATIKSEL DOGRULAMA RAPORU]");
        Debug.Log("  Arac Baslangic  : (0, 0.5, -80)");
        Debug.Log("  Rampa Girisi    : (0, 0.0, " + RAMP_START_Z + ")");
        Debug.Log("  Rampa Tepesi    : (0, " + rampTopY.ToString("F2") + ", " + rampTopZ.ToString("F2") + ")");
        Debug.Log("  Hedef Merkezi   : (0, " + targetY + ", " + targetZ.ToString("F2") + ")");
        Debug.Log("  Ucus Mesafesi   : " + (targetZ - rampTopZ).ToString("F1") + " m");
        Debug.Log("  Rampa Acisi     : " + RAMP_ANGLE + " derece");
        Debug.Log("=================================================================");
        Debug.Log("TAMAMLANDI! SimplePoly City haritasi uzerine stunt track eklendi.");
        Debug.Log("Play tusuna basarak test edebilirsin!");

        EditorUtility.DisplayDialog(
            "Harika!",
            "SimplePoly City haritasi uzerine stunt track basariyla eklendi!\n\n" +
            "Rampa Acisi: " + RAMP_ANGLE + " derece\n" +
            "Rampa Tepesi: Y=" + rampTopY.ToString("F1") + "m, Z=" + rampTopZ.ToString("F1") + "m\n" +
            "Hedefe Uzaklik: " + (targetZ - rampTopZ).ToString("F0") + "m\n\n" +
            "Play tusuna bas ve dene!",
            "Gaz ver!");
    }
}
