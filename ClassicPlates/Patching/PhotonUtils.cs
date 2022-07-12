using System.Collections;
using ExitGames.Client.Photon;
using MelonLoader;
using Photon.Realtime;
using UnhollowerBaseLib;
using VRC.Core;
using Array = Il2CppSystem.Array;
using Object = Il2CppSystem.Object;

namespace ClassicPlates.Patching;

public class Moderation
{
    public int? Player;
    public bool? Blocked;
    public bool? Muted;
}

public static class PhotonUtils
{
    private static readonly Dictionary<string, Moderation> CachedModeration = new();
    private static readonly Dictionary<string, APIUser> CachedPlayers = new();
    private static LoadBalancingClient? _loadBalancingClient;
    private static readonly List<int> QueuedBlocks = new();
    private static readonly List<int> QueuedMutes = new();

    internal static void HandleModerationEvent(EventData eventData)
    {
        _loadBalancingClient ??= Photon.Pun.PhotonNetwork.field_Public_Static_LoadBalancingClient_0;

        var moderationDict = eventData.Parameters[245]
            .TryCast<Il2CppSystem.Collections.Generic.Dictionary<byte, Object>>();

        var moderation = new Moderation();
        ClassicPlates.Debug("Handling Moderation Event...");
        if (moderationDict != null)
        {
            if (moderationDict[0].Unbox<byte>() != 21)
            {
                ClassicPlates.Debug("Redundant Moderation Event, Skipping...");
                return;
            }

            if (moderationDict.ContainsKey(1))
            {
                ClassicPlates.Debug("Player Moderation Event...");

                moderation.Player = moderationDict[1].Unbox<int>();
                ClassicPlates.Debug("Player: " + moderation.Player);

                if (moderationDict.ContainsKey(10))
                {
                    var block = moderationDict[10].Unbox<bool>();
                    moderation.Blocked = block;
                    ClassicPlates.Debug(
                        $"Block Status: {(moderation.Blocked.Value ? "Blocked" : "Unblocked")}");
                }
                else
                {
                    ClassicPlates.Debug("Block Status: Null");
                }

                if (moderationDict.ContainsKey(11))
                {
                    var mute = moderationDict[11].Unbox<bool>();
                    moderation.Muted = mute;
                    ClassicPlates.Debug(
                        $"Mute Status: {(moderation.Muted.Value ? "Muted" : "Unmuted")}");
                }
                else
                {
                    ClassicPlates.Debug("Mute Status: Null");
                }
                ApplyModeration(moderation);
            }
            else
            {
                ClassicPlates.Debug("Cached Moderation Event...");
                var blocks = Il2CppArrayBase<int>.WrapNativeGenericArrayPointer(moderationDict[10].Pointer);
                var mutes = Il2CppArrayBase<int>.WrapNativeGenericArrayPointer(moderationDict[11].Pointer);

                if (blocks is {Length: > 0})
                    foreach (var i in blocks)
                    {
                        ClassicPlates.Debug($"Queued Block: {i}");
                        QueuedBlocks.Add(i);
                    }

                if (mutes is {Length: > 0})
                    foreach (var i in mutes)
                    {
                        ClassicPlates.Debug($"Queued Mute: {i}");
                        QueuedMutes.Add(i);
                    }
                
                ApplyCachedModeration();
            }
        }
        else
        {
            ClassicPlates.DebugError("Moderation Dictionary is null, Skipping...");
        }
    }
    
    public static void HandleInteractionEvent(EventData eventData)
    {
        _loadBalancingClient ??= Photon.Pun.PhotonNetwork.field_Public_Static_LoadBalancingClient_0;
        var localID = _loadBalancingClient.prop_Player_0.prop_Int32_0;
        
        var interactionArr = Il2CppArrayBase<Array>.WrapNativeGenericArrayPointer(eventData.Parameters[245].Pointer);
        if (interactionArr != null)
        {
            var interactions = new List<int>();
            foreach (var arrObj in interactionArr)
            {
                if (arrObj != null)
                {
                    var intArr = Il2CppArrayBase<int>.WrapNativeGenericArrayPointer(arrObj.Pointer).ToList();

                    if (intArr.Count > 0)
                    {
                        var key = intArr.Last();
                        if (!intArr.Remove(key))
                        {
                            ClassicPlates.Error("Failed to remove local key from interaction array");
                        }

                        if (intArr.Contains(localID))
                        {
                            interactions.Add(key);
                        }
                    }
                    else
                    {
                        ClassicPlates.Debug("Interaction Array is too short, Skipping...");
                    }
                }
                else
                {
                    ClassicPlates.Error("Player Interaction Array is null, Skipping...");
                }
            }

            if (_loadBalancingClient is {field_Private_Room_0: { }})
            {
                if (ClassicPlates.NameplateManager == null) return;
                foreach (var plate in ClassicPlates.NameplateManager.Nameplates.Values)
                {
                    if (plate == null) continue;
                    if (plate.player == null) continue;
                    var isInteractable = interactions.Contains(plate.player.prop_Int32_0);
                    ClassicPlates.Debug(
                        $"User: {plate.player.field_Private_APIUser_0.displayName} Interaction: {isInteractable}");
                    plate.Interactable = isInteractable;
                }
            }
            else
            {
                MelonCoroutines.Start(QueueInteractions(interactions));
            }
        }
        else
        {
            ClassicPlates.Error("Interaction Array is null, Skipping...");
        }
    }

