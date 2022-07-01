using System.Collections;
using ExitGames.Client.Photon;
using MelonLoader;
using Photon.Realtime;
using UnhollowerBaseLib;
using Array = Il2CppSystem.Array;
using Object = Il2CppSystem.Object;

namespace ClassicPlates.Patching;

public static class PhotonUtils
{
    private static readonly Dictionary<string, Moderation> CachedModeration = new();
    private static readonly Dictionary<string, PhotonPlayer?> CachedPlayers = new();
    private static LoadBalancingClient? _loadBalancingClient;
    private static readonly List<int> QueuedBlocks = new();
    private static readonly List<int> QueuedMutes = new();

    internal static void HandleModerationEvent(LoadBalancingClient loadBalancingClient, EventData eventData)
    {
        _loadBalancingClient ??= loadBalancingClient;

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
    
    public static void HandleInteractionEvent(LoadBalancingClient loadBalancingClient, EventData eventData)
    {
        _loadBalancingClient ??= loadBalancingClient;
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
                var cachedPlayer = GetPhotonPlayer(moderation.Player.Value);
                
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
                                if (ClassicPlates.NameplateManager.Nameplates.ContainsKey(cachedPlayer.ID))
                                {
                                    var plate = ClassicPlates.NameplateManager.GetNameplate(cachedPlayer.ID);

                                    ClassicPlates.Debug("Applying Moderation for Player: " + cachedPlayer.ID);
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
                    var player = GetPhotonPlayer(moderation.Player.Value);
                    if (player != null)
                    {
                        CachedModeration.Add(player.ID, moderation);
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

        foreach (var cachedPlayer in QueuedMutes.Select(GetPhotonPlayer))
        {
            if (cachedPlayer != null)
            {
                if (ClassicPlates.NameplateManager.Nameplates.ContainsKey(cachedPlayer.ID))
                {
                    var plate = ClassicPlates.NameplateManager.GetNameplate(cachedPlayer.ID);
                    ClassicPlates.Debug("Applying Queued Mute for Player: " + cachedPlayer.ID);

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

        foreach (var cachedPlayer in QueuedBlocks.Select(GetPhotonPlayer))
        {
            if (cachedPlayer != null)
            {
                if (ClassicPlates.NameplateManager.Nameplates.ContainsKey(cachedPlayer.ID))
                {
                    var plate = ClassicPlates.NameplateManager.GetNameplate(cachedPlayer.ID);
                    ClassicPlates.Debug("Applying Queued Block for Player: " + cachedPlayer.ID);

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

    private static IEnumerator QueueModeration(PhotonPlayer cachedPlayer, bool blocked = false, bool muted = false)
    {
        if(ClassicPlates.NameplateManager == null) {yield break;}
        while (!ClassicPlates.NameplateManager.Nameplates.ContainsKey(cachedPlayer.ID)) { yield return null; }
        var plate = ClassicPlates.NameplateManager.GetNameplate(cachedPlayer.ID);
        
        if (muted)
        {
            ClassicPlates.Debug("Applying Queued Mute for Player: " + cachedPlayer.ID);

            if (plate != null) plate.IsMutedBy = true;
        }
        
        if (blocked)
        {
            ClassicPlates.Debug("Applying Queued Block for Player: " + cachedPlayer.ID);

            if (plate != null)
                plate.IsBlocked = true;
        }
    }

    private static PhotonPlayer? GetPhotonPlayer(int playerID)
    {
        var player = _loadBalancingClient?.prop_Room_0.Method_Public_Virtual_New_Player_Int32_Boolean_0(playerID);
        if (player == null) return null;
        var managedHash = player.prop_Hashtable_0["user"]
            .TryCast<Il2CppSystem.Collections.Generic.Dictionary<string, Object>>();
        if (managedHash != null)
        {
            var hash = managedHash.GetHashCode();
            var photonPlayer = new PhotonPlayer();
            if (CachedPlayers.TryGetValue(hash.ToString(), out var cachedPlayer))
            {
                photonPlayer = cachedPlayer;
            }
            else
            {
                foreach (var keyPair in managedHash)
                {
                    switch (keyPair.Key)
                    {
                        case "id":
                        {
                            photonPlayer.ID = keyPair.Value.ToString();
                            break;
                        }
                        case "displayName":
                        {
                            photonPlayer.DisplayName = keyPair.Value.ToString();
                            break;
                        }
                        case "developerType":
                        {
                            photonPlayer.DeveloperType = keyPair.Value.ToString();
                            break;
                        }
                        case "profilePicOverride":
                        {
                            photonPlayer.ProfilePicOverride = keyPair.Value.ToString();
                            break;
                        }
                        case "currentAvatarImageUrl":
                        {
                            photonPlayer.CurrentAvatarImageUrl = keyPair.Value.ToString();
                            break;
                        }
                        case "currentAvatarThumbnailImageUrl":
                        {
                            photonPlayer.CurrentAvatarThumbnailImageUrl = keyPair.Value.ToString();
                            break;
                        }
                        case "userIcon":
                        {
                            photonPlayer.UserIcon = keyPair.Value.ToString();
                            break;
                        }
                        case "last_platform":
                        {
                            photonPlayer.LastPlatform = keyPair.Value.ToString();
                            break;
                        }
                        case "allowAvatarCopying":
                        {
                            photonPlayer.AllowAvatarCopying = keyPair.Value.Unbox<bool>();
                            break;
                        }
                        case "status":
                        {
                            photonPlayer.Status = keyPair.Value.ToString();
                            break;
                        }
                        case "statusDescription":
                        {
                            photonPlayer.StatusDescription = keyPair.Value.ToString();
                            break;
                        }
                        case "bio":
                        {
                            photonPlayer.Bio = keyPair.Value.ToString();
                            break;
                        }
                        case "bioLinks":
                        {
                            photonPlayer.BioLinks = Il2CppArrayBase<Object>.WrapNativeGenericArrayPointer(keyPair.Value.Pointer);
                            break;
                        }
                        case "tags":
                        {
                            photonPlayer.Tags = Il2CppArrayBase<Object>.WrapNativeGenericArrayPointer(keyPair.Value.Pointer);
                            break;
                        }
                    }
                }

                CachedPlayers.Add(hash.ToString(), photonPlayer);
            }

            return photonPlayer;
        }
        ClassicPlates.Error("Player Hashtable is Null");
        return null;
    }
}

public class Moderation
{
    public int? Player;
    public bool? Blocked;
    public bool? Muted;
}

public class PhotonPlayer
{
#pragma warning disable CS8618
    public string ID;
    public string DisplayName;
    public string DeveloperType;
    public string ProfilePicOverride;
    public string CurrentAvatarImageUrl;
    public string CurrentAvatarThumbnailImageUrl;
    public string UserIcon;
    public string LastPlatform;
    public bool AllowAvatarCopying;
    public string Status;
    public string StatusDescription;
    public string Bio;
    public object[] BioLinks;
    public object[] Tags;
#pragma warning restore CS8618
}