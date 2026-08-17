using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

public class ChallengeBuilder : EditorWindow
{
    const float ROAD_W = 20f;
    const float RAMP_ANGLE = 18f;
    const float RAMP_LEN = 60f;
    const float RAMP_W = 18f;
    const float RAMP_THICK = 0.15f;
    const float RAMP_START_Z = 40f;

    [MenuItem("Tools/Build Challenge Level")]
    public static void BuildChallengeLevel()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hata!", "Lutfen once Play modundan cikin.", "Tamam");
            return;
        }

        Debug.Log("=================================================================");
        Debug.Log("CHALLENGE BUILDER v3 - SimplePoly City entegrasyonu basladi...");

        // 1. MAIN MENU
        Scene mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject mainCamObj = new GameObject("Main Camera");
        Camera mainCam = mainCamObj.AddComponent<Camera>();
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
        mainCamObj.tag = "MainCamera";

        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject evSys = new GameObject("EventSystem");
        evSys.AddComponent<EventSystem>();
        evSys.AddComponent<StandaloneInputModule>();

        GameObject btnObj = new GameObject("ChallengeButton");
        btnObj.transform.SetParent(canvasObj.transform, false);
        RectTransform btnRT = btnObj.AddComponent<RectTransform>();
        btnRT.sizeDelta = new Vector2(340, 90);
        btnRT.anchoredPosition = Vector2.zero;
        btnObj.AddComponent<Image>().color = new Color(0.9f, 0.1f, 0.1f);
        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRT = txtObj.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        Text txt = txtObj.AddComponent<Text>();
        txt.text = "CHALLENGE OYNA!";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 30;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        GameObject logicObj = new GameObject("MenuLogic");
        MainMenu menuLogic = logicObj.AddComponent<MainMenu>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            btn.onClick, new UnityEngine.Events.UnityAction(menuLogic.StartChallenge));

        string mainMenuPath = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(mainMenuScene, mainMenuPath);
        Debug.Log("[1/6] MainMenu kaydedildi.");

        // 2. CHALLENGE SAHNESI (BOŞ, TEMIZ)
        Scene challengeScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Gunes
        GameObject sunObj = new GameObject("Sun");
        Light sun = sunObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.25f;
        sun.color = new Color(1f, 0.95f, 0.85f);
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.7f;
        sunObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Ortam isigi
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.72f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.65f, 0.65f, 0.65f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.28f, 0.20f);
        RenderSettings.fogColor = new Color(0.7f, 0.8f, 0.95f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 150f;
        RenderSettings.fogEndDistance = 400f;

        Debug.Log("[2/6] Isiklandirma ve gokyuzu ayarlandi.");

        // 3. MATERYALLER
        Material asphaltMat = new Material(Shader.Find("Standard"));
        asphaltMat.color = new Color(0.14f, 0.14f, 0.15f);
        asphaltMat.SetFloat("_Glossiness", 0.15f);

        Material stripeMat = new Material(Shader.Find("Standard"));
        stripeMat.color = new Color(0.95f, 0.95f, 0.80f);
        stripeMat.SetFloat("_Glossiness", 0.1f);

        Material yellowMat = new Material(Shader.Find("Standard"));
        yellowMat.color = new Color(0.95f, 0.78f, 0.05f);
        yellowMat.SetFloat("_Glossiness", 0f);

        Material grassMat = new Material(Shader.Find("Standard"));
        grassMat.color = new Color(0.22f, 0.48f, 0.18f);
        grassMat.SetFloat("_Glossiness", 0.05f);

        Material sidewalkMat = new Material(Shader.Find("Standard"));
        sidewalkMat.color = new Color(0.60f, 0.60f, 0.62f);
        sidewalkMat.SetFloat("_Glossiness", 0.1f);

        Material rampMat = new Material(Shader.Find("Standard"));
        rampMat.color = new Color(0.13f, 0.13f, 0.14f);
        rampMat.SetFloat("_Glossiness", 0.25f);

        Material targetMat = new Material(Shader.Find("Standard"));
        targetMat.color = new Color(0.08f, 0.30f, 0.80f);
        targetMat.SetFloat("_Glossiness", 0.6f);
        targetMat.SetFloat("_Metallic", 0.2f);

        Debug.Log("[3/6] Materyaller olusturuldu.");

        // 4. PIST + RAMPA
        float rampRad  = RAMP_ANGLE * Mathf.Deg2Rad;
        float rampDZ   = RAMP_LEN * Mathf.Cos(rampRad);
        float rampDY   = RAMP_LEN * Mathf.Sin(rampRad);
        float rampTopZ = RAMP_START_Z + rampDZ;
        float rampTopY = rampDY;
        float targetZ  = rampTopZ + 70f;
        float targetY  = 15f;

        // Ana yol
        GameObject runway = GameObject.CreatePrimitive(PrimitiveType.Cube);
        runway.name = "Runway_Asphalt";
        runway.transform.position = new Vector3(0f, -0.05f, -20f);
        runway.transform.localScale = new Vector3(ROAD_W, 0.1f, 200f);
        runway.GetComponent<Renderer>().sharedMaterial = asphaltMat;

        // Kaldirımlar
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sw.name = "Sidewalk";
            sw.transform.position = new Vector3(side * (ROAD_W / 2f + 2f), -0.02f, -20f);
            sw.transform.localScale = new Vector3(4f, 0.12f, 200f);
            sw.GetComponent<Renderer>().sharedMaterial = sidewalkMat;
        }

        // Orta sari kesik cizgiler
        GameObject stripeParent = new GameObject("RoadStripes");
        for (float z = -110f; z < RAMP_START_Z; z += 5f)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            s.transform.SetParent(stripeParent.transform);
            s.transform.position = new Vector3(0f, 0.02f, z);
            s.transform.localScale = new Vector3(0.35f, 0.02f, 2.5f);
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
                ys.transform.position = new Vector3(side * (ROAD_W / 2f - 0.5f), 0.02f, z);
                ys.transform.localScale = new Vector3(0.25f, 0.02f, 5f);
                ys.GetComponent<Renderer>().sharedMaterial = yellowMat;
                Object.DestroyImmediate(ys.GetComponent<Collider>());
            }
        }

        // Rampa
        float lipY = (RAMP_THICK / 2f) * Mathf.Cos(rampRad);
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp";
        ramp.transform.position = new Vector3(0f, rampDY / 2f - lipY, RAMP_START_Z + rampDZ / 2f);
        ramp.transform.localScale = new Vector3(RAMP_W, RAMP_THICK, RAMP_LEN);
        ramp.transform.rotation = Quaternion.Euler(-RAMP_ANGLE, 0f, 0f);
        ramp.GetComponent<Renderer>().sharedMaterial = rampMat;

        // Rampa seritleri
        GameObject rampStripeParent = new GameObject("RampStripes");
        rampStripeParent.transform.SetParent(ramp.transform);
        rampStripeParent.transform.localPosition = Vector3.zero;
        rampStripeParent.transform.localRotation = Quaternion.identity;
        for (float rz = -27f; rz < 28f; rz += 5f)
        {
            GameObject rs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rs.transform.SetParent(rampStripeParent.transform);
            rs.transform.localPosition = new Vector3(0f, 0.52f, rz);
            rs.transform.localRotation = Quaternion.identity;
            rs.transform.localScale = new Vector3(0.35f / RAMP_W, 0.04f / RAMP_THICK, 2.5f / RAMP_LEN);
            rs.GetComponent<Renderer>().sharedMaterial = stripeMat;
            Object.DestroyImmediate(rs.GetComponent<Collider>());
        }

        Debug.Log("[4/6] Pist ve rampa kuruldu. Rampa tepesi: " + rampTopZ.ToString("F2"));

        // 5. HEDEF
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        target.name = "GiantTarget";
        target.transform.position = new Vector3(0f, targetY, targetZ);
        target.transform.localScale = new Vector3(16f, 28f, 16f);
        target.GetComponent<Renderer>().sharedMaterial = targetMat;
        Rigidbody targetRb = target.AddComponent<Rigidbody>();
        targetRb.mass = 8000f;

        GameObject landing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        landing.name = "LandingArea";
        landing.transform.position = new Vector3(0f, -0.05f, targetZ + 30f);
        landing.transform.localScale = new Vector3(80f, 0.1f, 160f);
        landing.GetComponent<Renderer>().sharedMaterial = grassMat;

        Debug.Log("[5/6] Hedef kuruldu: Z=" + targetZ.ToString("F2"));

        // 6. SIMPLYPOLY CITY SEHRI
        string[] skyB = {
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building Sky_big_color01.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building Sky_big_color02.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building Sky_big_color03.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building Sky_small_color01.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building Sky_small_color02.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building Sky_small_color03.prefab",
        };
        string[] midB = {
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Residential_color01.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Residential_color02.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Residential_color03.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Bar.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Pizza.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Fast Food.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Music Store.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Gas Station.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Factory.prefab",
        };
        string[] nPaths = {
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Natures/Natures_Big Tree.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Natures/Natures_Fir Tree.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Natures/Natures_Cube Tree.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Natures/Natures_Bush_01.prefab",
        };
        string[] pPaths = {
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Street Light.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Traffic Signal_big.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Bench_1.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_BillBoard_medium.prefab",
            "Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Bus Stop.prefab",
        };

        GameObject cityRoot = new GameObject("SimplePoly_City");
        int bI=0, sI=0, nI=0, pI=0, spawnCount=0;

        for (float z = -80f; z <= 230f; z += 22f)
        {
            // Sol on sira (yola bakan orta binalar)
            if (SpawnPrefab(midB[bI % midB.Length], cityRoot, new Vector3(-(ROAD_W/2f+8f), 0f, z), Quaternion.Euler(0,90,0)) != null) spawnCount++;
            bI++;
            // Sol orta sira (gokyuzu binasi)
            if (SpawnPrefab(skyB[sI % skyB.Length], cityRoot, new Vector3(-(ROAD_W/2f+26f), 0f, z+5f), Quaternion.Euler(0,90,0)) != null) spawnCount++;
            sI++;
            // Sol arka sira
            if (SpawnPrefab(skyB[sI % skyB.Length], cityRoot, new Vector3(-(ROAD_W/2f+44f), 0f, z-5f), Quaternion.Euler(0,180,0)) != null) spawnCount++;
            sI++;

            // Sag on sira
            if (SpawnPrefab(midB[bI % midB.Length], cityRoot, new Vector3(ROAD_W/2f+8f, 0f, z), Quaternion.Euler(0,-90,0)) != null) spawnCount++;
            bI++;
            // Sag orta sira
            if (SpawnPrefab(skyB[sI % skyB.Length], cityRoot, new Vector3(ROAD_W/2f+26f, 0f, z+5f), Quaternion.Euler(0,-90,0)) != null) spawnCount++;
            sI++;
            // Sag arka sira
            if (SpawnPrefab(skyB[sI % skyB.Length], cityRoot, new Vector3(ROAD_W/2f+44f, 0f, z-5f), Quaternion.Euler(0,0,0)) != null) spawnCount++;
            sI++;

            // Kaldirim props
            SpawnPrefab(pPaths[pI % pPaths.Length], cityRoot, new Vector3(-(ROAD_W/2f+2.5f), 0f, z-6f), Quaternion.Euler(0,90,0));
            SpawnPrefab(pPaths[pI % pPaths.Length], cityRoot, new Vector3(ROAD_W/2f+2.5f, 0f, z+6f), Quaternion.Euler(0,-90,0));
            pI++;

            // Agaclar
            SpawnPrefab(nPaths[nI % nPaths.Length], cityRoot, new Vector3(-(ROAD_W/2f+4f), 0f, z+8f), Quaternion.identity);
            SpawnPrefab(nPaths[nI % nPaths.Length], cityRoot, new Vector3(ROAD_W/2f+4f, 0f, z-8f), Quaternion.identity);
            nI++;
        }

        // Sehir zemini
        GameObject cityFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cityFloor.name = "CityFloor";
        cityFloor.transform.position = new Vector3(0f, -0.15f, 70f);
        cityFloor.transform.localScale = new Vector3(300f, 0.15f, 500f);
        cityFloor.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

        Debug.Log("[6/6] SimplePoly City yerlestirildi. Bina sayisi: " + spawnCount);

        // 7. ARABA + KAMERA
        Scene sandboxScene = EditorSceneManager.OpenScene("Assets/Scenes/Sandbox.unity", OpenSceneMode.Additive);
        GameObject originalCar = GameObject.Find("Car");
        if (originalCar != null)
        {
            GameObject newCar = PrefabUtility.InstantiatePrefab(originalCar) as GameObject;
            if (newCar == null) newCar = Object.Instantiate(originalCar);
            newCar.name = "PlayerCar";
            newCar.transform.position = new Vector3(0f, 0.6f, -80f);
            newCar.transform.rotation = Quaternion.identity;
            SceneManager.MoveGameObjectToScene(newCar, challengeScene);

            GameObject origCam = GameObject.Find("Main Camera");
            if (origCam != null)
            {
                GameObject newCam = Object.Instantiate(origCam);
                newCam.name = "Main Camera";
                newCam.tag = "MainCamera";
                SceneManager.MoveGameObjectToScene(newCam, challengeScene);
                CameraCar camScript = newCam.GetComponent<CameraCar>();
                if (camScript != null)
                {
                    camScript.target = newCar;
                    camScript.targetHeightOffset = 1.2f;
                    camScript.distance = 7f;
                    camScript.cameraHeightOffset = 2.8f;
                }
            }
        }
        EditorSceneManager.CloseScene(sandboxScene, true);

        // 8. KAYDET
        string challengePath = "Assets/Scenes/Challenge.unity";
        EditorSceneManager.SaveScene(challengeScene, challengePath);
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(mainMenuPath, true),
            new EditorBuildSettingsScene(challengePath, true),
        };

        Debug.Log("=================================================================");
        Debug.Log("[MATEMATIKSEL RAPOR]");
        Debug.Log("   Arac Baslangic : (0, 0.6, -80)");
        Debug.Log("   Rampa Girisi   : (0, 0.0, " + RAMP_START_Z + ")");
        Debug.Log("   Rampa Tepesi   : (0, " + rampTopY.ToString("F2") + ", " + rampTopZ.ToString("F2") + ")");
        Debug.Log("   Hedef Merkezi  : (0, " + targetY + ", " + targetZ.ToString("F2") + ")");
        Debug.Log("   Ucus Mesafesi  : " + (targetZ - rampTopZ).ToString("F1") + " m");
        Debug.Log("   Rampa Acisi    : " + RAMP_ANGLE + " derece");
        Debug.Log("=================================================================");

        EditorSceneManager.OpenScene(challengePath);
        Debug.Log("TAMAMLANDI — Challenge sahnesi Scene view'da hazir!");
    }

    private static GameObject SpawnPrefab(string path, GameObject parent, Vector3 pos, Quaternion rot)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("[ChallengeBuilder] Prefab bulunamadi: " + path);
            return null;
        }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.localScale = Vector3.one;

        // BoxCollider — bina seklini saran, performansli cozum
        if (go.GetComponent<Collider>() == null)
        {
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                BoxCollider bc = go.AddComponent<BoxCollider>();
                bc.center = go.transform.InverseTransformPoint(b.center);
                bc.size   = go.transform.InverseTransformVector(b.size);
            }
        }
        return go;
    }
}
