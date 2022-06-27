using MelonLoader;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

// Thanku Rin for this version of EnableDisableListening.
// https://github.com/RinLovesYou/VrcSpotifyIntegration/blob/58e028fc097ea4235a780e89ffcf5ce33e133d72/VRCSpotifyMod/EnableDisableListener.cs
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace ClassicPlates.MonoScripts;

[RegisterTypeInIl2Cpp]
internal class EnableDisableListener : MonoBehaviour
{
    [method: HideFromIl2Cpp] public event Action? OnEnableEvent;
    [method: HideFromIl2Cpp] public event Action? OnDisableEvent;

    public EnableDisableListener(IntPtr obj) : base(obj)
    {
    }

    public void OnEnable() => OnEnableEvent?.Invoke();
    public void OnDisable() => OnDisableEvent?.Invoke();
}