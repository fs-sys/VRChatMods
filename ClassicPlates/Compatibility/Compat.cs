using MelonLoader;

namespace ClassicPlates.Compatibility;

public static class Compat
{
    private static bool _uiExpansionKit;

    public static void Init()
    {
        foreach (var mod in MelonHandler.Mods)
        {
            switch (mod.Info.Name)
            {
                case "UI Expansion Kit":
                {
                    _uiExpansionKit = true;
                    ClassicPlates.Log("UI Expansion Kit found");
                    break;
                }
            }
        }
        if (_uiExpansionKit != true)
        {
            ClassicPlates.Warning("This mod is completely standalone, however, UiExpansionKit is recommended for the best experience.");
        }
    }
}