using System.Collections.Generic;

public static class PlayerLoadout
{
    public static readonly string[] AvailableItems = { "手电筒", "工具箱", "临时电池" };
    public static readonly List<int> SelectedIndices = new();
    public const int MaxSlots = 2;

    public static void Toggle(int index)
    {
        if (SelectedIndices.Contains(index))
            SelectedIndices.Remove(index);
        else if (SelectedIndices.Count < MaxSlots)
            SelectedIndices.Add(index);
    }

    public static bool IsSelected(int index) => SelectedIndices.Contains(index);

    public static void Clear() => SelectedIndices.Clear();
}
