
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Realtime;
using VRC.Core;
using VRC.Management;
using VRC.UI;

namespace ClassicPlates.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class Patching
{
    private static readonly HarmonyLib.Harmony _instance = new HarmonyLib.Harmony("ClassicPlates");

    //Many patches are based on VRChat Utility Kit. Thank you Sleepers and loukylor.
    public static void Init()
    {
        var _localAddFriend = typeof(APIUser).GetMethod("LocalAddFriend");
        var _onFriend = typeof(Patching).GetMethod(nameof(OnFriend),BindingFlags.NonPublic | BindingFlags.Static);
        
        var _unfriendUser = typeof(APIUser).GetMethod("UnfriendUser");
        var _unfriendUserRemote = typeof(FriendsListManager).GetMethod("Method_Private_Void_String_0");
        var _onUnfriend = typeof(Patching).GetMethod(nameof(OnUnfriend),BindingFlags.NonPublic | BindingFlags.Static);

        var _onMasterClientSwitch = typeof(NetworkManager).GetMethod("OnMasterClientSwitched");
        var _OnMasterChange = typeof(Patching).GetMethod(nameof(OnMasterChange),BindingFlags.NonPublic | BindingFlags.Static);

        var _VRCPlayerAwake = typeof(VRCPlayer).GetMethod("Awake");
        var _OnVRCPlayerAwake = typeof(Patching).GetMethod(nameof(OnPlayerAwake),BindingFlags.NonPublic | BindingFlags.Static);

        var _ModerationSend1 = typeof(ModerationManager).GetMethods().Where(mb => mb.Name.StartsWith("Method_Private_ApiPlayerModeration_String_String_ModerationType_")).ToList();
        var _OnModerationSend1 = typeof(Patching).GetMethod(nameof(OnPlayerModerationSend1),BindingFlags.NonPublic | BindingFlags.Static);

        var _ModerationSend2 = typeof(ModerationManager).GetMethods().Where(mb => mb.Name.StartsWith("Method_Private_Void_String_ModerationType_Action_1_ApiPlayerModeration_Action_1_String_")).ToList();
        var _OnModerationSend2 = typeof(Patching).GetMethod(nameof(OnPlayerModerationSend2),BindingFlags.NonPublic | BindingFlags.Static);

        var _ModerationRemove = typeof(ModerationManager).GetMethod("Method_Private_Void_String_ModerationType_0");
        var _OnModerationRemove = typeof(Patching).GetMethod(nameof(OnPlayerModerationRemove),BindingFlags.NonPublic | BindingFlags.Static);

        var _OnEvent = typeof(LoadBalancingClient).GetMethod("OnEvent");
        var _OnEventPatch = typeof(Patching).GetMethod(nameof(OnEvent),BindingFlags.NonPublic | BindingFlags.Static);

        var _SetNameplateMode = typeof(VRC.NameplateManager).GetMethod("Method_Public_Static_set_Void_NameplateMode_0");
        var _OnNameplateModeUpdate = typeof(Patching).GetMethod(nameof(OnNameplateModeUpdate),BindingFlags.NonPublic | BindingFlags.Static);

        var _SetStatusMode = typeof(VRC.NameplateManager).GetMethod("Method_Public_Static_set_Void_StatusMode_PDM_0");
        var _OnStatusModeUpdate = typeof(Patching).GetMethod(nameof(OnStatusModeUpdate),BindingFlags.NonPublic | BindingFlags.Static);

        var _LoadingBarProgress = typeof(AvatarLoadingBar).GetMethods().Where(mb => mb.Name.Contains("Method_Public_Void_Single_Int64_")).ToList();
        var _OnAvatarDownloadProgress = typeof(Patching).GetMethod(nameof(OnAvatarDownloadProgress),BindingFlags.NonPublic | BindingFlags.Static);
        
        
        if (_localAddFriend != null && _onFriend != null)
            _instance.Patch(_localAddFriend, null, new HarmonyMethod(_onFriend));

        if(_unfriendUser != null && _onUnfriend != null)
            _instance.Patch(_unfriendUser, null, new HarmonyMethod(_onUnfriend));

        if(_onMasterClientSwitch != null && _OnMasterChange != null)
            _instance.Patch(_onMasterClientSwitch, new HarmonyMethod(_OnMasterChange));
        
        if(_VRCPlayerAwake != null && _OnVRCPlayerAwake != null)
            _instance.Patch(_VRCPlayerAwake, null,new HarmonyMethod(_OnVRCPlayerAwake));

        if(_ModerationSend1 != null && _OnModerationSend1 != null) 
            _ModerationSend1.ForEach(info => _instance.Patch(info,null, new HarmonyMethod(_OnModerationSend1)));

        if(_ModerationSend2 != null && _OnModerationSend2 != null) 
            _ModerationSend2.ForEach(info => _instance.Patch(info,null, new HarmonyMethod(_OnModerationSend2)));
        
        if(_ModerationRemove != null && _OnModerationRemove != null)
            _instance.Patch(_ModerationRemove, null,new HarmonyMethod(_OnModerationRemove));

        if(_unfriendUserRemote != null && _onUnfriend != null)
            _instance.Patch(_unfriendUserRemote, null, new HarmonyMethod(_onUnfriend));

        if(_OnEvent != null && _OnEventPatch != null)
            _instance.Patch(_OnEvent, null,new HarmonyMethod(_OnEventPatch));

        if(_SetNameplateMode != null && _OnNameplateModeUpdate != null)
            _instance.Patch(_SetNameplateMode, null,new HarmonyMethod(_OnNameplateModeUpdate));

        if(_SetStatusMode != null && _OnStatusModeUpdate != null)
            _instance.Patch(_SetStatusMode, null,new HarmonyMethod(_OnStatusModeUpdate));
        
        //TODO: Add Avatar Loading Bar
        // if(_LoadingBarProgress != null && _OnAvatarDownloadProgress != null)
        //      _LoadingBarProgress.ForEach(info => _instance.Patch(info, null,new HarmonyMethod(_OnAvatarDownloadProgress)));
                             
    }

