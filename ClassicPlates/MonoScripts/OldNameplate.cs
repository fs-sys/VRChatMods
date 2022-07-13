using System.Collections;
using System.Diagnostics.CodeAnalysis;
using MelonLoader;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;
using VRC;
using VRC.SDKBase.Validation.Performance;
using static ClassicPlates.AssetManager;
using static VRCAvatarManager;

namespace ClassicPlates.MonoScripts;

[RegisterTypeInIl2Cpp]
public class OldNameplate : MonoBehaviour
{
    public OldNameplate(IntPtr ptr) : base(ptr)
    {
    }

    public Player? player;
    
    public bool qmOpen;
    
    private bool _isFriend;
    private bool _isMaster;
    private bool _isMuted;
    private bool _isMutedBy;
    private bool _isBlocked;
    private bool _isQuest;
    private bool _isAfk;
    private bool _isVip;
    
    private bool _interactionStatus;
    private float _userVolume;
    private Color _plateColor;
    private Color _nameColor;
    private string? _name;
    private string? _rank;
    // private bool _showSocialRank;
    private string? _status;
    private string? _profilePicture;
    private string? _plateBackground;
    private PerformanceRating _performance;
    private AvatarKind _avatarKind;

    internal GameObject? Nameplate;
    private Transform? _transform;
    private PositionConstraint? _constraint;
    private Camera? _camera;

    private Image? _mainPlate;
    private Text? _mainText;
    private Text? _mainStatus;
    private RawImage? _mainBackground;

    private Image? _afkPlate;
    private Text? _afkText;
    private RawImage? _afkBackground;

    private Image? _userPlate;
    private RawImage? _userIcon;

    private Image? _vipPlate;
    private Text? _vipText;
    private RawImage? _vipBackground;

    private Image? _voiceBubble;
    private Image? _voiceStatus;
    private Text? _voiceVolume;

    private Image? _badgeMaster;
    private Image? _badgeFallback;
    private Image? _badgePerformance;
    private Image? _badgeQuest;
    public Image? badgeCompat;

    private Image? _iconFriend;
    private Image? _iconInteract;

    private Text? _rankText;


    public bool IsLocal { get; set; }

    public bool IsFriend
    {
        get => _isFriend;
        set
        {
            _isFriend = value;
            if (_iconFriend != null) _iconFriend.gameObject.active = _isFriend;
            if (_isFriend)
            {
                if (_rankText == null) return;
                var text = _rankText.text;
                if (text.Contains("Friend")) return;
                text = $"Friend ({text})";
                _rankText.text = text;
            }
            else
            {
                if (_rankText == null) return;
                var text = _rankText.text;
                if (!text.StartsWith("Friend (")) return;
                text = text.Remove(0, 8).Replace(')', ' ').Trim();
                _rankText.text = text;
            }
        }
    }

    public bool IsMaster
    {
        get => _isMaster;
        set
        {
            _isMaster = value;
            if (_badgeMaster == null) return;
            if (Settings.ShowMaster != null)
                _badgeMaster.gameObject.active = _isMaster && Settings.ShowMaster.Value;
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            _isMuted = value;

            if (_voiceBubble == null || _voiceBubble.gameObject == null) return;

            var component = _voiceBubble.gameObject.GetComponent<SpriteSwapAnimation>();
            if (component == null)
            {
                component = _voiceBubble.gameObject.AddComponent<SpriteSwapAnimation>();
                component.field_Public_Image_0 = _voiceBubble;
                component.field_Public_ArrayOf_Sprite_0 = _isMuted ? MutedSprites : SpeakingSprites;
            }

            if (component == null) return;
            component.field_Public_ArrayOf_Sprite_0 = _isMuted ? MutedSprites : SpeakingSprites;

            _voiceBubble.gameObject.SetActive(_isMuted);
        }
    }


