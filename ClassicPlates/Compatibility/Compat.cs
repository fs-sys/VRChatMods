using MelonLoader;

namespace ClassicPlates.Compatibility;

public class Compat
{
    public static bool UiExpansionKit;

    public static void Init()
    {
        foreach (var mod in MelonHandler.Mods)
        {
            switch (mod.Info.Name)
            {
                case "UI Expansion Kit":
                {
                    UiExpansionKit = true;
                    ClassicPlates.Log("UI Expansion Kit found");
                    break;
                }
            }
        }
        if (UiExpansionKit != true)
        {
            ClassicPlates.Warning("This mod is completely standalone, however, UiExpansionKit is recommended for the best experience.");
        }
    }
}