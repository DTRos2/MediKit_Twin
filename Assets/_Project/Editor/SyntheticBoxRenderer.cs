using System;
using System.Collections.Generic;
using MediKitTwin.Inspection;
using MediKitTwin.Inventory;
using MediKitTwin.ML;
using MediKitTwin.Recipes;
using UnityEditor;
using UnityEngine;

// 박스 설계(SyntheticBoxCase)와 촬영용 연속 값(SyntheticVariation)으로 박스를 만들어 검사 카메라 시점에서 촬영한다. (FR-08)
// 열려 있는 씬을 건드리지 않도록 에디터의 격리된 미리보기 씬(PreviewRenderUtility)에서만 만들고 찍는다.
// 박스는 지그 프리팹의 KitBox를 복제하고, 구성품은 ShelfItemLooks의 모양과 색으로 선반과 같은 그리퍼 기준 크기로 놓는다.
public sealed class SyntheticBoxRenderer : IDisposable
{
    private const string JigPrefabPath = "Assets/_Project/Prefabs/PF_KittingJig.prefab";
    private const string BoxName = "KitBox";
    private const string ItemMaterialDir = "Assets/_Project/Materials";

    // 실제 씬(Scene_Cell)의 Directional Light 색. 학습 이미지가 이 색을 포함해야 씬에서 찍은 크롭을 맞힌다.
    private static readonly Color SceneLightColor = new Color(1f, 0.95686275f, 0.8392157f);
    // 씬 Directional Light의 각도(Euler 50, -30, 0)와 같은 값. 검사 카메라는 이미지 위쪽을 박스의 +Z에 맞추고, 박스는 인스펙션 자리에서
    // 항상 비슷한 방향이라 박스 기준 조명 방향이 거의 고정이다. 그림자는 없지만(검사 카메라가 끈다) 칸막이와 벽의 옆면이 비스듬히 보여서,
    // 빛을 등진 옆면이 어두운 띠로 찍힌다. 이 띠가 서는 쪽이 조명 방향에 달려 있어 씬과 같은 방향으로 맞춘다.
    // 씬 사진에서 위 벽 안쪽은 밝고 아래 벽 안쪽과 오른쪽 벽 안쪽은 어둡다.
    private const float KeyLightPitch = 50f;
    private const float KeyLightYaw = -30f;
    private readonly PreviewRenderUtility _util = new PreviewRenderUtility();
    private readonly SyntheticLighting _lighting;
    private readonly TopDownCameraModel _cameraModel;
    private readonly InspectionCameraSettingsSO _cameraSettings;
    private readonly ShelfItemLooksSO _looks;
    private readonly GraspSizeLimit _graspLimit;
    private readonly Dictionary<string, RecipeSO> _recipesByKit;
    private readonly GameObject _boxTemplate;
    private readonly float _boxFloorLocalY;
    private Color? _keyBaseColor;
    private Color? _fillBaseColor;
    private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
    private readonly Dictionary<Material, Material> _boxMaterials = new Dictionary<Material, Material>();

