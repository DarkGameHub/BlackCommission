public static class MvpMissionRuntime
{
    public static OfficeTaskDefinition ActiveTask { get; private set; }
    public static string ReturnOfficeScene { get; private set; } = "HQ";
    public static bool HasActiveTask => ActiveTask != null;

    public static void BeginMission(OfficeTaskDefinition task, string returnOfficeScene)
    {
        ActiveTask = task;
        ReturnOfficeScene = string.IsNullOrWhiteSpace(returnOfficeScene) ? "HQ" : returnOfficeScene;
    }

    public static void Clear()
    {
        ActiveTask = null;
        ReturnOfficeScene = "HQ";
    }
}
