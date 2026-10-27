using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// 一键搭建 AR 巡检场景：菜单 Tools → AR巡检 → 一键搭建 AR 场景
/// 自动完成：AR Session / XR Origin / 平面检测 / 标记 Prefab / 上报表单 UI / 脚本连线 / 安卓构建设置
/// </summary>
public static class SceneBuilder
{
    [MenuItem("Tools/AR巡检/一键搭建 AR 场景")]
    public static void BuildScene()
    {
        // ===== 0. 准备目录与资源 =====
        DirectoryUtil.Ensure("Assets/Scenes");
        DirectoryUtil.Ensure("Assets/Prefabs");
        DirectoryUtil.Ensure("Assets/Materials");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ===== 1. 标记 Prefab（10cm 立方体）=====
        var markerTemp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        markerTemp.name = "Marker";
        markerTemp.transform.localScale = Vector3.one * 0.1f;
        markerTemp.GetComponent<Renderer>().sharedMaterial = CreateMaterial("MarkerMat", new Color(0.95f, 0.4f, 0.2f));
        var markerPrefab = PrefabUtility.SaveAsPrefabAsset(markerTemp, "Assets/Prefabs/Marker.prefab");
        Object.DestroyImmediate(markerTemp);

        // ===== 2. AR 平面可视化 Prefab =====
        var planeTemp = new GameObject("ARPlaneVisual");
        planeTemp.AddComponent<ARPlane>();
        planeTemp.AddComponent<ARPlaneMeshVisualizer>();
        planeTemp.AddComponent<MeshFilter>();
        var planeRenderer = planeTemp.AddComponent<MeshRenderer>();
        planeRenderer.sharedMaterial = CreateMaterial("PlaneMat", new Color(0.1f, 0.75f, 0.5f, 0.4f));
        var planePrefab = PrefabUtility.SaveAsPrefabAsset(planeTemp, "Assets/Prefabs/ARPlaneVisual.prefab");
        Object.DestroyImmediate(planeTemp);

        // ===== 3. AR Session =====
        new GameObject("AR Session", typeof(ARSession));

        // ===== 4. XR Origin（含相机偏移 + AR 相机）=====
        var xrOriginGO = new GameObject("XR Origin");
        var xrOrigin = xrOriginGO.AddComponent<XROrigin>();

        var cameraOffsetGO = new GameObject("Camera Offset");
        cameraOffsetGO.transform.SetParent(xrOriginGO.transform, false);

        var cameraGO = new GameObject("Main Camera");
        cameraGO.transform.SetParent(cameraOffsetGO.transform, false);
        cameraGO.tag = "MainCamera";
        cameraGO.AddComponent<Camera>().clearFlags = CameraClearFlags.Depth;
        cameraGO.GetComponent<Camera>().backgroundColor = new Color(0, 0, 0, 0);
        cameraGO.AddComponent<AudioListener>();
        cameraGO.AddComponent<ARCameraManager>();

        xrOrigin.CameraFloorOffsetObject = cameraOffsetGO;
        xrOrigin.Camera = cameraGO.GetComponent<Camera>();

        // 平面检测 + 射线检测 + 我们的放置脚本
        var planeManager = xrOriginGO.AddComponent<ARPlaneManager>();
        planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
        planeManager.planePrefab = planePrefab;
        xrOriginGO.AddComponent<ARRaycastManager>();
        var markerPlacer = xrOriginGO.AddComponent<PlaceMarkerOnPlane>();
        markerPlacer.markerPrefab = markerPrefab;

        // ===== 5. AppManager（后端通信）=====
        var appManagerGO = new GameObject("AppManager");
        var apiClient = appManagerGO.AddComponent<IssueApiClient>();

        // ===== 6. UI（uGUI）=====
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        var res = GetUIResources();

        // 顶部提示文字
        var statusGO = DefaultControls.CreateText(res);
        statusGO.name = "StatusText";
        statusGO.transform.SetParent(canvasGO.transform, false);
        var statusText = statusGO.GetComponent<Text>();
        statusText.text = "移动手机寻找平面，点击平面放置标记";
        statusText.fontSize = 40;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.white;
        SetRect(statusGO, new Vector2(0, 0.9f), new Vector2(1, 1), Vector2.zero, Vector2.zero);

        // 底部表单面板
        var panelGO = DefaultControls.CreatePanel(res);
        panelGO.name = "FormPanel";
        panelGO.transform.SetParent(canvasGO.transform, false);
        SetRect(panelGO, new Vector2(0, 0), new Vector2(1, 0.45f), Vector2.zero, Vector2.zero);

        var titleGO = DefaultControls.CreateInputField(res);
        titleGO.name = "TitleInput";
        titleGO.transform.SetParent(panelGO.transform, false);
        titleGO.GetComponent<InputField>().placeholder.GetComponent<Text>().text = "标题（必填）";
        SetRect(titleGO, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);

        var descGO = DefaultControls.CreateInputField(res);
        descGO.name = "DescriptionInput";
        descGO.transform.SetParent(panelGO.transform, false);
        descGO.GetComponent<InputField>().placeholder.GetComponent<Text>().text = "问题描述（选填）";
        SetRect(descGO, new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.68f), Vector2.zero, Vector2.zero);

