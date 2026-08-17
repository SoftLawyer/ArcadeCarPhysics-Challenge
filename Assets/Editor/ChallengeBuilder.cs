using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

public class ChallengeBuilder : EditorWindow
{
    [MenuItem("Tools/Build Challenge Level")]
    public static void BuildChallengeLevel()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hata!", "Lütfen önce Play (▶️) modundan çıkın.", "Tamam");
            return;
        }

        Debug.Log("=================================================================");
        Debug.Log("🚀 [CHALLENGE BUILDER] Şehir ve Stunt Pisti İnşası Başlatılıyor...");

        // 1. ANA MENÜ SAHNESİ
        Scene mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        GameObject mainCamera = new GameObject("Main Camera");
        Camera cam = mainCamera.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
        
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();

        GameObject buttonObj = new GameObject("ChallengeButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(340, 90);
        btnRect.anchoredPosition = Vector2.zero;
        Image btnImg = buttonObj.AddComponent<Image>();
        btnImg.color = new Color(0.85f, 0.15f, 0.15f);
        Button btn = buttonObj.AddComponent<Button>();
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text btnText = textObj.AddComponent<Text>();
        btnText.text = "CHALLENGE OYNA!";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 28;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        
        GameObject logicObj = new GameObject("MenuLogic");
        MainMenu menuLogic = logicObj.AddComponent<MainMenu>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, new UnityEngine.Events.UnityAction(menuLogic.StartChallenge));
        
        string mainMenuPath = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(mainMenuScene, mainMenuPath);

        // 2. YENİ TEMİZ CHALLENGE SAHNESİ (Demo Sahnesi Yerine Kendi Şehrimizi Kuruyoruz!)
        Scene challengeScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Varsa eski kameraları temizle
        Camera[] existingCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera c in existingCams) Object.DestroyImmediate(c.gameObject);

        Light[] existingLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in existingLights) Object.DestroyImmediate(l.gameObject);

        // Şehir Işığı
        GameObject sunObj = new GameObject("StuntSun");
        Light sun = sunObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.3f;
        sun.color = new Color(1f, 0.96f, 0.88f); // Sıcak gün ışığı
        sun.shadows = LightShadows.Soft;
        sunObj.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        // 3. MATEMATİKSEL PİST VE RAMPA (Asfalt ve Şeritler)
        GameObject runway = GameObject.CreatePrimitive(PrimitiveType.Cube);
        runway.name = "Runway_Road";
        runway.transform.position = new Vector3(0f, -0.5f, -30f); 
        runway.transform.localScale = new Vector3(20f, 1f, 200f); 
        
        Material asphaltMat = new Material(Shader.Find("Standard"));
        asphaltMat.color = new Color(0.15f, 0.15f, 0.15f); // Koyu Asfalt Rengi
        asphaltMat.SetFloat("_Glossiness", 0.2f); // Mat görünüm
        runway.GetComponent<Renderer>().sharedMaterial = asphaltMat;

        Material stripeMat = new Material(Shader.Find("Standard"));
        stripeMat.color = Color.white;
        stripeMat.SetFloat("_Glossiness", 0f);

        // Pist Şeritleri (Runway Centerlines)
        GameObject runwayStripes = new GameObject("Runway_Stripes");
        for (float z = -120f; z < 70f; z += 4f)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.SetParent(runwayStripes.transform);
            stripe.transform.position = new Vector3(0f, 0.01f, z); // Yolun milimetrik üstünde
            stripe.transform.localScale = new Vector3(0.4f, 0.02f, 2f); // Çizgi boyutları
            stripe.GetComponent<Renderer>().sharedMaterial = stripeMat;
            Object.DestroyImmediate(stripe.GetComponent<Collider>()); // Takılma yapmasın
        }

        float thetaDeg = 18f;
        float thetaRad = thetaDeg * Mathf.Deg2Rad;
        float rampLength = 60f;
        float rampWidth = 18f;
        float rampThickness = 0.2f; 
        float startZ = 40f;
        float deltaZ = rampLength * Mathf.Cos(thetaRad); 
        float deltaY = rampLength * Mathf.Sin(thetaRad); 

        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "PrecisionRamp";
        float lipCorrectionY = (rampThickness / 2f) * Mathf.Cos(thetaRad);
        ramp.transform.position = new Vector3(0f, (deltaY / 2f) - lipCorrectionY, startZ + (deltaZ / 2f));
        ramp.transform.localScale = new Vector3(rampWidth, rampThickness, rampLength);
        ramp.transform.rotation = Quaternion.Euler(-thetaDeg, 0f, 0f);
        ramp.GetComponent<Renderer>().sharedMaterial = asphaltMat; // Rampayı da asfalt yap

        // Rampa Şeritleri (Ramp Centerlines)
        GameObject rampStripes = new GameObject("Ramp_Stripes");
        rampStripes.transform.SetParent(ramp.transform);
        rampStripes.transform.localPosition = Vector3.zero;
        rampStripes.transform.localRotation = Quaternion.identity;
        
        for (float z = -25f; z < 28f; z += 4f)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.SetParent(rampStripes.transform);
            stripe.transform.localPosition = new Vector3(0f, 0.51f, z); // Rampanın local üst yüzeyi
            stripe.transform.localRotation = Quaternion.identity;
            // Rampanın Z boyutu scale ile bozulmasın diye ters hesap
            stripe.transform.localScale = new Vector3(0.4f / rampWidth, 0.05f / rampThickness, 2f / rampLength);
            stripe.GetComponent<Renderer>().sharedMaterial = stripeMat;
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
        }

        // 4. HEDEF KARAKTER
        float peakZ = startZ + deltaZ;
        float targetZ = peakZ + 70f;
        GameObject targetPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        targetPlayer.name = "GiantTarget";
        targetPlayer.transform.position = new Vector3(0f, 15f, targetZ);
        targetPlayer.transform.localScale = new Vector3(16f, 30f, 16f);
        Material targetMat = new Material(Shader.Find("Standard"));
        targetMat.color = new Color(0.1f, 0.35f, 0.85f);
        targetPlayer.GetComponent<Renderer>().sharedMaterial = targetMat;
        targetPlayer.AddComponent<Rigidbody>().mass = 8000f;

        GameObject groundPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        groundPlane.name = "LandingArea";
        groundPlane.transform.position = new Vector3(0f, -0.5f, targetZ);
        groundPlane.transform.localScale = new Vector3(100f, 1f, 120f);
        Material grassMat = new Material(Shader.Find("Standard"));
        grassMat.color = new Color(0.25f, 0.55f, 0.25f);
        groundPlane.GetComponent<Renderer>().sharedMaterial = grassMat;

        // 4.5. PEMBE MATERYAL HATASINI DÜZELT (OmniRunner'ın Curved Shader'ı bu projede yok)
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Loading Games/Toon City Pack/Materials" });
        Shader standardShader = Shader.Find("Standard");
        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader.name != "Standard")
            {
                mat.shader = standardShader;
                EditorUtility.SetDirty(mat);
            }
        }
        AssetDatabase.SaveAssets();

        // 5. ŞEHRİ PREFABLARDAN İNŞA ET (Gerçekçi Büyük Şehir)
        string[] buildings = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Loading Games/Toon City Pack/Prefabs/Buildings" });
        string[] trees = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Loading Games/Toon City Pack/Prefabs/Vegetation" });
        string[] props = AssetDatabase.FindAssets("t:GameObject", new[] { 
            "Assets/Loading Games/Toon City Pack/Prefabs/Urban Props", 
            "Assets/Loading Games/Toon City Pack/Prefabs/Infrastructure/Props" 
        });
        
        // Şehir Zemini (Beton Zemin)
        GameObject cityGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cityGround.name = "City_Concrete_Foundation";
        cityGround.transform.position = new Vector3(0f, -0.6f, 50f);
        cityGround.transform.localScale = new Vector3(300f, 1f, 400f);
        Material concreteMat = new Material(Shader.Find("Standard"));
        concreteMat.color = new Color(0.3f, 0.3f, 0.32f);
        cityGround.GetComponent<Renderer>().sharedMaterial = concreteMat;

        if (buildings.Length > 0)
        {
            GameObject cityParent = new GameObject("ProceduralCity");
            int buildIndex = 0;
            int treeIndex = 0;
            int propIndex = 0;

            // Z= -60'dan 220'ye kadar Derinlemesine Şehir Grid'i (3 Sıra Sağ, 3 Sıra Sol)
            for (float z = -60f; z <= 220f; z += 25f)
            {
                // X ekseninde binalar (-85, -55, -25) ve (25, 55, 85)
                float[] xPositions = { -85f, -55f, -25f, 25f, 55f, 85f };
                
                foreach (float x in xPositions)
                {
                    string path = AssetDatabase.GUIDToAssetPath(buildings[buildIndex % buildings.Length]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab)
                    {
                        GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cityParent.transform);
                        b.transform.position = new Vector3(x, 0f, z);
                        
                        // Yön: Yola bakan binalar (x=-25 ve x=25) yola dönsün, arkadakiler rastgele dönsün
                        if (x == -25f) b.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                        else if (x == 25f) b.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                        else b.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
                        
                        b.transform.localScale = Vector3.one * 1.5f; 
                        AddCollidersToBuilding(b);
                    }
                    buildIndex++;
                }

                // Kaldırımlara Ağaç ve Prop (Sokak lambası vb.) Ekle (X = -12 ve X = 12)
                if (trees.Length > 0)
                {
                    string tPath = AssetDatabase.GUIDToAssetPath(trees[treeIndex % trees.Length]);
                    GameObject tPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tPath);
                    if (tPrefab)
                    {
                        GameObject t1 = (GameObject)PrefabUtility.InstantiatePrefab(tPrefab, cityParent.transform);
                        t1.transform.position = new Vector3(-12f, 0f, z + 5f);
                        t1.transform.localScale = Vector3.one * 1.5f;
                        GameObject t2 = (GameObject)PrefabUtility.InstantiatePrefab(tPrefab, cityParent.transform);
                        t2.transform.position = new Vector3(12f, 0f, z - 5f);
                        t2.transform.localScale = Vector3.one * 1.5f;
                    }
                    treeIndex++;
                }

                if (props.Length > 0)
                {
                    string pPath = AssetDatabase.GUIDToAssetPath(props[propIndex % props.Length]);
                    GameObject pPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
                    if (pPrefab)
                    {
                        GameObject p1 = (GameObject)PrefabUtility.InstantiatePrefab(pPrefab, cityParent.transform);
                        p1.transform.position = new Vector3(-14f, 0f, z - 5f);
                        GameObject p2 = (GameObject)PrefabUtility.InstantiatePrefab(pPrefab, cityParent.transform);
                        p2.transform.position = new Vector3(14f, 0f, z + 5f);
                    }
                    propIndex++;
                }
            }
        }

        // 6. ARABA ENTEGRASYONU
        Scene sandboxScene = EditorSceneManager.OpenScene("Assets/Scenes/Sandbox.unity", OpenSceneMode.Additive);
        GameObject originalCar = GameObject.Find("Car");
        if (originalCar != null)
        {
            GameObject newCar = PrefabUtility.InstantiatePrefab(originalCar) as GameObject;
            if (newCar == null) newCar = Object.Instantiate(originalCar);

            newCar.name = "PlayerCar";
            newCar.transform.position = new Vector3(0f, 0.6f, -80f); // Daha çok hızlanması için en geriye alındı (Eskisi -40'tı)
            newCar.transform.rotation = Quaternion.identity; 
            SceneManager.MoveGameObjectToScene(newCar, challengeScene);

            GameObject origCam = GameObject.Find("Main Camera");
            if (origCam != null)
            {
                GameObject newCam = Object.Instantiate(origCam);
                newCam.name = "Main Camera";
                SceneManager.MoveGameObjectToScene(newCam, challengeScene);
                CameraCar camScript = newCam.GetComponent<CameraCar>();
                if (camScript != null)
                {
                    camScript.target = newCar;
                    camScript.targetHeightOffset = 1.2f;
                    camScript.distance = 6.5f;
                    camScript.cameraHeightOffset = 2.5f;
                }
            }
        }
        EditorSceneManager.CloseScene(sandboxScene, true);

        // 7. KAYDET
        string challengePath = "Assets/Scenes/Challenge.unity";
        EditorSceneManager.SaveScene(challengeScene, challengePath);

        EditorBuildSettingsScene[] newSettings = new EditorBuildSettingsScene[2];
        newSettings[0] = new EditorBuildSettingsScene(mainMenuPath, true);
        newSettings[1] = new EditorBuildSettingsScene(challengePath, true);
        EditorBuildSettings.scenes = newSettings;

        Debug.Log("✅ [ŞEHİR KURULDU] OmniRunner stili prosedürel şehir başarıyla oluşturuldu.");
        EditorSceneManager.OpenScene(mainMenuPath);
    }

    // ARABANIN BİNALARIN İÇİNDEN GEÇMEMESİ İÇİN OTOMATİK ÇARPIŞMA (COLLIDER) EKLENMESİ
    private static void AddCollidersToBuilding(GameObject building)
    {
        MeshFilter[] meshes = building.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshes)
        {
            if (mf.gameObject.GetComponent<Collider>() == null)
            {
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                // MeshCollider convex olmamalı (kutu gibi değil, binanın şeklini sarması için)
                mc.convex = false;
            }
        }
    }
}