    private static IEnumerator QueueInteractions(List<int> interactions)
    {
        while (_loadBalancingClient is {field_Private_Room_0: null})
        {
            yield return null;
        }
        
        if (ClassicPlates.NameplateManager == null) yield break;
        foreach (var plate in ClassicPlates.NameplateManager.Nameplates.Values)
        {
            if (plate == null) continue;
            if (plate.player == null) continue;
            var isInteractable = interactions.Contains(plate.player.prop_Int32_0);
            ClassicPlates.Debug($"User: {plate.player.field_Private_APIUser_0.displayName} Interaction: {isInteractable}");
            plate.Interactable = isInteractable;
        }
    }

    public static void ApplyModeration(string id)
    {
        if (!string.IsNullOrEmpty(id) && CachedModeration.TryGetValue(id, out var moderation))
        {
            ApplyModeration(moderation);
            CachedModeration.Remove(id);
        }
    }

    private static void ApplyModeration(Moderation moderation)
    {
        if (moderation != null)
        {
            if (moderation.Player != null)
            {
                var cachedPlayer = GetAPIUser(moderation.Player.Value);
                
                if (_loadBalancingClient is {field_Private_Room_0: { }} room)
                {
                    var photonPlayer =
                        room.field_Private_Room_0.Method_Public_Virtual_New_Player_Int32_Boolean_0(
                            moderation.Player.Value);

                    if (photonPlayer != null)
                    {
                        if (ClassicPlates.NameplateManager != null)
                        {
                            if (cachedPlayer != null)
                            {
                                if (ClassicPlates.NameplateManager.Nameplates.ContainsKey(cachedPlayer.id))
                                {
                                    var plate = ClassicPlates.NameplateManager.GetNameplate(cachedPlayer.id);

                                    ClassicPlates.Debug("Applying Moderation for Player: " + cachedPlayer.id);
                                    if (plate == null || moderation.Blocked == null || moderation.Muted == null)
                                        return;
                                    ClassicPlates.Debug("Block Status: " + moderation.Blocked.Value);
                                    ClassicPlates.Debug("Mute Status: " + moderation.Muted.Value);

                                    plate.IsBlocked = moderation.Blocked.Value;
                                    plate.IsMutedBy = moderation.Muted.Value;

                                }
                                else
                                {
                                    if (moderation.Blocked != null && moderation.Muted != null)
                                    {
                                        MelonCoroutines.Start(QueueModeration(cachedPlayer,
                                            moderation.Blocked.Value,
                                            moderation.Muted.Value));
                                    }
                                    else
                                    {
                                        ClassicPlates.Debug("Moderation is empty, skipping...");
                                    }
                                }
                            }
                            else
                            {
                                ClassicPlates.Error("Failed to get Photon Player for Moderation");
                            }
                        }
                        else
                        {
                            ClassicPlates.Error("Failed to get Nameplate Manager");
                        }
                    }
                    else
                    {
                        ClassicPlates.Error("Failed to get Player for Moderation");
                    }
                }
                else
                {
                    ClassicPlates.Debug("Room was null, caching moderation...");
                    var player = GetAPIUser(moderation.Player.Value);
                    if (player != null)
                    {
                        CachedModeration.Add(player.id, moderation);
                    }
                    else
                    {
                        ClassicPlates.Error("Unable to Cache Moderation, Photon Player was null");
                    }
                }
            }
            else
            {
                ClassicPlates.Error("Unable to Cache Moderation, Player was null");
            }
        }
        else
        {
            ClassicPlates.Error("Unable to Cache Moderation, Moderation was null");
        }
    }

