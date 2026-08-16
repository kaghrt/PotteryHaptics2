using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PotteryHaptics.Core;
using PotteryHaptics.Experiment;
using PotteryHaptics.Visual;
using Object = UnityEngine.Object;

namespace PotteryHaptics.Core.Editor
{
    /// <summary>
    /// 4シーン(Launcher / JND_Elasticity / JND_Viscosity / Identification)をコード側で
    /// 完全に自動構築する。既存シーンがある場合は中身を全消去してから再構築する(冪等性)。
    /// 手動でのGameObject配線は行わない方針(過去の未配線バグの再発防止)。
    /// </summary>
    public static class SceneBuilder
    {
        private const string ScenesDirectory = "Assets/_Project/Scenes";
        private const string ClayMaterialPath = "Assets/_Project/Materials/ClayMaterial.mat";
        private const string MudMaterialPath = "Assets/_Project/Materials/MudMaterial.mat";

        private static Material neutralMaterialCache;

        [MenuItem("HapticResearch/Build Launcher Scene")]
        public static void BuildLauncherSceneMenuItem() => BuildLauncherScene();

        [MenuItem("HapticResearch/Build JND_Elasticity Scene")]
        public static void BuildJndElasticitySceneMenuItem() => BuildJndElasticityScene();

        [MenuItem("HapticResearch/Build JND_Viscosity Scene")]
        public static void BuildJndViscositySceneMenuItem() => BuildJndViscosityScene();

        [MenuItem("HapticResearch/Build Identification Scene")]
        public static void BuildIdentificationSceneMenuItem() => BuildIdentificationScene();

        [MenuItem("HapticResearch/Build Everything")]
        public static void BuildEverything()
        {
            ProceduralPotteryMeshGenerator.GenerateBowlMesh();
            StimulusSetGenerator.GenerateAll();

            BuildLauncherScene();
            BuildJndElasticityScene();
            BuildJndViscosityScene();
            BuildIdentificationScene();

            RegisterScenesInBuildSettings();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("HapticResearch/Register Scenes In Build Settings")]
        public static void RegisterScenesInBuildSettings()
        {
            string[] sceneNames = { "Launcher", "JND_Elasticity", "JND_Viscosity", "Identification" };
            var scenes = new EditorBuildSettingsScene[sceneNames.Length];
            for (int i = 0; i < sceneNames.Length; i++)
            {
                string path = Path.Combine(ScenesDirectory, sceneNames[i] + ".unity").Replace('\\', '/');
                scenes[i] = new EditorBuildSettingsScene(path, true);
            }

            EditorBuildSettings.scenes = scenes;
        }

        // ==================== Launcher ====================

        public static void BuildLauncherScene()
        {
            Scene scene = OpenOrCreateEmptyScene("Launcher");

            CreateMainCamera();
            CreateDirectionalLight();

            GameObject experimentManagerGo = new GameObject("ExperimentManager");
            experimentManagerGo.AddComponent<ExperimentManager>();

            GameObject sceneFlowGo = new GameObject("SceneFlowController");
            sceneFlowGo.AddComponent<SceneFlowController>();

            CreateEventSystem();
            Canvas canvas = CreateCanvas("LauncherCanvas");

            InputField inputField = CreateInputField(canvas.transform, "ParticipantIdInputField", new Vector2(0, 40));
            Button startButton = CreateButton(canvas.transform, "StartButton", "開始", new Vector2(0, -60));

            GameObject launcherControllerGo = new GameObject("LauncherController");
            LauncherController launcherController = launcherControllerGo.AddComponent<LauncherController>();
            SetObjectField(launcherController, "participantIdInputField", inputField);
            SetObjectField(launcherController, "startButton", startButton);

            SaveScene(scene, "Launcher");
        }

        // ==================== JND_Elasticity ====================