    private static void OnAvatarIsReady(VRCPlayer vrcPlayer)
    {
        if (vrcPlayer == null || vrcPlayer.field_Internal_GameObject_0.name.Contains("Avatar_Utility_Base_")) return;
        if (ClassicPlates.NameplateManager != null) ClassicPlates.NameplateManager.CreateNameplate(vrcPlayer);
    }

    private static void OnFriend(APIUser __0)
    {
        if (ClassicPlates.NameplateManager == null || __0 == null ||
            !ClassicPlates.NameplateManager.Nameplates.TryGetValue(__0.id, out var nameplate)) return;
        if (nameplate != null)
        {
            nameplate.IsFriend = true;
        }
    }

    private static void OnUnfriend(string __0)
    {
        if (string.IsNullOrEmpty(__0)) return;
        if (ClassicPlates.NameplateManager == null) return;
        
        var plate = ClassicPlates.NameplateManager.GetNameplate(__0);
        if (plate != null)
        {
            plate.IsFriend = false;
        }
    }
    
    private static void OnMasterChange(Player __0)
    {
        if (__0 != null && __0.field_Public_Player_0 != null)
        {
            if (ClassicPlates.NameplateManager != null)
            {
                ClassicPlates.NameplateManager.MasterClient = __0.field_Public_Player_0.field_Private_APIUser_0.id;
            }
        }
        else
        {
            ClassicPlates.Error("Master Change Detected, but player was null");
        }
    }

    private static void OnPlayerAwake(VRCPlayer __instance)
    {
        __instance.Method_Public_add_Void_OnAvatarIsReady_0(new Action(()
            => OnAvatarIsReady(__instance)
        ));
    }

    private static void OnAvatarDownloadProgress(AvatarLoadingBar __instance, float __0, long __1)
    {
    
    }