    public bool IsMutedBy
    {
        get => _isMutedBy;
        set
        {
            _isMutedBy = value;
            if (SpriteDict == null || _voiceStatus == null) return;
            if (_isMutedBy)
            {
                _voiceStatus.sprite = SpriteDict["earmute"];
                _voiceStatus.gameObject.active = true;

            }
            else
            {
                _voiceStatus.sprite = SpriteDict["ear"];

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (_userVolume == 1f) _voiceStatus.gameObject.active = false;
            }
        }
    }

    public bool IsBlocked
    {
        get => _isBlocked;
        set
        {
            _isBlocked = value;
            if (Nameplate != null && Settings.Enabled != null)
                    Nameplate.active = Settings.Enabled.Value && !_isBlocked && Settings.NameplateMode != VRC.NameplateManager.NameplateMode.Hidden;
        }
    }

    public float UserVolume
    {
        get => _userVolume;
        set
        {
            _userVolume = value;

            if (_voiceVolume == null || _voiceStatus == null) return;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_userVolume != 1f)
            {
                _voiceVolume.text = $"{(int) (_userVolume * 100)}%";
                _voiceVolume.gameObject.active = true;
                _voiceStatus.gameObject.active = true;
            }
            else
            {
                _voiceVolume.text = "100%";
                _voiceVolume.gameObject.active = false;

                if (!_isMutedBy)
                {
                    _voiceStatus.gameObject.SetActive(false);
                }
            }
        }
    }

    public bool IsQuest
    {
        get => _isQuest;
        set
        {
            _isQuest = value;
            if (_badgeQuest == null) return;
            if (Settings.ShowQuest != null)
                _badgeQuest.gameObject.active = _isQuest && Settings.ShowQuest.Value;
        }
    }

    public bool IsAfk
    {
        get => _isAfk;
        set
        {
            _isAfk = value;
            if (_afkPlate != null) _afkPlate.transform.parent.gameObject.active = _isAfk;
        }
    }
    
    public bool IsVip
    {
        get => _isVip;
        set
        {
            _isVip = value;
            if (_vipPlate != null) _vipPlate.transform.parent.gameObject.active = _isVip;
        }
    }

    public bool Interactable
    {
        get => _interactionStatus;
        set
        {
            _interactionStatus = value;
            if (SpriteDict == null || _iconInteract == null) return;

            _iconInteract.sprite = _interactionStatus
                ? SpriteDict["physyes"]
                : SpriteDict["physno"];

            if (Settings.ShowInteraction == null) return;
            if (Settings.ShowInteractionOnMenu != null)
                _iconInteract.gameObject.active = Settings.ShowInteraction.Value |
                                                  (Settings.ShowInteractionOnMenu.Value && qmOpen);
        }
    }

    public Color PlateColor
    {
        get => _plateColor;
        set
        {
            _plateColor = value;

            if (_mainPlate != null) _mainPlate.color = _plateColor;
            if (_vipPlate != null) _vipPlate.color = _plateColor;
            if (_afkPlate != null) _afkPlate.color = _plateColor;
            if (_userPlate != null) _userPlate.color = _plateColor;
        }
    }

    public Color NameColor
    {
        get => _nameColor;
        set
        {
            _nameColor = value;

            if (_mainText != null) _mainText.color = _nameColor;

            // Didn't really like how this looked, so I'm disabling it
            // if (_mainStatus != null) _mainStatus.color = _nameColor;
            // if (_rankText != null) _rankText.color = _nameColor;
            // if (_vipText != null) _vipText.color = _nameColor;
            // if (_afkText != null) _afkText.color = _nameColor;
        }
    }

    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            if (_mainText == null) return;
            var displayName = _name;
            if (displayName is {Length: > 16})
            {
                displayName = displayName.Remove(15) + "...";
            }

            _mainText.text = displayName;
            _mainText.gameObject.active = true;
        }
    }

    private bool ShowSocialRank => true; //_showSocialRank;
    /*set
        {
            if (_showSocialRank == value) return;
            _showSocialRank = value;
            if (_showSocialRank)
            {
                if (player != null)
                {
                    var userRank = VRCPlayer.Method_Public_Static_String_APIUser_0(player.field_Private_APIUser_0);
                    var userColor = VRCPlayer.Method_Public_Static_Color_APIUser_0(player.field_Private_APIUser_0);
                    if (userRank != null) Rank = userRank;

                    if (Settings.PlateColorByRank is {Value: true})
                        PlateColor = userColor;
                    if (Settings.NameColorByRank is {Value: true})
                        NameColor = userColor;
                }
            }
            else
            {
                Rank = "User";
                if (Settings.PlateColorByRank is {Value: true})
                    PlateColor = VRCPlayer.field_Internal_Static_Color_4;
                if (Settings.NameColorByRank is {Value: true})
                    NameColor = VRCPlayer.field_Internal_Static_Color_4;
            }
            IsFriend = _isFriend;
        }*/
    
    public string? Rank
    {
        get => _rank;
        set
        {
            if (_rankText == null) return;
            if (ShowSocialRank)
            {
                _rank = value;
                _rankText.text = _rank;
            }
            else
            {
                _rank = "User";
                _rankText.text = "User";
            }
            IsFriend = _isFriend;
            if (Settings.ShowRank != null) _rankText.gameObject.active = Settings.ShowRank.Value;
        }
    }

    public string? Status
    {
        get => _status;
        set
        {
            _status = value;
            if (_mainStatus == null) return;
            _mainStatus.text = _status;
            _mainStatus.gameObject.active = Settings.StatusMode == VRC.NameplateManager.StatusMode.AlwaysOn |
                                            (Settings.StatusMode == VRC.NameplateManager.StatusMode.ShowOnMenu &&
                                             qmOpen);
        }
    }

    public string? ProfilePicture
    {
        get => _profilePicture;
        set
        {
            _profilePicture = value;
            if (string.IsNullOrEmpty(_profilePicture)) return;
            if (_profilePicture != null && _userIcon != null)
                MelonCoroutines.Start(NameplateManager.SetRawImage(_profilePicture, _userIcon));
        }
    }

    [SuppressMessage("ReSharper", "IteratorMethodResultIsIgnored")]
    public string? PlateBackground
    {
        get => _plateBackground;
        set
        {
            _plateBackground = value;

            if (_plateBackground == null)
                return;
            if (_mainBackground != null)
                MelonCoroutines.Start(NameplateManager.SetRawImage(_plateBackground, _mainBackground));
            if (_afkBackground != null)
                MelonCoroutines.Start(NameplateManager.SetRawImage(_plateBackground, _afkBackground));
            if (_vipBackground != null)
                MelonCoroutines.Start(NameplateManager.SetRawImage(_plateBackground, _vipBackground));
        }
    }

    public PerformanceRating Performance
    {
        get => _performance;

        set
        {
            _performance = value;

            if (SpriteDict == null || _badgePerformance == null || _badgeFallback == null ||
                Settings.ShowPerformance == null) return;
            switch (_performance)
            {
                case PerformanceRating.None:
                {
                    _badgePerformance.sprite = SpriteDict["blocked"];
                    _badgeFallback.sprite = SpriteDict["fallback"];
                    return;
                }
                case PerformanceRating.Excellent:
                {
                    _badgePerformance.sprite = SpriteDict["great"];
                    _badgeFallback.sprite = SpriteDict["fallbackgreat"];
                    _badgePerformance.gameObject.active = Settings.ShowPerformance.Value;
                    break;
                }
                case PerformanceRating.Good:
                {
                    _badgePerformance.sprite = SpriteDict["good"];
                    _badgeFallback.sprite = SpriteDict["fallbackgood"];
                    _badgePerformance.gameObject.active = Settings.ShowPerformance.Value;
                    break;
                }
                case PerformanceRating.Medium:
                {
                    _badgePerformance.sprite = SpriteDict["medium"];
                    _badgeFallback.sprite = SpriteDict["fallbackmedium"];
                    _badgePerformance.gameObject.active = Settings.ShowPerformance.Value;
                    break;
                }
                case PerformanceRating.Poor:
                {
                    _badgePerformance.sprite = SpriteDict["poor"];
                    _badgeFallback.sprite = SpriteDict["fallbackpoor"];
                    _badgePerformance.gameObject.active = Settings.ShowPerformance.Value;
                    break;
                }
                case PerformanceRating.VeryPoor:
                {
                    _badgePerformance.sprite = SpriteDict["horrible"];
                    _badgeFallback.sprite = SpriteDict["fallbackhorrible"];
                    _badgePerformance.gameObject.active = Settings.ShowPerformance.Value;
                    break;
                }
                default:
                {
                    ClassicPlates.Error("Unknown performance rating: " + _performance);
                    break;
                }
            }
        }
    }

    public AvatarKind AvatarKind
    {
        get => _avatarKind;
        set
        {
            _avatarKind = value;
            if (SpriteDict == null || _badgeFallback == null || _badgePerformance == null ||
                Settings.ShowFallback == null || Settings.ShowPerformance == null) return;
            if (player != null) ClassicPlates.Debug($"{player.prop_APIUser_0.displayName}'s Avatar kind: " + _avatarKind);
            switch (_avatarKind)
            {
                case AvatarKind.Undefined:
                    _badgeFallback.gameObject.active = false;
                    _badgePerformance.gameObject.active = false;
                    break;

                case AvatarKind.Loading:
                    _badgeFallback.gameObject.active = false;
                    _badgePerformance.gameObject.active = false;
                    break;

                case AvatarKind.Error:
                    _badgeFallback.sprite = SpriteDict["fallbackerror"];
                    _badgeFallback.gameObject.active = Settings.ShowFallback.Value;
                    _badgePerformance.gameObject.active = false;
                    break;

                case AvatarKind.Blocked:
                    _badgeFallback.gameObject.active = false;
                    _badgePerformance.gameObject.active = false;
                    break;

                case AvatarKind.Safety:
                    _badgePerformance.sprite = SpriteDict["blocked"];
                    _badgePerformance.gameObject.active = Settings.ShowPerformance.Value;
                    _badgeFallback.gameObject.active = false;
                    break;

                case AvatarKind.Performance:
                    _badgePerformance.gameObject.active = false;
                    _badgeFallback.gameObject.active = Settings.ShowFallback.Value;
                    break;

                case AvatarKind.Substitute:
                    _badgeFallback.gameObject.active = false;
                    _badgePerformance.gameObject.active = false;
                    break;

                case AvatarKind.Fallback:
                    _badgePerformance.gameObject.active = false;
                    _badgeFallback.gameObject.active = Settings.ShowFallback.Value;
                    break;

                case AvatarKind.Custom:
                    _badgeFallback.gameObject.active = false;
                    _badgePerformance.gameObject.active = Settings.ShowPerformance.Value;
                    break;

                case AvatarKind.Impostor:
                    _badgePerformance.sprite = SpriteDict["imposter"];
                    _badgePerformance.gameObject.active = Settings.ShowPerformance.Value;
                    _badgeFallback.gameObject.active = false;
                    break;

                default:
                {
                    ClassicPlates.Error("Unknown avatar kind: " + _avatarKind);
                    break;
                }
            }
        }
    }

    public void Awake()
    {
        if (Camera.main != null) _camera = Camera.main;

        Nameplate = gameObject;
        _transform = Nameplate.transform;
        _constraint = Nameplate.AddComponent<PositionConstraint>();
        _constraint.constraintActive = false;

        _mainPlate = Nameplate.transform.Find("Main/Plate").GetComponent<Image>();
        _mainText = Nameplate.transform.Find("Main/Name").GetComponent<Text>();
        _mainStatus = Nameplate.transform.Find("Main/Status").GetComponent<Text>();
        _mainBackground = Nameplate.transform.Find("Main/Mask/Background").GetComponent<RawImage>();

        _afkPlate = Nameplate.transform.Find("AFK/Plate").GetComponent<Image>();
        _afkText = Nameplate.transform.Find("AFK/Text").GetComponent<Text>();
        _afkBackground = Nameplate.transform.Find("AFK/Mask/Background").GetComponent<RawImage>();

        _userPlate = Nameplate.transform.Find("VIP/Icon").GetComponent<Image>();
        _userIcon = Nameplate.transform.Find("VIP/Icon/Image").GetComponent<RawImage>();

        _vipPlate = Nameplate.transform.Find("VIP/Plate/Plate").GetComponent<Image>();
        _vipText = Nameplate.transform.Find("VIP/Plate/Text").GetComponent<Text>();
        _vipBackground = Nameplate.transform.Find("VIP/Plate/Text").GetComponent<RawImage>();

        _voiceBubble = Nameplate.transform.Find("Voice/Bubble").GetComponent<Image>();

        _voiceStatus = Nameplate.transform.Find("Voice/Status").GetComponent<Image>();
        _voiceVolume = Nameplate.transform.Find("Voice/Volume").GetComponent<Text>();

        _badgeMaster = Nameplate.transform.Find("Badges/Master").GetComponent<Image>();
        _badgeFallback = Nameplate.transform.Find("Badges/Fallback").GetComponent<Image>();
        _badgePerformance = Nameplate.transform.Find("Badges/Performance").GetComponent<Image>();
        _badgeQuest = Nameplate.transform.Find("Badges/Quest").GetComponent<Image>();
        badgeCompat = Nameplate.transform.Find("Badges/Compat").GetComponent<Image>();

        _iconFriend = Nameplate.transform.Find("Icons/Friend").GetComponent<Image>();
        _iconInteract = Nameplate.transform.Find("Icons/Interact").GetComponent<Image>();

        _rankText = Nameplate.transform.Find("Rank").GetComponent<Text>();

        if (ClassicPlates.NameplateManager != null)
            NameplateManager.InitializePlate(this,
                Nameplate.GetComponentInParent<Player>());

        MelonCoroutines.Start(SpeechManagement());
        MelonCoroutines.Start(PlateManagement());

        ApplySettings();
    }

    [HideFromIl2Cpp]
    private IEnumerator SpeechManagement()
    {
        while (true)
        {
            if (Nameplate != null && Nameplate.active && player != null && SpriteDict != null)
            {
                if (player._USpeaker.field_Private_Boolean_0)
                {
                    if (_voiceBubble != null && !_voiceBubble.gameObject.active)
                    {
                        if (Settings.ShowVoiceBubble != null)
                        {
                            _voiceBubble.gameObject.active = Settings.ShowVoiceBubble.Value;
                        }

                        if (_mainPlate != null && _mainPlate.gameObject.active)
                        {
                            _mainPlate.sprite = SpriteDict["nameplatetalk"];
                        }

                        if (_afkPlate != null && _afkPlate.gameObject.active)
                        {
                            _afkPlate.sprite = SpriteDict["nameplatetalk"];
                        }

                        if (_vipPlate != null && _vipPlate.gameObject.active)
                        {
                            _vipPlate.sprite = SpriteDict["nameplatetalk"];
                        }
                    }
                }
                else
                {
                    if (_voiceBubble != null)
                    {
                        _voiceBubble.gameObject.active = IsMuted;

                        if (_mainPlate != null && _mainPlate.gameObject.active)
                        {
                            _mainPlate.sprite = SpriteDict["nameplate"];
                        }

                        if (_afkPlate != null && _afkPlate.gameObject.active)
                        {
                            _afkPlate.sprite = SpriteDict["nameplate"];
                        }

                        if (_vipPlate != null && _vipPlate.gameObject.active)
                        {
                            _vipPlate.sprite = SpriteDict["nameplate"];
                        }
                    }
                }
            }

            yield return new WaitForSeconds(.5f);
        }
        // ReSharper disable once IteratorNeverReturns
    }

    // This is a coroutine for all of the patches that are overcomplicated or not possible within the mod.
    // This is purely to make the mod function and stop delaying the release because of small issues that annoy me.
    // I'm extremely picky, wanting everything to work exactly how I want it to, which is why this has taken as long as it has.
    [HideFromIl2Cpp]
    private IEnumerator PlateManagement()
    {
        while (true)
        {
            if (Nameplate != null && Nameplate.active && player != null)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (UserVolume != player._USpeaker.field_Private_Single_1)
                    UserVolume = player._USpeaker.field_Private_Single_1;
                
                // if(ShowSocialRank != player.field_Private_APIUser_0.showSocialRank)
                //     ShowSocialRank = player.field_Private_APIUser_0.showSocialRank;
            }

            yield return new WaitForSeconds(2f);
        }
        // ReSharper disable once IteratorNeverReturns
    }

    public void Update()
    {
        transform.LookAt(2f * transform.position - _camera!.transform.position);
    }
    
    [HideFromIl2Cpp]
    public void ApplySettings(Vector3 position, float scaleValue, float offsetValue)
    {
        if (_transform != null)
        {
            _transform.position = new(position.x, position.y + offsetValue, position.z);
            _transform.localScale = new(scaleValue, scaleValue, scaleValue);
        }
        ApplySettings();
    }

    [HideFromIl2Cpp]
    public void ApplySettings()
    {
        try
        {
            if (player == null)
            {
                if (Nameplate != null) player = Nameplate.GetComponentInParent<Player>();
            }

            if (Settings.ModernMovement is {Value: true})
            {
                if (_constraint == null)
                {
                    _constraint = Nameplate!.AddComponent<PositionConstraint>();
                    ClassicPlates.Error("Constraint is null, forcefully adding it.");
                }
                
                if (_constraint.sourceCount > 1)
                {
                    ClassicPlates.Error("Constraint.sourceCount is greater than 1, resetting...");
                    _constraint.SetSources(null);
                }

                if (_constraint.sourceCount == 1)
                {
                    if (_constraint.GetSource(0).sourceTransform == null)
                    {
                        ClassicPlates.Debug("Removing Null Constraint Source");
                        _constraint.RemoveSource(0);
                    }
                }

                if (_constraint.sourceCount < 1)
                {
                    if (player != null)
                    {
                        var avatarManager = player._vrcplayer.field_Private_VRCAvatarManager_0;
                        if (avatarManager != null)
                        {
                            var headBone = avatarManager.field_Private_Transform_0;
                            if (headBone != null)
                            {
                                _constraint.AddSource(new ConstraintSource
                                {
                                    sourceTransform = headBone,
                                    weight = 1
                                });
                            }
                        }
                        else
                        {
                            ClassicPlates.DebugError("VRCAvatarManager is null, cannot add constraint source.");
                        }
                    }
                    else
                    {
                        ClassicPlates.Error("Could not create constraint, player is null.");
                    }
                }
            }

            if (Settings.Offset != null)
            {
                if (_constraint != null)
                {
                    _constraint.translationOffset = new Vector3(0f, Settings.Offset.Value, 0f);
                    if (Settings.ModernMovement != null) _constraint.constraintActive = Settings.ModernMovement.Value;
                }
            }

            if (Settings.ShowRank != null)
                if (_rankText != null && player != null)
                {
                    // ShowSocialRank = player.field_Private_APIUser_0.showSocialRank;
                    Rank = VRCPlayer.Method_Public_Static_String_APIUser_0(player.field_Private_APIUser_0);
                }
            if (_mainStatus != null)
                _mainStatus.gameObject.active =
                    Settings.StatusMode == VRC.NameplateManager.StatusMode.AlwaysOn && Status != "" |
                    (Settings.StatusMode == VRC.NameplateManager.StatusMode.AlwaysOff && qmOpen && Status != "");

            if (Settings.ShowMaster != null)
                if (_badgeMaster != null)
                    _badgeMaster.gameObject.active = Settings.ShowMaster.Value && IsMaster;

            if (Settings.ShowIcon != null)
                if (_userPlate != null)
                    _userPlate.gameObject.active = Settings.ShowIcon.Value && ProfilePicture != "";

            if (Settings.ShowVoiceBubble != null)
                if (_voiceBubble != null && player != null)
                    _voiceBubble.gameObject.active =
                        Settings.ShowVoiceBubble.Value && player._USpeaker.field_Private_Boolean_0;

            if (Settings.ShowInteraction != null)
                if (_iconInteract != null)
                    _iconInteract.gameObject.active = Settings.ShowInteraction.Value;

            if (Settings.PlateColor != null && Settings.PlateColorByRank != null && Settings.BtkColorPlates != null)
            {
                if (Settings.BtkColorPlates.Value)
                {
                    if (player != null) PlateColor = BonoUtils.GetColourFromUserID(player.field_Private_APIUser_0.id);
                }
                else
                {
                    if (Settings.PlateColorByRank.Value)
                    {
                        if (player != null) PlateColor = ShowSocialRank ? VRCPlayer.Method_Public_Static_Color_APIUser_0(player.field_Private_APIUser_0) : VRCPlayer.field_Internal_Static_Color_4;
                    }
                    else
                    {
                        if (ColorUtility.TryParseHtmlString(Settings.PlateColor.Value, out var color))
                            PlateColor = color;
                        else
                        {
                            PlateColor = Color.green;
                            Settings.PlateColor.Value = "#00FF00";
                            ClassicPlates.DebugError("Invalid color string for nameplate color.");
                        }
                    }
                }
            }

            if (Settings.NameColor != null && Settings.NameColorByRank != null && Settings.BtkColorNames != null)
            {
                if (Settings.BtkColorNames.Value)
                {
                    if (player != null) NameColor = BonoUtils.GetColourFromUserID(player.field_Private_APIUser_0.id);
                }
                else
                {
                    if (Settings.NameColorByRank.Value)
                    {
                        if (player != null) NameColor = ShowSocialRank ? VRCPlayer.Method_Public_Static_Color_APIUser_0(player.prop_APIUser_0) : VRCPlayer.field_Internal_Static_Color_4;
                    }
                    else
                    {
                        if (ColorUtility.TryParseHtmlString(Settings.NameColor.Value, out var color))
                            NameColor = color;
                        else
                        {
                            NameColor = Color.white;
                            Settings.NameColor.Value = "#FFFFFF";
                            ClassicPlates.DebugError("Invalid color string for name color.");
                        }
                    }
                }
            }

            if (Settings.ShowPerformance is {Value: true})
            {
                if (player != null)
                    Performance =
                        player._vrcplayer.field_Private_VRCAvatarManager_0
                            .field_Private_AvatarPerformanceStats_0
                            .GetPerformanceRatingForCategory(AvatarPerformanceCategory.Overall);
            }
            else
            {
                if (_badgePerformance != null) _badgePerformance.gameObject.active = false;
            }

            if (Settings.ShowFallback is {Value: true})
            {
                if (player != null)
                    AvatarKind =
                        player._vrcplayer.field_Private_VRCAvatarManager_0.field_Private_AvatarKind_0;
            }
            else
            {
                if (_badgeFallback != null) _badgeFallback.gameObject.active = false;
            }

            if (Settings.Enabled != null && Nameplate != null)
            {
                switch (Settings.NameplateMode)
                {
                    case VRC.NameplateManager.NameplateMode.Standard:
                        Nameplate.active = Settings.Enabled.Value && IsBlocked == false;
                        break;
                    case VRC.NameplateManager.NameplateMode.Icons:
                        Nameplate.active = Settings.Enabled.Value && IsBlocked == false;
                        break;
                    case VRC.NameplateManager.NameplateMode.Hidden:
                        Nameplate.active = false;
                        break;
                }

                if (IsLocal)
                {
                    ClassicPlates.Debug("Local Player");
                    if (Nameplate != null) Nameplate.active = false;
                }
            }
        }
        catch (Exception e)
        {
            ClassicPlates.Error("Unable to Apply Nameplate Settings: " + e);
        }
    }

    [HideFromIl2Cpp]
    public void OnQMEnable()
    {
        qmOpen = true;
        if (Settings.Enabled is not {Value: true}) return;
        if (Settings.StatusMode == VRC.NameplateManager.StatusMode.ShowOnMenu |
            Settings.StatusMode == VRC.NameplateManager.StatusMode.AlwaysOn && Status != "")
            if (_mainStatus != null)
                _mainStatus.gameObject.active = true;

        if (Settings.ShowSelfOnMenu is {Value: true} && IsLocal)
            if (Nameplate != null)
                Nameplate.active = Settings.Enabled.Value && IsBlocked == false;

        if (Settings.ShowOthersOnMenu is {Value: true} && !IsLocal)
            if (Nameplate != null)
                Nameplate.active = Settings.Enabled.Value && IsBlocked == false;

        if (Settings.ShowInteractionOnMenu is {Value: true} | Settings.ShowInteraction is {Value: true})
        {
            if (_iconInteract != null)
                _iconInteract.gameObject.active = true;
        }
    }

    [HideFromIl2Cpp]
    public void OnQMDisable()
    {
        qmOpen = false;
        if (Settings.StatusMode == VRC.NameplateManager.StatusMode.ShowOnMenu |
            Settings.StatusMode == VRC.NameplateManager.StatusMode.AlwaysOff)
            if (_mainStatus != null)
                _mainStatus.gameObject.active = false;

        if (IsLocal | (!IsLocal && Settings.NameplateMode == VRC.NameplateManager.NameplateMode.Hidden))
        {
            if(Nameplate != null)
                Nameplate.active = false;
        }

        if (Settings.ShowInteractionOnMenu is {Value: true} | Settings.ShowInteraction is {Value: false})
        {
            if (_iconInteract != null)
                _iconInteract.gameObject.active = false;
        }
    }

    public void OnStatusModeChanged(VRC.NameplateManager.StatusMode statusMode)
    {
        if (statusMode == VRC.NameplateManager.StatusMode.AlwaysOn)
        {
            if (_mainStatus != null)
                _mainStatus.gameObject.active = true;
        }
        else
        {
            if (_mainStatus != null)
                _mainStatus.gameObject.active = false;
        }
    }

    public void OnNameplateModeChanged(VRC.NameplateManager.NameplateMode nameplateMode)
    {
        if (IsLocal) return;
        if (Settings.Enabled == null) return;
        switch (nameplateMode)
        {
            case VRC.NameplateManager.NameplateMode.Hidden:
            {
                if (Nameplate != null)
                    Nameplate.active = false;
                break;
            }

            case VRC.NameplateManager.NameplateMode.Icons:
            {
                if (Nameplate != null)
                        Nameplate.active = Settings.Enabled.Value && IsBlocked == false;
                break;
            }

            case VRC.NameplateManager.NameplateMode.Standard:
            {
                if (Nameplate != null)
                    Nameplate.active = Settings.Enabled.Value && IsBlocked == false;
                break;
            }

            case VRC.NameplateManager.NameplateMode.MAX:
            {
                if (Nameplate != null)
                    Nameplate.active = Settings.Enabled.Value && IsBlocked == false;
                break;
            }
        }
    }
}

// public enum InteractionOverride
// {
//     None,
//     AlwaysOn,
//     AlwaysOff
// }