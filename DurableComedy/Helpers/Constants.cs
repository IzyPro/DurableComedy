namespace DurableComedy.Helpers
{
    public static class Constants
    {
        public const string ResourceGroupName = "DurableComedyRG";
        public const string ContainerGroupName = "CG_DurableComedy";
        public const string InstanceID = "DurableComedyInstanceID";
    }

    public static class Function
    {
        // FUNCTION NAMES
        public const string Start = "Start";
        public const string DeleteCG = "Delete_ACI_Group";
        public const string JobFinished = "Job_Finished_Event";
        public const string OrchestrateCG = "Orchestrate_ACI_Group";
        public const string RunOrchestrator = "Run_Orchestrator";
    }
}
