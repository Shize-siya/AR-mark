using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>按 Unity 官方标准结构重建 AR 场景（可重复执行）</summary>
public static class ARSceneBuilder
{
    [MenuItem("Tools/AR巡检/重建 AR 场景（官方结构）")]
    public static void Rebuild()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // 1. 清场
        foreach (var root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        // ========== 2. AR Session ==========
        var sessionGO = new GameObject("AR Session");
        sessionGO.AddComponent<ARSession>();

        // ========== 3. XR Origin (ARSessionOrigin) ==========
        var originGO = new GameObject("XR Origin");
        var origin = originGO.AddComponent<ARSessionOrigin>();
        var planeMgr = originGO.AddComponent<ARPlaneManager>();
        var raycastMgr = originGO.AddComponent<ARRaycastManager>();
        var forceVisible = originGO.AddComponent<ForcePlaneVisible>();
        forceVisible.planeManager = planeMgr;

        // planePrefab
        var prefabPath = "Assets/Prefabs/ARPlaneVisual.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            planeMgr.planePrefab = prefab;
            Debug.Log("✅ planePrefab assigned");
        }
        else
        {
            // 自动创建 prefab
            prefab = CreatePlanePrefab();
            planeMgr.planePrefab = prefab;
            Debug.Log("✅ planePrefab CREATED and assigned");
        }

        // ========== 4. Main Camera (AR) ==========
        var camGO = new GameObject("Main Camera");
        camGO.transform.SetParent(originGO.transform, false);
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Depth;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        camGO.AddComponent<AudioListener>();
        camGO.AddComponent<ARCameraManager>();
        camGO.AddComponent<ARCameraBackground>();
        // TrackedPoseDriver (SpatialTracking)
        var tpdType = System.Type.GetType("UnityEngine.SpatialTracking.TrackedPoseDriver, UnityEngine.SpatialTracking");
        if (tpdType != null) camGO.AddComponent(tpdType);

        // 把 XR Origin 的 camera 设上
        origin.camera = cam;

        // ========== 5. Canvas (UI) ==========
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ========== 6. AppManager + PlaceMarkerOnPlane ==========
        var appGO = new GameObject("AppManager");
        var api = appGO.AddComponent<IssueApiClient>();
        api.serverBaseUrl = "http://192.168.31.127:8080";
        // PlaceMarkerOnPlane 必须和 ARRaycastManager 在同一个 GameObject（XR Origin）上
        // 因为它的 Awake() 用 GetComponent<ARRaycastManager>()
        var marker = originGO.AddComponent<PlaceMarkerOnPlane>();
        marker.markerPrefab = CreateMarkerPrefab();

        // ========== 7. StatusText (顶部提示) ==========
        var statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(canvasGO.transform, false);
        var statusText = statusGO.AddComponent<Text>();
        statusText.text = "移动手机检测平面...";
        statusText.fontSize = 24;
        statusText.color = Color.white;
        statusText.alignment = TextAnchor.UpperCenter;
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var statusRT = (RectTransform)statusGO.transform;
        statusRT.anchorMin = new Vector2(0, 1);
        statusRT.anchorMax = new Vector2(1, 1);
        statusRT.pivot = new Vector2(0.5f, 1);
        statusRT.anchoredPosition = new Vector2(0, -20);
        statusRT.sizeDelta = new Vector2(0, 40);

        // ========== 8. FormPanel (底部表单，默认隐藏) ==========
        var formGO = new GameObject("FormPanel");
        formGO.transform.SetParent(canvasGO.transform, false);
        formGO.SetActive(false);
        var formImg = formGO.AddComponent<Image>();
        formImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        var formRT = (RectTransform)formGO.transform;
        formRT.anchorMin = new Vector2(0, 0);
        formRT.anchorMax = new Vector2(1, 0);
        formRT.pivot = new Vector2(0.5f, 0);
        formRT.anchoredPosition = new Vector2(0, 0);
        formRT.sizeDelta = new Vector2(0, 400);

        // 表单内部控件（标题输入、描述输入、优先级、提交、关闭）
        var formUI = BuildFormControls(formGO.transform, canvasGO.transform);
        formUI.statusText = statusText;

        // ========== 9. 关联到脚本 ==========
        marker.issueForm = formUI;
        formUI.apiClient = api;
        formUI.markerPlacer = marker;
        formUI.formPanel = formGO;

