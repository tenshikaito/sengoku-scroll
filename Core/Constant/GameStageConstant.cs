namespace Core.Constant
{
    public static class GameStageConstant
    {
        public const string stageDirectoryName = "stage";

        private const string informationFileExtension = "info";
        private const string dataFileExtension = "dat";

        private const string mapFileName = "map";
        private const string masterFileName = "environment";
        private const string characterFileName = "character";
        private const string scenarioFileName = "scenario";

        public const string stageInformationFileName = "stage." + informationFileExtension;

        public const string mapInformationFileName = mapFileName + "." + informationFileExtension;
        public const string mapDataFileName = mapFileName + "." + dataFileExtension;

        public const string masterInformationFileName = masterFileName + "." + informationFileExtension;
        public const string masterDataFi1eName = masterFileName + "." + dataFileExtension;

        public const string characterInformationFileName = characterFileName + "." + informationFileExtension;
        public const string characterDataFileName = characterFileName + "." + dataFileExtension;

        public const string scenarioInformationFileName = scenarioFileName + "." + informationFileExtension;
        public const string scenarioDataFileName = scenarioFileName + "." + dataFileExtension;
    }
}
