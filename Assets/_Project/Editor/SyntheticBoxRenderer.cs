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

    // 고정 조명의 기준값. 랜덤화는 이 값에 배율과 각도 오차를 더한다.
    private const float KeyLightIntensity = 1.4f;
    private const float FillLightIntensity = 0.5f;
    private const float KeyLightPitch = 65f;
    private const float KeyLightYaw = 30f;
    private static readonly Color Background = new Color(0.45f, 0.47f, 0.50f);
    private static readonly Color Ambient = new Color(0.40f, 0.40f, 0.42f);

    private readonly PreviewRenderUtility _util = new PreviewRenderUtility();
    private readonly TopDownCameraModel _cameraModel;
    private readonly InspectionCameraSettingsSO _cameraSettings;
    private readonly ShelfItemLooksSO _looks;
    private readonly GraspSizeLimit _graspLimit;
    private readonly Dictionary<string, RecipeSO> _recipesByKit;
    private readonly GameObject _boxTemplate;
    private readonly float _boxFloorLocalY;
    private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();

    public SyntheticBoxRenderer(
        InspectionCameraSettingsSO cameraSettings,
        ShelfItemLooksSO looks,
        ShelfLayoutConfigSO shelfLayout,
        IEnumerable<RecipeSO> recipes,
        float boxFloorThickness)
    {
        _cameraSettings = cameraSettings;
        _cameraModel = cameraSettings.CreateModel();
        _looks = looks;
        _graspLimit = new GraspSizeLimit(shelfLayout.GripperOpeningWidthM, shelfLayout.GraspClearanceRatio);

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
        camera.backgroundColor = Background;
        camera.orthographic = false;
        camera.fieldOfView = cameraSettings.VerticalFovDegrees;
        camera.aspect = (float)cameraSettings.ImageWidth / cameraSettings.ImageHeight;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 5f;
        camera.transform.position = new Vector3(0f, cameraSettings.HeightAboveBox, 0f);
        camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        _util.ambientColor = Ambient;
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
            _util.camera.Render();
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
        foreach (Collider collider in box.GetComponentsInChildren<Collider>())
        {
            UnityEngine.Object.DestroyImmediate(collider);
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

            AddItem(box.transform, componentId, item.SlotLocalPosition, variation.GetItemOffset(slot), variation.GetItemYawDegrees(slot));
        }

        _util.AddSingleGO(box);
        return box;
    }

    private void AddItem(Transform box, string componentId, Vector3 slotLocal, Vector2 offset, float yawDegrees)
    {
        ShelfItemLook look = _looks.GetLook(componentId, out _);
        float scale = _graspLimit.ScaleFor(look.Size.x, look.Size.y, look.Size.z);
        Vector3 size = look.Size * scale;

        PrimitiveType primitive = look.Shape == ShelfItemShape.Cylinder ? PrimitiveType.Cylinder : PrimitiveType.Cube;
        GameObject item = GameObject.CreatePrimitive(primitive);
        item.name = $"Item_{componentId}";
        item.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());

        item.transform.SetParent(box, false);
        item.transform.localPosition = new Vector3(slotLocal.x + offset.x, _boxFloorLocalY + size.y * 0.5f, slotLocal.z + offset.y);
        item.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);

        // 원기둥 프리미티브는 높이가 scale.y의 2배이다.
        item.transform.localScale = primitive == PrimitiveType.Cylinder ? new Vector3(size.x, size.y * 0.5f, size.z) : size;
        item.GetComponent<Renderer>().sharedMaterial = GetItemMaterial(componentId, look.Color);
    }

    private Material GetItemMaterial(string componentId, Color fallbackColor)
    {
        if (_materials.TryGetValue(componentId, out Material cached))
        {
            return cached;
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>($"{ItemMaterialDir}/M_Item_{componentId}.mat");
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { color = fallbackColor, hideFlags = HideFlags.HideAndDontSave };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", fallbackColor);
            }
        }

        _materials[componentId] = material;
        return material;
    }

    private void ApplyLights(SyntheticVariation variation)
    {
        Light key = _util.lights[0];
        key.intensity = KeyLightIntensity * variation.LightIntensityScale;
        key.transform.rotation = Quaternion.Euler(KeyLightPitch + variation.LightPitchOffsetDegrees, KeyLightYaw + variation.LightYawOffsetDegrees, 0f);

        Light fill = _util.lights[1];
        fill.intensity = FillLightIntensity * variation.LightIntensityScale;
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
