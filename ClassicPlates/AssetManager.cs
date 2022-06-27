using System.Collections;
using System.Reflection;
using MelonLoader;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace ClassicPlates;

internal static class AssetManager
{
    internal static GameObject? Nameplate;
    private static AssetBundle? _bundle;
    public static readonly Dictionary<string, Sprite>? SpriteDict = new();
    public static Il2CppReferenceArray<Sprite>? SpeakingSprites;
    public static Il2CppReferenceArray<Sprite>? MutedSprites;

    private static GameObject LoadPrefab(string @object)
    {
        if (_bundle is null)
        {
            ClassicPlates.Error($"Failed to load Prefab: {@object}");
            throw new FileLoadException();
        }
        var go = _bundle.LoadAsset_Internal(@object, Il2CppType.Of<GameObject>()).Cast<GameObject>();
        go.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        go.hideFlags = HideFlags.HideAndDontSave;
        ClassicPlates.Debug($"Loaded Prefab: {@object}");
        return go;
    }

    private static Sprite LoadSprite(string sprite)
    {
        if (_bundle is null)
        {
            ClassicPlates.Error($"Failed to load Sprite: {sprite}");
            throw new FileLoadException();
        }
        var sprite2 = _bundle.LoadAsset_Internal(sprite, Il2CppType.Of<Sprite>()).Cast<Sprite>();
        sprite2.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        sprite2.hideFlags = HideFlags.HideAndDontSave;
        ClassicPlates.Debug($"Loaded Sprite: {sprite}");
        return sprite2;
    }

