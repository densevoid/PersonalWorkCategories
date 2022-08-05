using System.Collections.Generic;

namespace HandyUI_PersonalWorkCategories.Utils;

public static class CommonUtils
{
    public static void CheckNameForUnique(ref string name, List<string> list, bool addSpace = true)
    {
        var compareName = name;
        var i = 1;
        while (list.Contains(compareName))
        {
            i++;
            compareName = name + (addSpace ? " " : "") + i;
        }

        name = compareName;
    }
}