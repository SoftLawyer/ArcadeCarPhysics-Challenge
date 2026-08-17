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

        // 3. MATEMATİKSEL PİST VE RAMPA
        GameObject runway = GameObject.CreatePrimitive(PrimitiveType.Cube);
        runway.name = "Runway_Road";
        runway.transform.position = new Vector3(0f, -0.5f, -10f);
        runway.transform.localScale = new Vector3(20f, 1f, 100f);
        Material roadMat = new Material(Shader.Find("Standard"));
        roadMat.color = new Color(0.12f, 0.12f, 0.12f);
        runway.GetComponent<Renderer>().sharedMaterial = roadMat;

        float thetaDeg = 18f;
        float thetaRad = thetaDeg * Mathf.Deg2Rad;
        float rampLength = 60f;
        float rampWidth = 18f;
        float startZ = 40f;
        float deltaZ = rampLength * Mathf.Cos(thetaRad); 
        float deltaY = rampLength * Mathf.Sin(thetaRad); 

        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "PrecisionRamp";
        ramp.transform.position = new Vector3(0f, deltaY / 2f, startZ + (deltaZ / 2f));
        ramp.transform.localScale = new Vector3(rampWidth, 1f, rampLength);
        ramp.transform.rotation = Quaternion.Euler(-thetaDeg, 0f, 0f);
        Material rampMat = new Material(Shader.Find("Standard"));
        rampMat.color = new Color(0.9f, 0.12f, 0.12f);
        ramp.GetComponent<Renderer>().sharedMaterial = rampMat;

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

        // 5. ŞEHRİ PREFABLARDAN İNŞA ET (Toon City Yöntemi)
        string[] buildings = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Loading Games/Toon City Pack/Prefabs/Buildings" });
        string[] trees = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Loading Games/Toon City Pack/Prefabs/Vegetation" });
        
        if (buildings.Length > 0)
        {
            GameObject cityParent = new GameObject("ProceduralCity");
            int buildIndex = 0;
            // Z=-60'dan Z=200'e kadar pistin sağ ve soluna binalar dik
            for (float z = -60f; z <= 220f; z += 18f)
            {
                // Sol Bina
                string leftPath = AssetDatabase.GUIDToAssetPath(buildings[buildIndex % buildings.Length]);
                GameObject leftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(leftPath);
                if (leftPrefab)
                {
                    GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(leftPrefab, cityParent.transform);
                    b.transform.position = new Vector3(-20f, 0f, z);
                    b.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Yola dönük
                    b.transform.localScale = Vector3.one * 1.5f; // Şehir devasa görünsün
                }
                buildIndex++;

                // Sağ Bina
                string rightPath = AssetDatabase.GUIDToAssetPath(buildings[buildIndex % buildings.Length]);
                GameObject rightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rightPath);
                if (rightPrefab)
                {
                    GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(rightPrefab, cityParent.transform);
                    b.transform.position = new Vector3(20f, 0f, z);
                    b.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // Yola dönük
                    b.transform.localScale = Vector3.one * 1.5f;
                }
                buildIndex++;
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
            newCar.transform.position = new Vector3(0f, 0.6f, -40f); 
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
}
