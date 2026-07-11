/// <summary>Language-aware access to authored commission content.</summary>
public static class OfficeTaskText
{
    public static string Title(OfficeTaskDefinition task) => Pick(task?.titleEnglish, task?.title, "UNLOCALIZED COMMISSION");
    public static string Client(OfficeTaskDefinition task) => Pick(task?.clientEnglish, task?.client, "UNDISCLOSED CLIENT");
    public static string Description(OfficeTaskDefinition task) => Pick(task?.descriptionEnglish, task?.description, "NO ENGLISH BRIEF ON FILE");
    public static string Location(OfficeTaskDefinition task) => Pick(task?.locationNameEnglish, task?.locationName, "UNDISCLOSED SITE");
    public static string Scrawl(OfficeTaskDefinition task)
    {
        string primary = MvpLocale.IsEnglish ? task?.clientScrawl : task?.clientScrawlChinese;
        if (!string.IsNullOrWhiteSpace(primary)) return primary;
        if (MvpLocale.IsEnglish) return "NO CLIENT ANNOTATION";
        return !string.IsNullOrWhiteSpace(task?.clientScrawl) ? task.clientScrawl : string.Empty;
    }

    static string Pick(string english, string chinese, string missingEnglish)
    {
        if (MvpLocale.IsEnglish)
            return !string.IsNullOrWhiteSpace(english) ? english : missingEnglish;
        return !string.IsNullOrWhiteSpace(chinese) ? chinese : english ?? string.Empty;
    }
}
