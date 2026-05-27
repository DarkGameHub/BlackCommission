public static class MvpMissionRuntime
{
    public static OfficeTaskDefinition ActiveTask { get; private set; }
    public static string ReturnOfficeScene { get; private set; } = "HQ";
    public static bool HasActiveTask => ActiveTask != null;
    public static OfficeTaskDefinition SelectedTask { get; private set; }
    public static string SelectedReturnOfficeScene { get; private set; } = "HQ";
    public static bool HasSelectedTask => SelectedTask != null;

    public static void SelectMission(OfficeTaskDefinition task, string returnOfficeScene)
    {
        SelectedTask = task;
        SelectedReturnOfficeScene = string.IsNullOrWhiteSpace(returnOfficeScene) ? "HQ" : returnOfficeScene;
    }

    public static void BeginMission(OfficeTaskDefinition task, string returnOfficeScene)
    {
        ActiveTask = task;
        ReturnOfficeScene = string.IsNullOrWhiteSpace(returnOfficeScene) ? "HQ" : returnOfficeScene;
        SelectedTask = null;
        SelectedReturnOfficeScene = "HQ";
    }

    public static void Clear()
    {
        ActiveTask = null;
        ReturnOfficeScene = "HQ";
        SelectedTask = null;
        SelectedReturnOfficeScene = "HQ";
    }
}