        var dropdownGO = DefaultControls.CreateDropdown(res);
        dropdownGO.name = "PriorityDropdown";
        dropdownGO.transform.SetParent(panelGO.transform, false);
        var dropdown = dropdownGO.GetComponent<Dropdown>();
        dropdown.options = new List<Dropdown.OptionData>
        {
            new("优先级：低"), new("优先级：中"), new("优先级：高"),
        };
        dropdown.value = 1;
        dropdown.captionText.text = "优先级：中";
        SetRect(dropdownGO, new Vector2(0.05f, 0.2f), new Vector2(0.55f, 0.4f), Vector2.zero, Vector2.zero);

        var buttonGO = DefaultControls.CreateButton(res);
        buttonGO.name = "SubmitButton";
        buttonGO.transform.SetParent(panelGO.transform, false);
        buttonGO.GetComponentInChildren<Text>().text = "提交上报";
        SetRect(buttonGO, new Vector2(0.6f, 0.2f), new Vector2(0.95f, 0.4f), Vector2.zero, Vector2.zero);

        // ===== 7. 挂表单脚本并连线 =====
        var form = canvasGO.AddComponent<IssueFormUI>();
        form.formPanel = panelGO;
        form.titleInput = titleGO.GetComponent<InputField>();
        form.descriptionInput = descGO.GetComponent<InputField>();
        form.priorityDropdown = dropdown;
        form.submitButton = buttonGO.GetComponent<Button>();
        form.statusText = statusText;
        form.apiClient = apiClient;
        form.markerPlacer = markerPlacer;
        markerPlacer.issueForm = form;

        panelGO.SetActive(false);

        // ===== 8. 保存场景并加入构建列表 =====
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ARScene.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/ARScene.unity", true) };

        // ===== 9. 安卓构建设置 =====
        PlayerSettings.productName = "AR巡检";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.example.arinspection");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("AR 场景搭建完成",
            "场景已保存到 Assets/Scenes/ARScene.unity\n\n" +
            "接下来：\n" +
            "1. Edit → Project Settings → XR Plug-in Management → 勾选 ARCore\n" +
            "2. 选中 AppManager，把 Server Base Url 改成电脑局域网 IP（如 http://192.168.1.100:8080）\n" +
            "3. File → Build Settings → Android → Switch Platform → Build And Run", "好的");
    }

    // ===== 工具方法 =====

    private static Material CreateMaterial(string name, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static DefaultControls.Resources GetUIResources()
    {
        return new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
        };
    }

    private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static class DirectoryUtil
    {
        public static void Ensure(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                var leaf = System.IO.Path.GetFileName(path);
                Ensure(parent);
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }
    }
}
