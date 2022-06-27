using System.Diagnostics.CodeAnalysis;
using VRC;

namespace ClassicPlates.Patching;

//Thank you Lily <3
[SuppressMessage("ReSharper", "NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract")]
public static class NetworkManagerHooks
{
    private static Action<Player>? _eventHandlerA;
    private static Action<Player>? _eventHandlerB;

    internal static void Init()
    {
        NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_0
            .field_Private_HashSet_1_UnityAction_1_T_0.Add(EventHandlerA);
        NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_1
            .field_Private_HashSet_1_UnityAction_1_T_0.Add(EventHandlerB);
    }

    private static Action<Player> EventHandlerA
    {
        get
        {
            _eventHandlerB ??= OnPlayerLeft;
            return _eventHandlerA ??= OnPlayerJoin;
        }
    }

    private static Action<Player> EventHandlerB
    {
        get
        {
            _eventHandlerA ??= OnPlayerLeft;
            return _eventHandlerB ??= OnPlayerJoin;
        }
    }

    private static void OnPlayerJoin(Player plr)
    {
        PhotonUtils.ApplyModerations(plr.prop_String_0);
    }

    private static void OnPlayerLeft(Player plr)
    {
        ClassicPlates.NameplateManager?.RemoveNameplate(plr._vrcplayer);
    }

}