    public SyntheticBoxRenderer(
        InspectionCameraSettingsSO cameraSettings,
        ShelfItemLooksSO looks,
        ShelfLayoutConfigSO shelfLayout,
        IEnumerable<RecipeSO> recipes,
        float boxFloorThickness,
        SyntheticLighting lighting = null)
    {
        _lighting = lighting ?? SyntheticLighting.Default;
        _cameraSettings = cameraSettings;
        _cameraModel = cameraSettings.CreateModel();
        _looks = looks;
        _graspLimit = shelfLayout.CreateItemSizeLimit();

        _recipesByKit = new Dictionary<string, RecipeSO>();
        foreach (RecipeSO recipe in recipes)
        {
            _recipesByKit[recipe.KitCode] = recipe;
        }

        var jig = AssetDatabase.LoadAssetAtPath<GameObject>(JigPrefabPath);
        Transform source = jig != null ? jig.transform.Find(BoxName) : null;
        if (source == null)
        {
            throw new InvalidOperationException($"{JigPrefabPath} 안에 {BoxName}이(가) 없다. 'Rebuild KittingJig Prefab'으로 지그를 먼저 만든다.");
        }

        _boxTemplate = source.gameObject;
        _boxFloorLocalY = MeasureBoxFloorTop(_boxTemplate, boxFloorThickness);

        var camera = _util.camera;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = InspectionBackground.Color;
        camera.orthographic = false;
        camera.fieldOfView = cameraSettings.VerticalFovDegrees;
        camera.aspect = (float)cameraSettings.ImageWidth / cameraSettings.ImageHeight;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 5f;
        camera.transform.position = new Vector3(0f, cameraSettings.HeightAboveBox, 0f);
        camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        _util.ambientColor = _lighting.Ambient;
    }

    public TopDownCameraModel CameraModel => _cameraModel;