        public static void BuildJndElasticityScene()
        {
            ProceduralPotteryMeshGenerator.GenerateBowlMesh();
            HapticCalibrationConfig config = CalibrationConfigProvider.GetOrCreate();
            StimulusSetGenerator.GenerateAll();

            Scene scene = OpenOrCreateEmptyScene("JND_Elasticity");

            CreateMainCamera();
            CreateDirectionalLight();

            FingerTracker fingerTracker = CreateFingerTrackerRig();

            GameObject elasticityGo = new GameObject("ElasticitySurface");
            ElasticitySurface elasticitySurface = elasticityGo.AddComponent<ElasticitySurface>();
            SetObjectField(elasticitySurface, "fingerTracker", fingerTracker);
            SetFloatField(elasticitySurface, "contactStartHeightCm", config.elasticityContactStartHeightCm);
            SetFloatField(elasticitySurface, "maxPushHeightCm", config.elasticityMaxPushHeightCm);

            GameObject outputGo = new GameObject("HapticOutputController");
            HapticOutputController output = outputGo.AddComponent<HapticOutputController>();
            SetObjectArrayField(output, "surfaces", new Object[] { elasticitySurface });

            VisualController visualController = CreateElasticityVisuals(out GameObject bowlGo);

            DeformationShaderDriver deformationDriver = bowlGo.AddComponent<DeformationShaderDriver>();
            SetObjectField(deformationDriver, "elasticitySurface", elasticitySurface);
            SetObjectField(deformationDriver, "targetRenderer", bowlGo.GetComponent<Renderer>());

            CreateEventSystem();
            Canvas canvas = CreateCanvas("JndCanvas");
            ResponseUI responseUI = CreateResponseUI(canvas.transform);

            GameObject sequencerGo = new GameObject("TrialSequencerJND");
            TrialSequencerJND sequencer = sequencerGo.AddComponent<TrialSequencerJND>();

            StimulusDefinition[] elasticityStimuli = StimulusSetGenerator.LoadElasticityJndSet(config);
            StimulusDefinition standard = StimulusSetGenerator.LoadStandardStimulus(TextureType.Elasticity, config);

            SetObjectField(sequencer, "surface", elasticitySurface);
            SetObjectField(sequencer, "responseUI", responseUI);
            SetObjectField(sequencer, "visualConditionSwitcherBehaviour", visualController);
            SetObjectField(sequencer, "standardStimulus", standard);
            SetObjectArrayField(sequencer, "comparisonStimuli", elasticityStimuli);
            SetStringField(sequencer, "phaseName", "JND_Elasticity");

            SaveScene(scene, "JND_Elasticity");
        }

        // ==================== JND_Viscosity ====================

        public static void BuildJndViscosityScene()
        {
            HapticCalibrationConfig config = CalibrationConfigProvider.GetOrCreate();
            StimulusSetGenerator.GenerateAll();

            Scene scene = OpenOrCreateEmptyScene("JND_Viscosity");

            CreateMainCamera();
            CreateDirectionalLight();

            FingerTracker fingerTracker = CreateFingerTrackerRig();

            GameObject viscosityGo = new GameObject("ViscositySurface");
            ViscositySurface viscositySurface = viscosityGo.AddComponent<ViscositySurface>();
            SetObjectField(viscositySurface, "fingerTracker", fingerTracker);
            SetFloatField(viscositySurface, "fixedHeightCm", config.viscosityFixedHeightCm);
            SetFloatField(viscositySurface, "heightToleranceCm", config.viscosityHeightToleranceCm);
            SetFloatField(viscositySurface, "minSpeedCmPerSec", config.viscosityMinSpeedCmPerSec);

            GameObject outputGo = new GameObject("HapticOutputController");
            HapticOutputController output = outputGo.AddComponent<HapticOutputController>();
            SetObjectArrayField(output, "surfaces", new Object[] { viscositySurface });

            VisualController visualController = CreateViscosityVisuals(out GameObject mudGo);

            MudStretchDriver stretchDriver = mudGo.AddComponent<MudStretchDriver>();
            SetObjectField(stretchDriver, "viscositySurface", viscositySurface);
            SetObjectField(stretchDriver, "stretchTarget", mudGo.transform);

            CreateEventSystem();
            Canvas canvas = CreateCanvas("JndCanvas");
            ResponseUI responseUI = CreateResponseUI(canvas.transform);

            GameObject sequencerGo = new GameObject("TrialSequencerJND");
            TrialSequencerJND sequencer = sequencerGo.AddComponent<TrialSequencerJND>();

            StimulusDefinition[] viscosityStimuli = StimulusSetGenerator.LoadViscosityJndSet(config);
            StimulusDefinition standard = StimulusSetGenerator.LoadStandardStimulus(TextureType.Viscosity, config);

            SetObjectField(sequencer, "surface", viscositySurface);
            SetObjectField(sequencer, "responseUI", responseUI);
            SetObjectField(sequencer, "visualConditionSwitcherBehaviour", visualController);
            SetObjectField(sequencer, "standardStimulus", standard);
            SetObjectArrayField(sequencer, "comparisonStimuli", viscosityStimuli);
            SetStringField(sequencer, "phaseName", "JND_Viscosity");

            SaveScene(scene, "JND_Viscosity");
        }