    static IEnumerator LoadResources()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ClassicPlates.Resources.classicplates");
        if (stream != null)
        {
            using var memoryStream = new MemoryStream((int) stream.Length);
            stream.CopyTo(memoryStream);
            _bundle = AssetBundle.LoadFromMemory_Internal(memoryStream.ToArray(), 0);
            try
            {
                Nameplate = LoadPrefab("Nameplate.prefab") ?? throw new Exception("AssetLoadException");

                SpriteDict?.Add("bubble0",
                    LoadSprite("bubble_0.png") ?? throw new Exception("AssetLoadException: bubble_0.png"));
                SpriteDict?.Add("bubble1",
                    LoadSprite("bubble_1.png") ?? throw new Exception("AssetLoadException: bubble_1.png"));
                SpriteDict?.Add("bubble2",
                    LoadSprite("bubble_2.png") ?? throw new Exception("AssetLoadException: bubble_2.png"));
                SpriteDict?.Add("bubble3",
                    LoadSprite("bubble_3.png") ?? throw new Exception("AssetLoadException: bubble_3.png"));
                SpriteDict?.Add("bubblemute",
                    LoadSprite("bubble_mute.png") ?? throw new Exception("AssetLoadException: bubble_mute.png"));

                SpriteDict?.Add("ear",
                    LoadSprite("ear.png") ?? throw new Exception("AssetLoadException: ear.png"));
                SpriteDict?.Add("earmute",
                    LoadSprite("ear_mute.png") ?? throw new Exception("AssetLoadException: ear_mute.png"));

                SpriteDict?.Add("defaulticon",
                    LoadSprite("icon_default.png") ?? throw new Exception("AssetLoadException: icon_default.png"));
                SpriteDict?.Add("iconborder",
                    LoadSprite("IconBorder.png") ?? throw new Exception("AssetLoadException: IconBorder.png"));
                SpriteDict?.Add("friend",
                    LoadSprite("friend_icon.png") ?? throw new Exception("AssetLoadException: friend_icon.png"));
                SpriteDict?.Add("quest",
                    LoadSprite("quest.png") ?? throw new Exception("AssetLoadException: quest.png"));
                SpriteDict?.Add("crown",
                    LoadSprite("crown.png") ?? throw new Exception("AssetLoadException: crown.png"));

                SpriteDict?.Add("nameplate",
                    LoadSprite("NameplateSilent.png") ?? throw new Exception("AssetLoadException: NameplateSilent.png"));
                SpriteDict?.Add("nameplatetalk",
                    LoadSprite("NameplateTalk.png") ?? throw new Exception("AssetLoadException: NameplateTalk.png"));
                SpriteDict?.Add("nameplatemask",
                    LoadSprite("NameplateMask.png") ?? throw new Exception("AssetLoadException: NameplateMask.png"));

                SpriteDict?.Add("fallback",
                    LoadSprite("fallback_icon.png") ?? throw new Exception("AssetLoadException: fallback_icon.png"));
                SpriteDict?.Add("fallbackerror",
                    LoadSprite("perf_fallback_error.png") ?? throw new Exception("AssetLoadException: perf_fallback_error.png"));
                SpriteDict?.Add("fallbackmissing",
                    LoadSprite("perf_fallback_missing.png") ?? throw new Exception("AssetLoadException: perf_fallback_missing.png"));
                SpriteDict?.Add("fallbackgreat",
                    LoadSprite("perf_fallback_great.png") ?? throw new Exception("AssetLoadException: perf_fallback_great.png"));
                SpriteDict?.Add("fallbackgood",
                    LoadSprite("perf_fallback_good.png") ?? throw new Exception("AssetLoadException: perf_fallback_good.png"));
                SpriteDict?.Add("fallbackmedium",
                    LoadSprite("perf_fallback_medium.png") ?? throw new Exception("AssetLoadException: perf_fallback_medium.png"));
                SpriteDict?.Add("fallbackpoor",
                    LoadSprite("perf_fallback_poor.png") ?? throw new Exception("AssetLoadException: perf_fallback_poor.png"));
                SpriteDict?.Add("fallbackhorrible",
                    LoadSprite("perf_fallback_horrible.png") ?? throw new Exception("AssetLoadException: perf_fallback_horrible.png"));

                SpriteDict?.Add("great",
                    LoadSprite("Perf_Great.png") ?? throw new Exception("AssetLoadException: Perf_Great.png"));
                SpriteDict?.Add("good",
                    LoadSprite("Perf_Good.png") ?? throw new Exception("AssetLoadException: Perf_Good.png"));
                SpriteDict?.Add("medium",
                    LoadSprite("Perf_Medium.png") ?? throw new Exception("AssetLoadException: Perf_Medium.png"));
                SpriteDict?.Add("poor",
                    LoadSprite("Perf_Poor.png") ?? throw new Exception("AssetLoadException: Perf_Poor.png"));
                SpriteDict?.Add("horrible",
                    LoadSprite("Perf_Horrible.png") ?? throw new Exception("AssetLoadException: Perf_Horrible.png"));
                SpriteDict?.Add("blocked",
                    LoadSprite("Perf_Blocked.png") ?? throw new Exception("AssetLoadException: Perf_Blocked.png"));
                SpriteDict?.Add("imposter",
                    LoadSprite("perf_impostor.png") ?? throw new Exception("AssetLoadException: perf_impostor.png"));

                SpriteDict?.Add("physyes",
                    LoadSprite("PhysYes.png") ?? throw new Exception("AssetLoadException: PhysYes.png"));
                SpriteDict?.Add("physno",
                    LoadSprite("PhysNo.png") ?? throw new Exception("AssetLoadException: PhysNo.png"));
                
                CreateSpriteArrays();
            }
            catch (Exception e)
            {
                ClassicPlates.Error($"Nameplate Assets failed to load\n\n{e}");
            }
        }
        else
        {
            ClassicPlates.Error("Stream is null, Nameplates cannot load");
        }

        yield break;
    }
    public static void Init() => MelonCoroutines.Start(LoadResources());
 
        
    private static void CreateSpriteArrays()
    {
        if (SpriteDict == null) return;
        
        SpeakingSprites = new Il2CppReferenceArray<Sprite>(new[]
        {
            SpriteDict["bubble0"],
            SpriteDict["bubble1"],
            SpriteDict["bubble2"],
            SpriteDict["bubble3"]
        });
            
        MutedSprites = new Il2CppReferenceArray<Sprite>(new[]
        {
            SpriteDict["bubblemute"]
        });
    }
}