    /// <summary>박스 하나를 촬영한 전체 이미지(검사 카메라 해상도). 호출한 쪽이 Texture를 지운다.</summary>
    public Texture2D Render(SyntheticBoxCase boxCase, SyntheticVariation variation)
    {
        GameObject root = BuildBox(boxCase, variation);
        try
        {
            ApplyLights(variation);
            ApplyColorVariation(root, variation);

            var rect = new Rect(0f, 0f, _cameraSettings.ImageWidth, _cameraSettings.ImageHeight);
            _util.BeginStaticPreview(rect);
            // camera.Render()를 직접 부르면 미리보기 장면의 환경광 설정(ambientColor)이 적용되지 않아 빛을 등진 면이 항상 일정하게 밝았다.
            // PreviewRenderUtility.Render가 환경광을 적용한 뒤 찍는다. URP로 그리려면 첫 인자를 true로 준다.
            _util.Render(allowScriptableRenderPipeline: true, updatefov: false);
            return _util.EndStaticPreview();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    public void Dispose()
    {
        _util.Cleanup();
        foreach (Material material in _materials.Values)
        {
            if (material != null && !EditorUtility.IsPersistent(material))
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        _materials.Clear();

        foreach (Material copy in _boxMaterials.Values)
        {
            if (copy != null)
            {
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        _boxMaterials.Clear();
    }

    private GameObject BuildBox(SyntheticBoxCase boxCase, SyntheticVariation variation)
    {
        if (!_recipesByKit.TryGetValue(boxCase.KitCode, out RecipeSO recipe))
        {
            throw new InvalidOperationException($"키트 {boxCase.KitCode}의 레시피가 없다.");
        }

        GameObject box = UnityEngine.Object.Instantiate(_boxTemplate);
        box.name = BoxName;
        box.hideFlags = HideFlags.HideAndDontSave;
        // 촬영에는 모양만 필요하다. SlotSensor처럼 콜라이더를 요구하는 스크립트가 있으면 콜라이더를 못 지우므로 스크립트부터 지운다.
        foreach (MonoBehaviour script in box.GetComponentsInChildren<MonoBehaviour>(true))
        {
            UnityEngine.Object.DestroyImmediate(script);
        }

        foreach (Collider collider in box.GetComponentsInChildren<Collider>(true))
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        foreach (Renderer renderer in box.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterials = Array.ConvertAll(renderer.sharedMaterials, GetReflectionFreeBoxMaterial);
        }

        box.transform.SetPositionAndRotation(
            new Vector3(variation.BoxOffset.x, 0f, variation.BoxOffset.y),
            Quaternion.Euler(0f, variation.BoxYawDegrees, 0f));
        box.transform.localScale = Vector3.one;

        for (int slot = 1; slot <= boxCase.SlotCount; slot++)
        {
            string componentId = boxCase.GetActualComponentId(slot);
            if (componentId == SlotCropRecord.EmptyLabel)
            {
                continue;
            }

            if (!recipe.TryGetItemBySlot(slot, out RecipeItem item))
            {
                throw new InvalidOperationException($"{recipe.name}: 슬롯 {slot}이 없다.");
            }

            AddItem(box.transform, componentId, item.SlotLocalPosition, variation, slot);
        }

        _util.AddSingleGO(box);
        return box;
    }

    private void AddItem(Transform box, string componentId, Vector3 slotLocal, SyntheticVariation variation, int slotNumber)
    {
        Vector2 offset = variation.GetItemOffset(slotNumber);
        float standingYawDegrees = variation.GetItemYawDegrees(slotNumber);
        float lyingYawDegrees = variation.GetItemLyingYawDegrees(slotNumber);
        bool lying = variation.IsItemLying(slotNumber);
        ShelfItemLook look = _looks.GetLook(componentId, out _);
        float scale = _graspLimit.ScaleFor(look.Size.x, look.Size.y, look.Size.z);
        Vector3 size = look.Size * scale;

        PrimitiveType primitive = look.Shape == ShelfItemShape.Cylinder ? PrimitiveType.Cylinder : PrimitiveType.Cube;

        // 눕는 것은 원기둥만이다. 판이나 상자는 넓은 면으로 놓이므로 서 있는 모양 그대로이다.
        // 누우면 축이 수평이 되어 바닥에서 닿는 높이는 길이가 아니라 지름(X)의 절반이다.
        bool onSide = lying && primitive == PrimitiveType.Cylinder;
        // 누운 각도(±90도)는 눕는 원기둥에만 쓴다. 판과 상자는 눕는 확률에 걸려도 서 있을 때의 각도(±20도)를 쓴다.
        // 넓은 범위로 돌리면 직사각형이 임의 각도로 놓여 정사각형(거즈)과 구별하기 어려워진다.
        float yawDegrees = onSide ? lyingYawDegrees : standingYawDegrees;
        float restHeight = onSide ? size.x : size.y;
        float centerHeight = restHeight * 0.5f;
        Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f) * (onSide ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity);

        // 얇은 판과 가는 막대는 칸막이에 기대 기울거나(판은 긴 모서리로 서기도 한다) 평평하지 않게 놓인다. 두꺼운 판과 상자는 그대로이다.
        ItemFlatness flatness = primitive == PrimitiveType.Cube ? ItemPosePlanner.Classify(size) : ItemFlatness.Block;
        if (flatness != ItemFlatness.Block)
        {
            ItemPose pose = ItemPosePlanner.Plan(
                size, flatness, variation.GetItemFlatYawDegrees(slotNumber),
                variation.GetItemTiltPitchDegrees(slotNumber), variation.GetItemTiltRollDegrees(slotNumber),
                variation.IsItemOnEdge(slotNumber), variation.GetItemEdgeSign(slotNumber));
            rotation = pose.LocalRotation;
            centerHeight = pose.CenterHeight;
        }

        GameObject item = GameObject.CreatePrimitive(primitive);
        item.name = $"Item_{componentId}";
        item.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());

        item.transform.SetParent(box, false);
        item.transform.localPosition = new Vector3(slotLocal.x + offset.x, _boxFloorLocalY + centerHeight, slotLocal.z + offset.y);

        // 원기둥 프리미티브의 축은 Y이다. X로 90도 눕혀 축을 수평으로 만든 뒤 제자리에서 돌린다.
        item.transform.localRotation = rotation;

        // 원기둥 프리미티브는 높이가 scale.y의 2배이다.
        item.transform.localScale = primitive == PrimitiveType.Cylinder ? new Vector3(size.x, size.y * 0.5f, size.z) : size;
        item.GetComponent<Renderer>().sharedMaterial = GetItemMaterial(componentId, look.Color);
    }

    // 박스 템플릿의 재질을 환경 반사를 끈 복사본으로 바꾼다. 같은 원본은 복사본 하나를 같이 쓴다.
    private Material GetReflectionFreeBoxMaterial(Material source)
    {
        if (source == null)
        {
            return null;
        }

        if (!_boxMaterials.TryGetValue(source, out Material copy))
        {
            copy = ReflectionFreeMaterial.Create(source);
            _boxMaterials[source] = copy;
        }

        return copy;
    }

    private Material GetItemMaterial(string componentId, Color fallbackColor)
    {
        if (_materials.TryGetValue(componentId, out Material cached))
        {
            return cached;
        }

        var source = AssetDatabase.LoadAssetAtPath<Material>($"{ItemMaterialDir}/M_Item_{componentId}.mat");
        if (source == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            source = new Material(shader) { color = fallbackColor, hideFlags = HideFlags.HideAndDontSave };
            if (source.HasProperty("_BaseColor"))
            {
                source.SetColor("_BaseColor", fallbackColor);
            }
        }

        Material material = ReflectionFreeMaterial.Create(source);
        if (!EditorUtility.IsPersistent(source))
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        _materials[componentId] = material;
        return material;
    }

    private void ApplyLights(SyntheticVariation variation)
    {
        Light key = _util.lights[0];
        // 흰색(따뜻함 -1)에서 씬 색(0), 그보다 따뜻한 색(+1)까지.
        Color tint = Color.LerpUnclamped(Color.white, SceneLightColor, 1f + variation.LightWarmth);
        // 미리보기 조명의 기본 색은 순백이 아니라 회색이다. 덮어쓰면 밝기가 달라지므로 기본 색에 틴트를 곱한다.
        _keyBaseColor ??= key.color;
        key.color = _keyBaseColor.Value * tint;
        key.intensity = _lighting.KeyIntensity * variation.LightIntensityScale;
        key.transform.rotation = Quaternion.Euler(KeyLightPitch + variation.LightPitchOffsetDegrees, KeyLightYaw + variation.LightYawOffsetDegrees, 0f);

        Light fill = _util.lights[1];
        _fillBaseColor ??= fill.color;
        fill.color = _fillBaseColor.Value * tint;
        fill.intensity = _lighting.FillIntensity * variation.LightIntensityScale;
        fill.transform.rotation = Quaternion.Euler(40f, KeyLightYaw + 180f, 0f);
    }

    // 박스와 구성품의 색에 색조, 채도, 명도 오차를 더한다. 머티리얼 에셋은 바꾸지 않고 렌더러별 속성 블록으로만 바꾼다.
    private static void ApplyColorVariation(GameObject root, SyntheticVariation variation)
    {
        var block = new MaterialPropertyBlock();
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            Material material = renderer.sharedMaterial;
            if (material == null)
            {
                continue;
            }

            string property = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            if (!material.HasProperty(property))
            {
                continue;
            }

            Color.RGBToHSV(material.GetColor(property), out float h, out float s, out float v);
            h = Mathf.Repeat(h + variation.HueShift, 1f);
            s = Mathf.Clamp01(s * variation.SaturationScale);
            v = Mathf.Clamp01(v * variation.ValueScale);

            renderer.GetPropertyBlock(block);
            block.SetColor(property, Color.HSVToRGB(h, s, v));
            renderer.SetPropertyBlock(block);
        }
    }

    // 박스 원점(박스 중심)을 기준으로 안쪽 바닥면의 높이를 잰다. 복제본을 원점에 잠시 놓고 렌더러 경계의 아래쪽을 쓴다.
    private static float MeasureBoxFloorTop(GameObject template, float floorThickness)
    {
        GameObject probe = UnityEngine.Object.Instantiate(template);
        probe.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            probe.transform.localScale = Vector3.one;

            Renderer[] renderers = probe.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("KitBox에 렌더러가 없다.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.min.y + floorThickness;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probe);
        }
    }
}