    private static void ApplyCachedModeration()
    {
        if(ClassicPlates.NameplateManager == null) {return;}
        if (!(QueuedBlocks.Count > 0 | QueuedMutes.Count > 0))
        {
            ClassicPlates.Debug("No Queued Moderations");
            return;
        }

        foreach (var cachedPlayer in QueuedMutes.Select(GetAPIUser))
        {
            if (cachedPlayer != null)
            {
                if (ClassicPlates.NameplateManager.Nameplates.ContainsKey(cachedPlayer.id))
                {
                    var plate = ClassicPlates.NameplateManager.GetNameplate(cachedPlayer.id);
                    ClassicPlates.Debug("Applying Queued Mute for Player: " + cachedPlayer.id);

                    if (plate != null) plate.IsMutedBy = true;
                }
                else
                {
                    ClassicPlates.Debug("Nameplate not found, Queuing Mute...");
                    MelonCoroutines.Start(QueueModeration(cachedPlayer));
                }
            }
            else
            {
                ClassicPlates.Error("Failed to Apply Moderation, Player was Null");
            }
        }

        foreach (var cachedPlayer in QueuedBlocks.Select(GetAPIUser))
        {
            if (cachedPlayer != null)
            {
                if (ClassicPlates.NameplateManager.Nameplates.ContainsKey(cachedPlayer.id))
                {
                    var plate = ClassicPlates.NameplateManager.GetNameplate(cachedPlayer.id);
                    ClassicPlates.Debug("Applying Queued Block for Player: " + cachedPlayer.id);

                    if (plate != null)
                        plate.IsBlocked = true;
                }
                else
                {
                    ClassicPlates.Debug("Nameplate not found, Queuing Block...");
                    MelonCoroutines.Start(QueueModeration(cachedPlayer, true));
                }
            }
            else
            {
                ClassicPlates.Error("Failed to Apply Moderation, Player was Null");
            }
        }

        QueuedBlocks.Clear();
        QueuedMutes.Clear();
    }

    private static IEnumerator QueueModeration(APIUser cachedPlayer, bool blocked = false, bool muted = false)
    {
        if(ClassicPlates.NameplateManager == null) {yield break;}
        while (!ClassicPlates.NameplateManager.Nameplates.ContainsKey(cachedPlayer.id)) { yield return null; }
        var plate = ClassicPlates.NameplateManager.GetNameplate(cachedPlayer.id);
        
        if (muted)
        {
            ClassicPlates.Debug("Applying Queued Mute for Player: " + cachedPlayer.id);

            if (plate != null) plate.IsMutedBy = true;
        }
        
        if (blocked)
        {
            ClassicPlates.Debug("Applying Queued Block for Player: " + cachedPlayer.id);

            if (plate != null)
                plate.IsBlocked = true;
        }
    }

    private static APIUser? GetAPIUser(int playerID)
    {
        var player = _loadBalancingClient?.prop_Room_0.Method_Public_Virtual_New_Player_Int32_Boolean_0(playerID);
        if (player == null) return null;
        var managedHash = player.prop_Hashtable_0["user"]
            .TryCast<Il2CppSystem.Collections.Generic.Dictionary<string, Object>>();
        if (managedHash != null)
        {
            var hash = managedHash.GetHashCode();
            var apiUser = new APIUser();
            if (CachedPlayers.TryGetValue(hash.ToString(), out APIUser cachedApiUser))
            {
                apiUser = cachedApiUser;
            }
            else
            {
                foreach (var keyPair in managedHash)
                {
                    switch (keyPair.Key)
                    {
                        case "id":
                        {
                            apiUser.id = keyPair.Value.ToString();
                            break;
                        }
                        case "displayName":
                        {
                            apiUser.displayName = keyPair.Value.ToString();
                            break;
                        }
                        case "developerType":
                        {
                            apiUser.developerType =
                                System.Enum.TryParse(keyPair.Value.ToString(), out APIUser.DeveloperType developerType)
                                    ? developerType
                                    : APIUser.DeveloperType.None;
                            break;
                        }
                        case "profilePicOverride":
                        {
                            apiUser.profilePicOverride = keyPair.Value.ToString();
                            break;
                        }
                        case "currentAvatarImageUrl":
                        {
                            apiUser.currentAvatarImageUrl = keyPair.Value.ToString();
                            break;
                        }
                        case "currentAvatarThumbnailImageUrl":
                        {
                            apiUser.currentAvatarThumbnailImageUrl = keyPair.Value.ToString();
                            break;
                        }
                        case "userIcon":
                        {
                            apiUser.userIcon = keyPair.Value.ToString();
                            break;
                        }
                        case "last_platform":
                        {
                            apiUser.last_platform = keyPair.Value.ToString();
                            break;
                        }
                        case "allowAvatarCopying":
                        {
                            apiUser.allowAvatarCopying = keyPair.Value.Unbox<bool>();
                            break;
                        }
                        case "status":
                        {
                            apiUser.status = keyPair.Value.ToString();
                            break;
                        }
                        case "statusDescription":
                        {
                            apiUser.statusDescription = keyPair.Value.ToString();
                            break;
                        }
                        case "bio":
                        {
                            apiUser.bio = keyPair.Value.ToString();
                            break;
                        }
                        case "bioLinks":
                        { 
                            apiUser.bioLinks = keyPair.Value.TryCast<Il2CppSystem.Collections.Generic.List<string>>();
                            break;
                        }
                        case "tags":
                        {
                            apiUser.tags = keyPair.Value.TryCast<Il2CppSystem.Collections.Generic.List<string>>();
                            break;
                        }
                    }
                }
                CachedPlayers.Add(hash.ToString(), apiUser);
            }

            return apiUser;
        }
        ClassicPlates.Error("Player Hashtable is Null");
        return null;
    }
}