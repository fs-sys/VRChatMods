using ClassicPlates.MonoScripts;
using System.Collections;
using ClassicPlates.Patching;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using VRC.Management;
using VRC.SDKBase.Validation.Performance;

namespace ClassicPlates;

public class NameplateManager
{
    public readonly Dictionary<string, OldNameplate?> Nameplates;
    private static Dictionary<string, Texture2D>? _cachedImage;
    private static EnableDisableListener? _enableDisableListener;
    private string _masterClient = "";

    public NameplateManager()
    {
        Nameplates = new Dictionary<string, OldNameplate?>();
        _cachedImage = new Dictionary<string, Texture2D>();
        _enableDisableListener = Resources.FindObjectsOfTypeAll<VRC.UI.Elements.QuickMenu>()[0].gameObject.AddComponent<EnableDisableListener>();
    }

    public string MasterClient
    {
        get => this._masterClient;
        set
        {
            if (MasterClient != "")
            {
                var plate = GetNameplate(MasterClient);
                if (plate != null)
                {
                    plate.IsMaster = false;
                }
            }

            var newMaster = GetNameplate(value);
            if (newMaster == null) return;
            newMaster.IsMaster = true;
            _masterClient = value;
        }
    }

    private void AddNameplate(OldNameplate nameplate, VRCPlayer player)
    {
        string id;
        try
        {
            id = player._player.field_Private_APIUser_0.id;
        }
        catch
        {
            return;
        }
        
        if (id != null && nameplate != null)
            Nameplates.Add(id, nameplate);
    }

    public void RemoveNameplate(VRCPlayer player)
    {
        string id;
        try
        {
            id = player._player.field_Private_APIUser_0.id;
        }
        catch
        {
            return;
        }

        Nameplates.Remove(id);
    }

    public OldNameplate? GetNameplate(VRCPlayer player)
    {
        if (Nameplates.TryGetValue(player._player.field_Private_APIUser_0.id, out OldNameplate? nameplate))
        {
            return nameplate;
        }

        ClassicPlates.DebugError($"Nameplate does not exist in Dictionary for player: {player._player.prop_APIUser_0.displayName}");
        return null;
    }

    public OldNameplate? GetNameplate(string id)
    {
        if (Nameplates.TryGetValue(id, out OldNameplate? nameplate))
        {
            return nameplate;
        }

        ClassicPlates.DebugError($"Nameplate does not exist in Dictionary for player: {id}");
        return null;
    }

    public void ClearNameplates()
    {
        Nameplates.Clear();
    }