        // ==================== Identification ====================

        public static void BuildIdentificationScene()
        {
            ProceduralPotteryMeshGenerator.GenerateBowlMesh();
            HapticCalibrationConfig config = CalibrationConfigProvider.GetOrCreate();
            StimulusSetGenerator.GenerateAll();

            Scene scene = OpenOrCreateEmptyScene("Identification");

            CreateMainCamera();
            CreateDirectionalLight();

            FingerTracker fingerTracker = CreateFingerTrackerRig();

            GameObject elasticityGo = new GameObject("ElasticitySurface");
            ElasticitySurface elasticitySurface = elasticityGo.AddComponent<ElasticitySurface>();
            SetObjectField(elasticitySurface, "fingerTracker", fingerTracker);
            SetFloatField(elasticitySurface, "contactStartHeightCm", config.elasticityContactStartHeightCm);
            SetFloatField(elasticitySurface, "maxPushHeightCm", config.elasticityMaxPushHeightCm);

            GameObject viscosityGo = new GameObject("ViscositySurface");
            ViscositySurface viscositySurface = viscosityGo.AddComponent<ViscositySurface>();
            SetObjectField(viscositySurface, "fingerTracker", fingerTracker);
            SetFloatField(viscositySurface, "fixedHeightCm", config.viscosityFixedHeightCm);
            SetFloatField(viscositySurface, "heightToleranceCm", config.viscosityHeightToleranceCm);
            SetFloatField(viscositySurface, "minSpeedCmPerSec", config.viscosityMinSpeedCmPerSec);

            GameObject outputGo = new GameObject("HapticOutputController");
            HapticOutputController output = outputGo.AddComponent<HapticOutputController>();
            SetObjectArrayField(output, "surfaces", new Object[] { elasticitySurface, viscositySurface });

            VisualController visualController = CreateIdentificationVisuals(elasticitySurface, viscositySurface);

            CreateEventSystem();
            Canvas canvas = CreateCanvas("IdentificationCanvas");
            ResponseUI responseUI = CreateResponseUI(canvas.transform);

            GameObject sequencerGo = new GameObject("TrialSequencerIdentification");
            TrialSequencerIdentification sequencer = sequencerGo.AddComponent<TrialSequencerIdentification>();

            SetObjectField(sequencer, "elasticitySurface", elasticitySurface);
            SetObjectField(sequencer, "viscositySurface", viscositySurface);
            SetObjectField(sequencer, "responseUI", responseUI);
            SetObjectField(sequencer, "visualConditionSwitcherBehaviour", visualController);
            SetObjectField(sequencer, "elasticityWeakStimulus", StimulusSetGenerator.LoadIdentificationStimulus(TextureType.Elasticity, IntensityLevel.Weak));
            SetObjectField(sequencer, "elasticityStrongStimulus", StimulusSetGenerator.LoadIdentificationStimulus(TextureType.Elasticity, IntensityLevel.Strong));
            SetObjectField(sequencer, "viscosityWeakStimulus", StimulusSetGenerator.LoadIdentificationStimulus(TextureType.Viscosity, IntensityLevel.Weak));
            SetObjectField(sequencer, "viscosityStrongStimulus", StimulusSetGenerator.LoadIdentificationStimulus(TextureType.Viscosity, IntensityLevel.Strong));

            SaveScene(scene, "Identification");
        }

        // ==================== シーン共通ヘルパー ====================

