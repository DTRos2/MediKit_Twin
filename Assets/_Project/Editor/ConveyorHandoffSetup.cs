using System;
using System.Collections.Generic;
using ConveyorSystem;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Conveyor;
using MediKitTwin.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 키팅이 끝난 박스가 컨베이어를 타고 팩킹 구역을 거쳐 끝까지 가도록 열린 씬(Scene_Cell)을 준비한다. (FR-05)
    /// 1) 박스가 멈출 자리(Sensor_Packing 아래 PackingStopPoint)를 정한다. 팔 B 시험용으로 손으로 놓아 둔 키트 박스가 있으면 그 자리가 곧
    ///    팔 B가 맞춰진 자리이므로 거기에 만들고, 이미 만들어 둔 것이 있으면 그대로 둔다(사람이 옮겼을 수 있다).
    /// 2) 멈출 자리를 품은 컨베이어 구간을 PauserConveyor로 바꾼다. 같은 자리·같은 모양이라 앞뒤 구간과 그대로 이어진다.
    ///    멈췄다 출발하는 일은 이 구간의 IsActive로 다룬다.
    /// 3) 시험용 키트 박스(Sensor_Packing 아래 KitPackage)를 지운다. 남겨 두면 팔 B가 컨베이어로 온 박스 대신 이것을 덮는다.
    /// 4) KitBoxDispatcher · PackingStop · ConveyorEndNotifier를 새 오브젝트(ConveyorHandoff)에 붙여 연결한다.
    ///    예전 임시 장치 ConveyorHandoffStub는 스크립트를 지웠으므로, 키팅 스테이션에 남은 "스크립트 없음" 컴포넌트를 함께 정리한다.
    /// 구간은 이름이 아니라 지그·센서·멈출 자리의 위치로 고른다. 이미 되어 있으면 건너뛰므로 여러 번 실행해도 된다.
    /// </summary>
    public static class ConveyorHandoffSetup
    {
        private const string DummyBoxGroupName = "KitPackage";
        private const string HandoffObjectName = "ConveyorHandoff";

        [MenuItem("Tools/MediKit/Setup Conveyor Handoff")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[ConveyorHandoffSetup] 플레이 모드를 끈 뒤 실행하세요.");
                return;
            }

            bool done = TrySetup();
            Debug.Log($"[ConveyorHandoffSetup] {(done ? "완료. 씬을 확인한 뒤 저장하세요." : "중단 — 위 오류를 확인하세요.")}");
        }

        /// <summary>배치 모드용: Scene_Cell을 열어 설정하고 저장한다. -executeMethod로 부른다.</summary>
        public static void SetupAndSave()
        {
            EditorSceneManager.OpenScene(ProjectAssetPaths.CellScene, OpenSceneMode.Single);
            bool done = TrySetup();
            if (done)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log($"[ConveyorHandoffSetup] {(done ? "설정하고 저장했습니다." : "중단했습니다.")}");
            EditorApplication.Exit(done ? 0 : 1);
        }

        private static bool TrySetup()
        {
            if (!SceneObjectFinder.TryFindSingle(out KitBoxSupply supply, "KitBoxSupply (키팅 지그)"))
            {
                return false;
            }

            Transform packingSensor = FindByName(CellObjectNames.PackingSensor);
            Transform endSensor = FindByName(CellObjectNames.ConveyorEndSensor);
            Transform managers = FindByName(CellObjectNames.Managers);
            if (packingSensor == null || endSensor == null || managers == null)
            {
                Debug.LogError($"[ConveyorHandoffSetup] 씬에 {CellObjectNames.PackingSensor} {(packingSensor != null)}, {CellObjectNames.ConveyorEndSensor} {(endSensor != null)}, {CellObjectNames.Managers} {(managers != null)} — 모두 있어야 한다.");
                return false;
            }

            // 멈출 자리는 시험용 박스를 지우기 전에 읽어 둔다.
            Transform stopPoint = FindOrCreateStopPoint(packingSensor);

            ConveyorBase packing = SegmentContaining(stopPoint.position);
            if (packing == null)
            {
                Debug.LogError("[ConveyorHandoffSetup] 멈출 자리(PackingStopPoint)를 품은 컨베이어 구간이 없다. 자리를 컨베이어 위로 옮긴 뒤 다시 실행하세요.");
                return false;
            }

            PauserConveyor pauser = packing as PauserConveyor ?? SwapToPauser(packing);
            if (pauser == null)
            {
                return false;
            }

            // 구간을 바꾼 뒤에 찾아야, 바뀐 구간을 입구나 끝 구간으로 잡았더라도 사라진 옛 구간을 참조하지 않는다.
            ConveyorBase entry = NearestConveyor(supply.transform.position);
            ConveyorBase last = NearestConveyor(endSensor.position);
            if (entry == null || last == null)
            {
                Debug.LogError("[ConveyorHandoffSetup] 씬에서 입구나 끝 컨베이어 구간을 찾지 못했다.");
                return false;
            }

            RemoveDummyBox(packingSensor);
            RemoveMissingStubScripts();

            GameObject host = FindOrCreateHost(managers);
            bool wired = Wire(host, supply, entry, pauser, packingSensor, stopPoint, last);
            EditorSceneManager.MarkSceneDirty(host.scene);
            return wired;
        }

        // 멈출 자리 오브젝트. 이미 있으면 그대로 쓰고, 없으면 시험용 박스(없으면 센서) 자리에 만든다.
        private static Transform FindOrCreateStopPoint(Transform packingSensor)
        {
            Transform existing = packingSensor.Find(CellObjectNames.PackingStopPoint);
            if (existing != null)
            {
                return existing;
            }

            Transform dummyBox = FindKitBoxUnder(packingSensor);
            Vector3 position = dummyBox != null ? dummyBox.position : packingSensor.position;

            var stopPoint = new GameObject(CellObjectNames.PackingStopPoint).transform;
            stopPoint.SetParent(packingSensor, false);
            stopPoint.position = position;
            Undo.RegisterCreatedObjectUndo(stopPoint.gameObject, "Create PackingStopPoint");
            Debug.Log($"[ConveyorHandoffSetup] 멈출 자리 {CellObjectNames.PackingStopPoint}를 {(dummyBox != null ? "시험용 박스 자리" : "센서 자리")}({position})에 만들었습니다.");
            return stopPoint;
        }

        private static Transform FindKitBoxUnder(Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != root && child.name.IndexOf(CellObjectNames.KitBoxKey, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return child;
                }
            }

            return null;
        }

        // 입력점에서 출력점까지의 선분 안에 점이 놓이는 구간 가운데, 옆으로 가장 가까운 구간.
        private static ConveyorBase SegmentContaining(Vector3 point)
        {
            ConveyorBase best = null;
            float bestLateral = float.MaxValue;
            foreach (ConveyorBase conveyor in Object.FindObjectsByType<ConveyorBase>(FindObjectsInactive.Include))
            {
                if (conveyor.InputPoint == null || conveyor.OutputPoints == null || conveyor.OutputPoints.Length == 0 || conveyor.OutputPoints[0] == null)
                {
                    continue;
                }

                bool inside = SegmentProjection.TryProject(conveyor.InputPoint.position, conveyor.OutputPoints[0].position, point, out _, out float lateral);
                if (inside && lateral < bestLateral)
                {
                    bestLateral = lateral;
                    best = conveyor;
                }
            }

            return best;
        }

        // 위치(가로)에서 가장 가까운 컨베이어 구간. 구간 가운데를 기준으로 하므로 구간 사이 경계에 놓인 물건도 한쪽으로 정해진다.
        private static ConveyorBase NearestConveyor(Vector3 position)
        {
            ConveyorBase nearest = null;
            float best = float.MaxValue;
            foreach (ConveyorBase conveyor in Object.FindObjectsByType<ConveyorBase>(FindObjectsInactive.Include))
            {
                Vector3 offset = conveyor.CenterPoint.position - position;
                offset.y = 0f;
                if (offset.sqrMagnitude < best)
                {
                    best = offset.sqrMagnitude;
                    nearest = conveyor;
                }
            }

            return nearest;
        }

        private static PauserConveyor SwapToPauser(ConveyorBase old)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.PauserConveyorPrefab);
            if (prefab == null)
            {
                Debug.LogError($"[ConveyorHandoffSetup] 프리팹이 없다: {ProjectAssetPaths.PauserConveyorPrefab}");
                return null;
            }

            Transform oldTransform = old.transform;
            var replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, oldTransform.gameObject.scene);
            replacement.transform.SetParent(oldTransform.parent, false);
            replacement.transform.SetPositionAndRotation(oldTransform.position, oldTransform.rotation);
            replacement.transform.localScale = oldTransform.localScale;
            replacement.transform.SetSiblingIndex(oldTransform.GetSiblingIndex());
            replacement.name = oldTransform.name;

            var pauser = replacement.GetComponent<PauserConveyor>();

            // 속도는 옛 구간과 같게 하고, 멈춤은 PackingStop이 정하므로 자체 대기 시간은 두지 않는다.
            var serialized = new SerializedObject(pauser);
            serialized.FindProperty(nameof(ConveyorBase.Speed)).floatValue = old.Speed;
            serialized.FindProperty(nameof(PauserConveyor.PauseDuration)).floatValue = 0f;
            serialized.ApplyModifiedProperties();

            Undo.DestroyObjectImmediate(oldTransform.gameObject);
            Debug.Log($"[ConveyorHandoffSetup] {replacement.name} 구간을 PauserConveyor로 바꿨습니다.");
            return pauser;
        }

        // 팩킹 센서 아래에서 이름이 KitPackage인 자식(손으로 놓은 시험용 키트 박스)을 지운다.
        private static void RemoveDummyBox(Transform packingSensor)
        {
            Transform dummy = packingSensor.Find(DummyBoxGroupName);
            if (dummy == null)
            {
                return;
            }

            Undo.DestroyObjectImmediate(dummy.gameObject);
            Debug.Log($"[ConveyorHandoffSetup] {CellObjectNames.PackingSensor} 아래의 시험용 {DummyBoxGroupName}을 지웠습니다.");
        }

        // 지운 ConveyorHandoffStub가 키팅 스테이션(KittingController가 있는 오브젝트)에 "스크립트 없음"으로 남아 있으면 떼어 낸다.
        // 다른 오브젝트의 빈 컴포넌트는 사람이 고칠 수 있으니 건드리지 않는다.
        private static void RemoveMissingStubScripts()
        {
            foreach (KittingController station in Object.FindObjectsByType<KittingController>(FindObjectsInactive.Include))
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(station.gameObject);
                if (removed > 0)
                {
                    Debug.Log($"[ConveyorHandoffSetup] {station.name}에서 스크립트가 없는 컴포넌트 {removed}개(옛 ConveyorHandoffStub)를 뗐습니다.");
                }
            }
        }

        private static GameObject FindOrCreateHost(Transform managers)
        {
            Transform existing = managers.Find(HandoffObjectName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var host = new GameObject(HandoffObjectName);
            host.transform.SetParent(managers, false);
            Undo.RegisterCreatedObjectUndo(host, "Create ConveyorHandoff");
            return host;
        }

        private static bool Wire(GameObject host, KitBoxSupply supply, ConveyorBase entry, PauserConveyor pauser, Transform packingSensor, Transform stopPoint, ConveyorBase last)
        {
            KitBoxDispatcher dispatcher = GetOrAdd<KitBoxDispatcher>(host);
            PackingStop stop = GetOrAdd<PackingStop>(host);
            ConveyorEndNotifier endNotifier = GetOrAdd<ConveyorEndNotifier>(host);

            // 속도는 참조 없이 컴포넌트 하나로 정한다. 이미 있으면 사람이 인스펙터에서 바꾼 값을 그대로 둔다.
            GetOrAdd<ConveyorSpeedSetter>(host);

            // 속도를 화면에서 조절하는 패널. 속도 조절 컴포넌트는 인터페이스로 찾으므로 연결할 참조가 없다.
            GetOrAdd<ConveyorSpeedDemoPanel>(host);

            return SerializedFieldAssigner.SetReference(dispatcher, "_boxSupply", supply)
                   & SerializedFieldAssigner.SetReference(dispatcher, "_entry", entry)
                   & SerializedFieldAssigner.SetReference(stop, "_conveyor", pauser)
                   & SerializedFieldAssigner.SetReference(stop, "_packingSensor", packingSensor)
                   & SerializedFieldAssigner.SetReference(stop, "_stopPoint", stopPoint)
                   & SerializedFieldAssigner.SetReference(endNotifier, "_lastConveyor", last);
        }

        private static T GetOrAdd<T>(GameObject host) where T : Component
        {
            var component = host.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(host);
        }

        private static Transform FindByName(string objectName)
        {
            var matches = new List<Transform>();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name == objectName && t.gameObject.scene.IsValid())
                {
                    matches.Add(t);
                }
            }

            if (matches.Count != 1)
            {
                Debug.LogError($"[ConveyorHandoffSetup] '{objectName}'가 씬에 {matches.Count}개 있다. 정확히 1개여야 한다.");
                return null;
            }

            return matches[0];
        }
    }
}