        // 保存
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("🎉 AR 场景重建完成（官方标准结构）");
        EditorUtility.DisplayDialog("完成", "AR 场景已按官方标准结构重建，可以直接 Build And Run。", "好");
    }

    static IssueFormUI BuildFormControls(Transform formParent, Transform canvasTransform)
    {
        // 创建 IssueFormUI 脚本挂在 canvas 上
        var formUI = canvasTransform.gameObject.AddComponent<IssueFormUI>();

        // 标题
        var titleInput = CreateInputField(formParent, "TitleInput", "标题（必填）", new Vector2(50, 320), 500, 60);
        formUI.titleInput = titleInput;

        // 描述
        var descInput = CreateInputField(formParent, "DescInput", "描述（选填）", new Vector2(50, 220), 500, 100);
        formUI.descriptionInput = descInput;

        // 优先级 Dropdown
        var priorityGO = new GameObject("PriorityDropdown");
        priorityGO.transform.SetParent(formParent, false);
        var pImg = priorityGO.AddComponent<Image>();
        pImg.color = Color.white;
        var pDropdown = priorityGO.AddComponent<Dropdown>();
        pDropdown.ClearOptions();
        pDropdown.AddOptions(new[] { "低", "中", "高" }.ToList());
        var pRT = (RectTransform)priorityGO.transform;
        pRT.anchoredPosition = new Vector2(50, 140);
        pRT.sizeDelta = new Vector2(200, 50);
        formUI.priorityDropdown = pDropdown;

        // 提交按钮
        var submitBtn = CreateButton(formParent, "SubmitButton", "提交", new Color(0.2f, 0.6f, 1f, 1f), new Vector2(50, 60), 200, 60);
        formUI.submitButton = submitBtn;

        // 关闭按钮（红色 X，右上角）
        var closeBtnGO = new GameObject("CloseButton");
        closeBtnGO.transform.SetParent(formParent, false);
        var cImg = closeBtnGO.AddComponent<Image>();
        cImg.color = new Color(0.9f, 0.3f, 0.3f, 1f);
        var closeBtn = closeBtnGO.AddComponent<Button>();
        var cRT = (RectTransform)closeBtnGO.transform;
        cRT.anchorMin = new Vector2(1, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(1, 1);
        cRT.anchoredPosition = new Vector2(-15, -15);
        cRT.sizeDelta = new Vector2(50, 50);
        // 文字子物体
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(closeBtnGO.transform, false);
        var txt = txtGO.AddComponent<Text>();
        txt.text = "X";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 32;
        txt.color = Color.white;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var tRT = (RectTransform)txtGO.transform;
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;
        formUI.closeButton = closeBtn;

        return formUI;
    }

    static InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 pos, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        var input = go.AddComponent<InputField>();
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(w, h);

        // Placeholder
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(go.transform, false);
        var phText = phGO.AddComponent<Text>();
        phText.text = placeholder;
        phText.alignment = TextAnchor.MiddleLeft;
        phText.fontSize = 20;
        phText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var phRT = (RectTransform)phGO.transform;
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(10, 0);
        phRT.offsetMax = Vector2.zero;

        // Text
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<Text>();
        txt.alignment = TextAnchor.MiddleLeft;
        txt.fontSize = 20;
        txt.color = Color.black;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var txtRT = (RectTransform)txtGO.transform;
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(10, 0);
        txtRT.offsetMax = Vector2.zero;

        input.textComponent = txt;
        input.placeholder = phText;

        return input;
    }

    static Button CreateButton(Transform parent, string name, string label, Color bg, Vector2 pos, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(w, h);

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<Text>();
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 22;
        txt.color = Color.white;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var txtRT = (RectTransform)txtGO.transform;
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        return btn;
    }

    static GameObject CreateMarkerPrefab()
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.5f, 0.1f, 1f); // 橙色完全不透明
        AssetDatabase.CreateAsset(mat, "Assets/Materials/MarkerMat.mat");
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        var dir = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        var path = dir + "/Marker.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    static GameObject CreatePlanePrefab()
    {
        // 创建材质 —— Opaque 纯绿（调试用），Standard Shader
        var mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 0); // Opaque
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = -1;
        mat.color = new Color(0f, 1f, 0f, 1f); // 纯绿完全不透明
        AssetDatabase.CreateAsset(mat, "Assets/Materials/PlaneMat.mat");

        // 创建 GameObject
        var go = new GameObject("ARPlaneVisual");
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = GameObject.CreatePrimitive(PrimitiveType.Plane).GetComponent<MeshFilter>().sharedMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        // 保存为 Prefab
        var dir = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        var path = dir + "/ARPlaneVisual.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
}