        private static Scene OpenOrCreateEmptyScene(string sceneName)
        {
            Directory.CreateDirectory(ScenesDirectory);
            string path = Path.Combine(ScenesDirectory, sceneName + ".unity").Replace('\\', '/');

            Scene scene;
            if (File.Exists(path))
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                ClearScene(scene);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            return scene;
        }

        private static void ClearScene(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SaveScene(Scene scene, string sceneName)
        {
            string path = Path.Combine(ScenesDirectory, sceneName + ".unity").Replace('\\', '/');
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateMainCamera()
        {
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0.3f, -0.5f);
            cameraGo.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
        }

        private static void CreateDirectionalLight()
        {
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.9f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasGo = new GameObject(name);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static InputField CreateInputField(Transform parent, string name, Vector2 anchoredPosition)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 60);
            rect.anchoredPosition = anchoredPosition;

            go.AddComponent<Image>().color = Color.white;
            InputField inputField = go.AddComponent<InputField>();

            Text text = CreateLabel(go.transform, string.Empty);
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.black;
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.offsetMin = new Vector2(10, 6);
            textRect.offsetMax = new Vector2(-10, -6);

            inputField.textComponent = text;
            return inputField;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 70);
            rect.anchoredPosition = anchoredPosition;

            go.AddComponent<Image>().color = new Color(0.85f, 0.8f, 0.7f);
            Button button = go.AddComponent<Button>();
            CreateLabel(go.transform, label);

            return button;
        }

        private static Text CreateLabel(Transform parent, string label)
        {
            GameObject textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(parent, false);

            Text text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return text;
        }

        private static ResponseUI CreateResponseUI(Transform canvasTransform)
        {
            GameObject panelGo = new GameObject("ResponsePanel", typeof(RectTransform));
            panelGo.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0, 150);
            panelRect.sizeDelta = new Vector2(900, 150);

            Button buttonA = CreateButton(panelGo.transform, "OptionAButton", "1", new Vector2(-230, 0));
            Button buttonB = CreateButton(panelGo.transform, "OptionBButton", "2", new Vector2(230, 0));

            GameObject responseUiGo = new GameObject("ResponseUI");
            ResponseUI responseUI = responseUiGo.AddComponent<ResponseUI>();

            SetObjectField(responseUI, "panel", panelGo);
            SetObjectField(responseUI, "optionAButton", buttonA);
            SetObjectField(responseUI, "optionBButton", buttonB);
            SetObjectField(responseUI, "optionALabel", buttonA.GetComponentInChildren<Text>());
            SetObjectField(responseUI, "optionBLabel", buttonB.GetComponentInChildren<Text>());

