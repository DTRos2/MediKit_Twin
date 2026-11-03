namespace MediKitTwin.Common
{
    /// <summary>
    /// 프로젝트 안 에셋의 경로를 한곳에 모은다. 씬·프리팹 경로를 파일마다 적어 두면 폴더를 옮길 때 빠뜨린 곳이 조용히 깨지므로,
    /// 에디터 도구와 시험은 모두 여기서 읽는다. 에디터 도구와 PlayMode 시험이 함께 쓰므로 런타임 어셈블리(Common)에 둔다.
    /// </summary>
    public static class ProjectAssetPaths
    {
        public const string AssetsRoot = "Assets";

        // 우리가 만든 에셋. 폴더 이름은 AssetDatabase.CreateFolder(부모, 이름)에 쓰려고 따로 둔다.
        public const string ProjectRoot = AssetsRoot + "/_Project";
        public const string ScenesFolder = ProjectRoot + "/Scenes";
        public const string PrefabsFolderName = "Prefabs";
        public const string PrefabsFolder = ProjectRoot + "/" + PrefabsFolderName;
        public const string ConfigFolderName = "Config";
        public const string ConfigFolder = ProjectRoot + "/" + ConfigFolderName;
        public const string ModelsFolderName = "Models";
        public const string ModelsFolder = ProjectRoot + "/" + ModelsFolderName;
        public const string MaterialsFolder = ProjectRoot + "/Materials";

        public const string CellScene = ScenesFolder + "/Scene_Cell.unity";
        public const string ArmAPrefab = PrefabsFolder + "/ArmA.prefab";
        public const string LidSupplyTablePrefab = PrefabsFolder + "/PF_LidSupplyTable.prefab";
        public const string KittingJigPrefab = PrefabsFolder + "/PF_KittingJig.prefab";
        public const string LabelStationPrefab = PrefabsFolder + "/PF_LabelStation.prefab";
        public const string ShelfPrefab = PrefabsFolder + "/PF_Shelf.prefab";
        public const string ShelfLayoutConfig = ConfigFolder + "/ShelfLayoutConfig.asset";
        public const string KitBoxPartitionConfig = ConfigFolder + "/KitBoxPartitionConfig.asset";

        // 가져다 쓴 외부 에셋(고치지 않는다).
        public const string ConveyorPrefabsFolder = AssetsRoot + "/Conveyor System/Prefab";
        public const string PauserConveyorPrefab = ConveyorPrefabsFolder + "/PauserConveyor.prefab";
        public const string WarehouseRobotPrefab = AssetsRoot + "/Warehouse_Robot_Controller/Prefabs/robot_boxy.prefab";
        public const string Fr5DescriptionFolder = AssetsRoot + "/fr5_description";
    }
}