    private static void OnPlayerModerationSend1(string __1, ApiPlayerModeration.ModerationType __2)
    {
        if (__1 == null) return;
        ClassicPlates.Debug("PlayerModerationSend1");
        UpdateModeration(__1, __2);
    }
    
    private static void OnPlayerModerationSend2(string __0, ApiPlayerModeration.ModerationType __1)
    {
        if (__0 == null) return;
        ClassicPlates.Debug("OnPlayerModerationSend2");
        UpdateModeration(__0, __1);
    }
    
    private static void OnPlayerModerationRemove(string __0, ApiPlayerModeration.ModerationType __1)
    {
        if (__0 == null) return;
        ClassicPlates.Debug("OnPlayerModerationRemove");
        RemoveModeration(__0, __1);
    }
    
    private static void OnEvent(LoadBalancingClient __instance, ref EventData __0)
    {
        if (__0 == null) return;
        if (__0.Code == 33)
        {
            PhotonUtils.HandleModerationEvent(__instance, __0);
        }

        if (__0.Code == 60)
        {
            PhotonUtils.HandleInteractionEvent(__instance, __0);
        }
    }

    private static void OnNameplateModeUpdate(/*VRC.NameplateManager __instance,*/ VRC.NameplateManager.NameplateMode __0)
    {
        Settings.NameplateMode = __0;
        ClassicPlates.Debug("OnNameplateModeUpdate: " + __0);
    }
    
    private static void OnStatusModeUpdate(/*VRC.NameplateManager __instance,*/ VRC.NameplateManager.StatusMode __0)
    {
        ClassicPlates.Debug("OnStatusModeUpdate: " + __0);
        Settings.StatusMode = __0;
    }

    private static void RemoveModeration(string id, ApiPlayerModeration.ModerationType type)
    {
        ClassicPlates.Log("Moderation Removed for user: " + id + " | Type: " + type);

        if (ClassicPlates.NameplateManager == null) return;
        var oldNameplate = ClassicPlates.NameplateManager.GetNameplate(id);
        if (oldNameplate == null) return;
        switch (type)
        {
            
            // Introduces unwanted behavior when friending users
            case ApiPlayerModeration.ModerationType.Mute:
            {
                // oldNameplate.IsMuted = false;
                break;
            }

            case ApiPlayerModeration.ModerationType.Unmute:
            {
                // oldNameplate.IsMuted = true;
                break;
            }

            case ApiPlayerModeration.ModerationType.Block:
            {
                oldNameplate.IsBlocked = false;
                break;
            }
            
            case ApiPlayerModeration.ModerationType.None:
                break;
            
            case ApiPlayerModeration.ModerationType.HideAvatar:
                break;
            
            case ApiPlayerModeration.ModerationType.ShowAvatar:
                break;

            case ApiPlayerModeration.ModerationType.InteractOff:
                break;
            
            case ApiPlayerModeration.ModerationType.InteractOn:
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private static void UpdateModeration(string id, ApiPlayerModeration.ModerationType type)
    {
        ClassicPlates.Log("Moderation Sent for user: " + id + " | Type: " + type);

        if (ClassicPlates.NameplateManager == null) return;
        var oldNameplate = ClassicPlates.NameplateManager.GetNameplate(id);
        if (oldNameplate == null) return;
        switch (type)
        {
            case ApiPlayerModeration.ModerationType.Mute:
            {
                oldNameplate.IsMuted = true;
                break;
            }

            case ApiPlayerModeration.ModerationType.Unmute:
            {
                oldNameplate.IsMuted = false;
                break;
            }

            case ApiPlayerModeration.ModerationType.InteractOn:
                break;

            case ApiPlayerModeration.ModerationType.InteractOff:
                break;
            
            case ApiPlayerModeration.ModerationType.Block:
            {
                oldNameplate.IsBlocked = true;
                break;
            }

            case ApiPlayerModeration.ModerationType.None:
                break;

            case ApiPlayerModeration.ModerationType.HideAvatar:
                break;

            case ApiPlayerModeration.ModerationType.ShowAvatar:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}