            return responseUI;
        }

        private static FingerTracker CreateFingerTrackerRig()
        {
            GameObject dummyGo = new GameObject("DummyKeyboardFingerInputSource");
            DummyKeyboardFingerInputSource dummy = dummyGo.AddComponent<DummyKeyboardFingerInputSource>();

            GameObject trackerGo = new GameObject("FingerTracker");
            FingerTracker tracker = trackerGo.AddComponent<FingerTracker>();
            SetObjectField(tracker, "positionProviderBehaviour", dummy);

            return tracker;
        }

        // ==================== 視覚オブジェクト生成 ====================

        private static VisualController CreateElasticityVisuals(out GameObject bowlGo)
        {
            GameObject rootGo = new GameObject("VisualController");
            VisualController visualController = rootGo.AddComponent<VisualController>();

            GameObject minimalGo = CreateMinimalSphere(rootGo.transform);
            GameObject richGo = new GameObject("RichVisual");
            richGo.transform.SetParent(rootGo.transform, false);

            bowlGo = CreateBowlObject(richGo.transform);

            SetObjectField(visualController, "minimalVisual", minimalGo);
            SetObjectField(visualController, "richVisual", richGo);

            return visualController;
        }

        private static VisualController CreateViscosityVisuals(out GameObject mudGo)
        {
            GameObject rootGo = new GameObject("VisualController");
            VisualController visualController = rootGo.AddComponent<VisualController>();

            GameObject minimalGo = CreateMinimalSphere(rootGo.transform);
            GameObject richGo = new GameObject("RichVisual");
            richGo.transform.SetParent(rootGo.transform, false);

            mudGo = CreateMudBlobObject(richGo.transform);

            SetObjectField(visualController, "minimalVisual", minimalGo);
            SetObjectField(visualController, "richVisual", richGo);

            return visualController;
        }

        private static VisualController CreateIdentificationVisuals(ElasticitySurface elasticitySurface, ViscositySurface viscositySurface)
        {
            GameObject rootGo = new GameObject("VisualController");
            VisualController visualController = rootGo.AddComponent<VisualController>();

            GameObject minimalGo = CreateMinimalSphere(rootGo.transform);
            GameObject richGo = new GameObject("RichVisual");
            richGo.transform.SetParent(rootGo.transform, false);

            GameObject bowlGo = CreateBowlObject(richGo.transform);
            ShowWhenSurfaceActive bowlShow = bowlGo.AddComponent<ShowWhenSurfaceActive>();
            SetObjectField(bowlShow, "surface", elasticitySurface);

            GameObject mudGo = CreateMudBlobObject(richGo.transform);
            mudGo.transform.localPosition = new Vector3(0.15f, 0f, 0f);
            ShowWhenSurfaceActive mudShow = mudGo.AddComponent<ShowWhenSurfaceActive>();
            SetObjectField(mudShow, "surface", viscositySurface);

            SetObjectField(visualController, "minimalVisual", minimalGo);
            SetObjectField(visualController, "richVisual", richGo);

            return visualController;
        }

        private static GameObject CreateMinimalSphere(Transform parent)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "MinimalVisual";
            sphere.transform.SetParent(parent, false);
            sphere.transform.localScale = Vector3.one * 0.1f;
            sphere.GetComponent<Renderer>().sharedMaterial = GetNeutralMaterial();
            return sphere;
        }

        private static GameObject CreateBowlObject(Transform parent)
        {
            GameObject bowlGo = new GameObject("Bowl");
            bowlGo.transform.SetParent(parent, false);

            Mesh bowlMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Models/Bowl_Procedural.asset");
            if (bowlMesh == null)
            {
                bowlMesh = ProceduralPotteryMeshGenerator.GenerateBowlMesh();
            }

            bowlGo.AddComponent<MeshFilter>().sharedMesh = bowlMesh;
            bowlGo.AddComponent<MeshRenderer>().sharedMaterial = GetOrCreateClayMaterial();

            return bowlGo;
        }

        private static GameObject CreateMudBlobObject(Transform parent)
        {
            GameObject mudGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mudGo.name = "MudBlob";
            mudGo.transform.SetParent(parent, false);
            mudGo.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
            mudGo.GetComponent<Renderer>().sharedMaterial = GetOrCreateMudMaterial();
            return mudGo;
        }

        private static Material GetNeutralMaterial()
        {
            if (neutralMaterialCache != null)
            {
                return neutralMaterialCache;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            neutralMaterialCache = new Material(shader) { color = new Color(0.6f, 0.6f, 0.6f) };
            return neutralMaterialCache;
        }

        private static Material GetOrCreateClayMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ClayMaterialPath);
            if (material != null)
            {
                return material;
            }

            Directory.CreateDirectory("Assets/_Project/Materials");
            material = new Material(Shader.Find("PotteryHaptics/ClayDeformation"));
            AssetDatabase.CreateAsset(material, ClayMaterialPath);
            return material;
        }

        private static Material GetOrCreateMudMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MudMaterialPath);
            if (material != null)
            {
                return material;
            }

            Directory.CreateDirectory("Assets/_Project/Materials");
            material = new Material(Shader.Find("PotteryHaptics/MudSurface"));
            AssetDatabase.CreateAsset(material, MudMaterialPath);
            return material;
        }

        // ==================== SerializedObject経由のフィールド設定 ====================

        private static void SetObjectField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[SceneBuilder] Field not found: {target.GetType().Name}.{fieldName}");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetFloatField(Object target, string fieldName, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).floatValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetStringField(Object target, string fieldName, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).stringValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetObjectArrayField(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedProperties();
        }
    }
}
