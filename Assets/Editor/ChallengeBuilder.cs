using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChallengeBuilder : EditorWindow
{
    [MenuItem("Tools/Build Challenge Level")]
    public static void BuildChallengeLevel()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hata!", "Bu aracı çalıştırmadan önce lütfen Play (▶️) modundan çıkın (Mavi butonu kapatın).", "Tamam");
            return;
        }

        // YENİ ANA MENÜ SAHNESİ OLUŞTURMA
        Scene mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // Kamera ekle
        GameObject mainCamera = new GameObject("Main Camera");
        Camera cam = mainCamera.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        
        // Canvas ekle
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // EventSystem ekle
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();

        // Buton ekle
        GameObject buttonObj = new GameObject("ChallengeButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(300, 100);
        btnRect.anchoredPosition = Vector2.zero;
        Image btnImg = buttonObj.AddComponent<Image>();
        btnImg.color = new Color(0.8f, 0.2f, 0.2f);
        Button btn = buttonObj.AddComponent<Button>();
        
        // Buton Metni ekle
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text btnText = textObj.AddComponent<Text>();
        btnText.text = "CHALLENGE OYNA!";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 24;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        
        // Butona tıklandığında çalışacak kod objesini ekle
        GameObject logicObj = new GameObject("MenuLogic");
        MainMenu menuLogic = logicObj.AddComponent<MainMenu>();
        
        // Event'i bağla
        UnityEngine.Events.UnityAction action = new UnityEngine.Events.UnityAction(menuLogic.StartChallenge);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
        
        // Sahneyi kaydet
        string mainMenuPath = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(mainMenuScene, mainMenuPath);


        // YENİ CHALLENGE SAHNESİ OLUŞTURMA
        Scene challengeScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Varsayılan kamerayı sil (çift Audio Listener hatasını önlemek için)
        GameObject defaultCam = GameObject.Find("Main Camera");
        if (defaultCam != null) Object.DestroyImmediate(defaultCam);

        // Zemin oluştur (Araba y=0'da hareket etmeli)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -0.5f, 100);
        ground.transform.localScale = new Vector3(100, 1, 400); // Çok uzun ve geniş bir zemin
        ground.GetComponent<Renderer>().sharedMaterial.color = new Color(0.2f, 0.2f, 0.2f); // Asfalt rengi

        // Dev rampa oluştur (Yukarı doğru zıplatan rampa, y=5'e kadar çıkacak)
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "GiantRamp";
        ramp.transform.position = new Vector3(0, 5, 50); // İleriye ve yukarıya yerleştirdik
        ramp.transform.localScale = new Vector3(20, 1, 50);
        ramp.transform.rotation = Quaternion.Euler(-15, 0, 0); // Yukarı doğru fırlatan eğim (-15)
        ramp.GetComponent<Renderer>().sharedMaterial.color = new Color(0.8f, 0.1f, 0.1f); // Kırmızı rampa

        // Dev hedef karakter (Yamal / Capsule) - Rampanın ardına yerleştir (Örneğin z=120)
        GameObject giantPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        giantPlayer.name = "GiantPlayer_Target";
        giantPlayer.transform.position = new Vector3(0, 20, 120); // Yere (y=20, z=120) yerleşti (boyu 40 olduğu için y=20 zemin hizasıdır)
        giantPlayer.transform.localScale = new Vector3(20, 40, 20);
        giantPlayer.GetComponent<Renderer>().sharedMaterial.color = new Color(0.1f, 0.3f, 0.8f); // Koyu mavi
        Rigidbody giantRb = giantPlayer.AddComponent<Rigidbody>();
        giantRb.mass = 5000f; 

        // Dev futbol topu
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "GiantFootball";
        ball.transform.position = new Vector3(15, 7.5f, 100);
        ball.transform.localScale = new Vector3(15, 15, 15);
        ball.GetComponent<Renderer>().sharedMaterial.color = Color.white;
        Rigidbody ballRb = ball.AddComponent<Rigidbody>();
        ballRb.mass = 500f;

        // Arabayı Sandbox'tan getir
        Scene sandboxScene = EditorSceneManager.OpenScene("Assets/Scenes/Sandbox.unity", OpenSceneMode.Additive);
        GameObject originalCar = GameObject.Find("Car");
        if (originalCar != null)
        {
            GameObject newCar = PrefabUtility.InstantiatePrefab(originalCar) as GameObject;
            if (newCar == null)
                newCar = Object.Instantiate(originalCar);

            newCar.name = "PlayerCar";
            newCar.transform.position = new Vector3(0, 1f, 0); // Araba y=0 (zemin hizası) seviyesinde başlıyor
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
                }
            }
        }
        
        // Orijinal Sandbox sahnesini geri kapat
        EditorSceneManager.CloseScene(sandboxScene, true);
        
        // Yeni Challenge sahnesini kaydet
        string challengePath = "Assets/Scenes/Challenge.unity";
        EditorSceneManager.SaveScene(challengeScene, challengePath);
        
        // Build Settings'e ekle
        EditorBuildSettingsScene[] originalSettings = EditorBuildSettings.scenes;
        EditorBuildSettingsScene[] newSettings = new EditorBuildSettingsScene[2];
        newSettings[0] = new EditorBuildSettingsScene(mainMenuPath, true);
        newSettings[1] = new EditorBuildSettingsScene(challengePath, true);
        EditorBuildSettings.scenes = newSettings;

        // Son olarak MainMenu'yü aç
        EditorSceneManager.OpenScene(mainMenuPath);
        
        Debug.Log("🎉 Challenge Modu başarıyla oluşturuldu! Sahneler eklendi ve Ana Menü açıldı.");
    }
}
