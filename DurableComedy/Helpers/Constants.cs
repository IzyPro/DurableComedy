namespace DurableComedy.Helpers
{
    public static class Constants
    {
        public const string ContainerGroupName = "durable-comedy-cg";
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