    //Nameplates can support 5 compatibility badges total
    public GameObject? AddBadge(OldNameplate plate, string id, Texture2D? icon)
    {
        if (plate.badgeCompat == null) return null;
        var gameObject = plate.badgeCompat.gameObject;
        var badge = UnityEngine.Object.Instantiate(gameObject, gameObject.transform.parent);
        badge.name = id;
        badge.transform.localPosition = gameObject.transform.localPosition;
        badge.transform.localRotation = gameObject.transform.localRotation;
        badge.transform.localScale = gameObject.transform.localScale;
        badge.SetActive(true);
        var image = badge.GetComponent<Image>();
        if (icon != null)
        {
            image.sprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height),
                new Vector2(0.5f, 0.5f));
        }
        else
        {
            image.enabled = false;
        }
        return badge;
    }

    public static void InitializePlate(OldNameplate oldNameplate, Player? player)
    {
        try
        {
            if (player != null)
            {
                oldNameplate.player = player;

                if (oldNameplate.player != null)
                {
                    oldNameplate.Name = player.field_Private_APIUser_0.displayName;

                    oldNameplate.Status = player.field_Private_APIUser_0.statusDescriptionDisplayString;
                    
                    oldNameplate.Rank =
                        VRCPlayer.Method_Public_Static_String_APIUser_0(player.field_Private_APIUser_0);

                    // Literally broken no matter what I try.
                    //oldNameplate.ShowSocialRank = player.field_Private_APIUser_0.showSocialRank;

                    oldNameplate.IsFriend = player.field_Private_APIUser_0.isFriend;

                    oldNameplate.IsMaster = player.field_Private_VRCPlayerApi_0.isMaster;

                    ClassicPlates.NameplateManager!._masterClient = player.field_Private_APIUser_0.id;

                    //Getting if this value has changed.
                    //uSpeaker.NativeMethodInfoPtr_Method_Public_Single_1
                    //Have fun future me, it's your favorite thing, native patching :D
                    oldNameplate.UserVolume = player.prop_USpeaker_0.field_Private_Single_1;

                    oldNameplate.ProfilePicture = player.field_Private_APIUser_0.userIcon;

                    oldNameplate.IsQuest = player.field_Private_APIUser_0._last_platform.ToLower() == "android";

                    oldNameplate.IsVip = player.field_Private_VRCPlayerApi_0.isModerator |
                                         player.field_Private_VRCPlayerApi_0.isSuper;

                    oldNameplate.IsLocal = player.field_Private_VRCPlayerApi_0.isLocal;

                    if (_enableDisableListener != null)
                    {
                        _enableDisableListener.OnEnableEvent += oldNameplate.OnQMEnable;
                        _enableDisableListener.OnDisableEvent += oldNameplate.OnQMDisable;
                    }
                    else
                    {
                        ClassicPlates.Error("EnableDisableListener is null");
                    }

                    oldNameplate.AvatarKind =
                        player._vrcplayer.field_Private_VRCAvatarManager_0.field_Private_AvatarKind_0;

                    oldNameplate.Performance =
                        oldNameplate.player._vrcplayer.field_Private_VRCAvatarManager_0
                            .field_Private_AvatarPerformanceStats_0
                            .GetPerformanceRatingForCategory(AvatarPerformanceCategory.Overall);

                    if (ModerationManager.field_Private_Static_ModerationManager_0
                        .field_Private_Dictionary_2_String_List_1_ApiPlayerModeration_0
                        .ContainsKey(player.field_Private_APIUser_0.id))
                    {
                        var moderationList =
                            ModerationManager.field_Private_Static_ModerationManager_0
                                .field_Private_Dictionary_2_String_List_1_ApiPlayerModeration_0[
                                    player.field_Private_APIUser_0.id];
                        var moderation = new List<ApiPlayerModeration.ModerationType>();
                        foreach (var m in moderationList)
                        {
                            moderation.Add(m.moderationType);
                        }

                        oldNameplate.IsMuted = !moderation.Contains(ApiPlayerModeration.ModerationType.Unmute) &&
                                               moderation.Contains(ApiPlayerModeration.ModerationType.Mute);
                        oldNameplate.IsBlocked = moderation.Contains(ApiPlayerModeration.ModerationType.Block);
                    }
                    else
                    {
                        ClassicPlates.Debug("No Moderations for: " + player.field_Private_APIUser_0.id);
                        oldNameplate.IsMuted = false;
                        oldNameplate.IsBlocked = false;
                    }
                }
                else
                {
                    oldNameplate.Name = "||Error||";
                    oldNameplate.Status = "||Failed to load||";
                }
            }

            else
            {
                oldNameplate.Name = "||Error||";
                oldNameplate.Status = "||Failed to load||";
                ClassicPlates.Error("Unable to Initialize Nameplate: Player is null");
            }
        }
        catch (Exception e)
        {
            oldNameplate.Name = "||Error||";
            oldNameplate.Status = "||Failed to load||";
            ClassicPlates.Error("Unable to Initialize Nameplate: " + e);
        }
    }

    internal static IEnumerator SetRawImage(string url, RawImage image)
    {
        if (_cachedImage != null)
        {
            if (_cachedImage.TryGetValue(url, out var tex))
            {
                ClassicPlates.Debug("Found Cached Image for: " + url);
            }
            else
            {
                //Dis Lily
                var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:87.0) Gecko/20100101 Firefox/87.0");

                var req = http.GetByteArrayAsync(url);
                while (!req.GetAwaiter().IsCompleted)
                {
                    yield return null;
                }

                if (!req.IsCanceled & !req.IsFaulted)
                {
                    var bytes = req.Result;
                    try
                    {
                        //I do Dis
                        ClassicPlates.Debug($"Download Finished: {url}");
                        tex = new Texture2D(2, 2)
                        {
                            hideFlags = HideFlags.DontUnloadUnusedAsset,
                            wrapMode = TextureWrapMode.Clamp,
                            filterMode = FilterMode.Trilinear
                        };

                        // ReSharper disable once InvokeAsExtensionMethod
                        // Compiles incorrectly if called as an extension. Why? Who knows
                        if (ImageConversion.LoadImage(tex, bytes))
                        {
                            ClassicPlates.Debug("Loading Using LoadImage...");
                        }
                        else
                        {
                            ClassicPlates.Debug("Loading using LoadRawTextureData...");
                            tex.LoadRawTextureData(bytes);
                        }

                        _cachedImage.Add(url, tex);
                    }
                    catch (Exception e)
                    {
                        ClassicPlates.Error(e.ToString());
                    }
                }
                else
                {
                    ClassicPlates.Error("Image Request Failed");
                }

                http.Dispose();
            }

            if (tex != null && tex.isReadable)
            {
                image.texture = tex;
                if (Settings.ShowIcon != null) image.transform.parent.gameObject.active = Settings.ShowIcon.Value;
                ClassicPlates.Debug("Applying Image");
            }
            else
            {
                ClassicPlates.Error("Texture is Unreadable: " + url);
                _cachedImage.Remove(url);
                image.transform.parent.gameObject.active = false;
            }
        }
        else
        {
            ClassicPlates.Error("Image Cache is Null");
        }
    }

    public void CreateNameplate(VRCPlayer vrcPlayer)
    {
        var oldNameplate = vrcPlayer.field_Public_PlayerNameplate_0.gameObject.transform.parent.parent;
        var position = oldNameplate.GetComponentInParent<NameplatePositioner>().Method_Private_Vector3_PDM_0();

        if (Settings.Offset != null && Settings.Scale != null && Settings.Enabled != null)
        {
            var scaleValue = Settings.Scale.Value * .001f;
            var offsetValue = Settings.Offset.Value;

            // Hopefully fixes ID null issues
            var player = vrcPlayer._player;
            if (player != null)
            {
                var apiUser = player.field_Private_APIUser_0;
                if (apiUser == null)
                {
                    ClassicPlates.DebugError("APIUser is null");
                    return;
                }

                var id = apiUser.id;
                if (id is {Length: > 0})
                {
                    if (Nameplates.TryGetValue(id, out var nameplate))
                    {
                        if (nameplate != null)
                        {
                            nameplate.ApplySettings(position, scaleValue, offsetValue);
                        }
                        else
                        {
                            ClassicPlates.Error("Unable to Update Nameplate: Nameplate is Null");
                        }
                    }
                    else
                    {
                        var plate = UnityEngine.Object.Instantiate(AssetManager.Nameplate,
                            new(position.x, position.y + offsetValue, position.z),
                            new(0, 0, 0, 0), oldNameplate.parent);

                        if (plate != null)
                        {
                            plate.transform.localScale = new(scaleValue, scaleValue, scaleValue);
                            plate.name = "OldNameplate";
                            nameplate = plate.AddComponent<OldNameplate>();
                            AddNameplate(nameplate, vrcPlayer);
                        }
                        else
                        {
                            ClassicPlates.Error("Unable to Instantiate Nameplate: Nameplate is Null");
                        }
                    }

                    if (Settings.Enabled.Value)
                    {
                        oldNameplate.GetChild(0).gameObject.active = false;
                        if (nameplate != null && Settings.ShowOthersOnMenu != null && !nameplate.IsLocal &&
                            nameplate.Nameplate != null)
                            nameplate.Nameplate.active = Settings.Enabled.Value & (
                                Settings.NameplateMode == VRC.NameplateManager.NameplateMode.Standard |
                                Settings.NameplateMode == VRC.NameplateManager.NameplateMode.Icons |
                                (Settings.ShowOthersOnMenu.Value &&
                                 Settings.NameplateMode == VRC.NameplateManager.NameplateMode.Hidden &&
                                 nameplate.qmOpen));
                    }
                    else
                    {
                        oldNameplate.GetChild(0).gameObject.active = true;
                        if (nameplate != null && nameplate.Nameplate != null)
                            nameplate.Nameplate.active = false;
                    }
                }
                else
                {
                    ClassicPlates.Error("Unable to Instantiate Nameplate: Player is Null");
                }
            }
            else
            {
                ClassicPlates.Error("Unable to Instantiate Nameplate: Player is Null");
            }
        }
        else
        {
            ClassicPlates.Error("Unable to Initialize Nameplate: Settings are null");
        }
    }
}