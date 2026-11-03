using MediKitTwin.ArmA;
using MediKitTwin.ArmB;
using MediKitTwin.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 로봇암 B가 뚜껑을 물리적으로 쥐고 덮을 수 있게 준비한다. (FR-10)
    /// 1) PF_LidSupplyTable의 뚜껑(Lid_*)에 Rigidbody(키네마틱)와 Graspable을 붙인다. 쌓여 있는 동안은 LidDispenser가 움직이므로 키네마틱이고,
    ///    가져갈 때 LidCoverTask가 물리 몸으로 바꾼다.
    /// 2) 열린 씬의 UR3 팔(로봇암 B)에 LidCoverTask를 붙이고 도구 목표, 그리퍼, 뚜껑 공급대, 키트 박스 검색 위치를 연결한다.
    /// 이미 붙어 있으면 건너뛰므로 여러 번 실행해도 된다. 씬은 저장하지 않으니 확인한 뒤 직접 저장한다.
    /// </summary>
    public static class ArmBLidSetup
    {
        private const float LidMassKg = 0.2f;

        [MenuItem("Tools/MediKit/Setup ArmB Lid Grasp")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[ArmBLidSetup] 플레이 모드를 끈 뒤 실행하세요.");
                return;
            }

            int lids = SetupLidPrefab();
            bool wired = SetupArmInScene();
            Debug.Log($"[ArmBLidSetup] 뚜껑 {lids}개에 Rigidbody·Graspable 확인, 로봇암 B 연결 {(wired ? "완료" : "건너뜀")}");
        }

        // 프리팹 안의 Lid_* 뚜껑마다 Rigidbody와 Graspable이 있게 한다. 반환값은 처리한 뚜껑 수.
        private static int SetupLidPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ProjectAssetPaths.LidSupplyTablePrefab);
            int count = 0;
            try
            {
                foreach (Transform child in root.transform)
                {
                    if (!child.name.StartsWith(CellObjectNames.LidPrefix)) continue;

                    Rigidbody body = child.GetComponent<Rigidbody>();
                    if (body == null) body = child.gameObject.AddComponent<Rigidbody>();
                    body.mass = LidMassKg;
                    body.isKinematic = true;

                    if (child.GetComponent<Graspable>() == null) child.gameObject.AddComponent<Graspable>();
                    count++;
                }

                PrefabUtility.SaveAsPrefabAsset(root, ProjectAssetPaths.LidSupplyTablePrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return count;
        }

        // 열린 씬의 UR3 팔에 LidCoverTask를 붙이고 연결한다. 필요한 오브젝트가 없으면 이유를 적고 false.
        private static bool SetupArmInScene()
        {
            ArmController arm = FindArmB();
            if (arm == null)
            {
                Debug.LogError("[ArmBLidSetup] UR3 프로필을 쓰는 ArmController를 찾지 못했습니다. Scene_Cell을 연 뒤 다시 실행하세요.");
                return false;
            }

            var tool = arm.GetComponent<IkArmTarget>();
            var gripper = arm.GetComponent<JointGripper>();
            var dispenser = Object.FindFirstObjectByType<LidDispenser>();
            Transform packingSensor = FindByName(CellObjectNames.PackingSensor);
            if (tool == null || gripper == null || dispenser == null || packingSensor == null)
            {
                Debug.LogError($"[ArmBLidSetup] 연결할 대상이 없습니다. IkArmTarget {(tool != null)}, JointGripper {(gripper != null)}, LidDispenser {(dispenser != null)}, Sensor_Packing {(packingSensor != null)}");
                return false;
            }

            var task = arm.GetComponent<LidCoverTask>();
            if (task == null) task = Undo.AddComponent<LidCoverTask>(arm.gameObject);

            // 덮는 높이 계산에 쓰는 뚜껑 모양은 빌더와 같은 값으로 맞춘다.
            var serialized = new SerializedObject(task);
            serialized.FindProperty("lidHeight").floatValue = CellSceneBuilder.LidHeight;
            serialized.FindProperty("lidWall").floatValue = CellSceneBuilder.LidWall;
            serialized.ApplyModifiedProperties();

            bool ok = SerializedFieldAssigner.SetReference(task, "toolTarget", tool)
                      & SerializedFieldAssigner.SetReference(task, "gripper", gripper)
                      & SerializedFieldAssigner.SetReference(task, "dispenser", dispenser)
                      & SerializedFieldAssigner.SetReference(task, "kitBoxSearchRoot", packingSensor);

            EditorSceneManager.MarkSceneDirty(arm.gameObject.scene);
            return ok;
        }

        // 씬에는 FR5 등 다른 팔도 있으므로 UR3 프로필을 쓰는 팔(로봇암 B)을 고른다.
        private static ArmController FindArmB()
        {
            foreach (ArmController candidate in Object.FindObjectsByType<ArmController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.Profile != null && candidate.Profile.name.Contains("UR3")) return candidate;
            }

            return null;
        }

        private static Transform FindByName(string objectName)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == objectName && t.gameObject.scene.IsValid()) return t;
            }

            return null;
        }
    }
}
