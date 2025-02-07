using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using AmongUs.GameOptions;
using Assets.CoreScripts;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using Reactor;
using Reactor.Utilities;
using Reactor.Utilities.Attributes;
using Reactor.Utilities.Extensions;
using Reactor.Utilities.ImGui;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SigmaMenu;

[BepInAutoPlugin]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
public partial class SigmaMenuPlugin : BasePlugin
{
    public Harmony Harmony { get; } = new(Id);

    public static ConfigEntry<bool> AlwaysImpostor { get; private set; }

    public static ConfigEntry<bool> RevealImps { get; private set; }
    
    public static Sprite SkibidiSprite { get; private set; }
    
    public static Vector3 LastPosition { get; set; }
    
    public static bool lastNoClip { get; set; }
    public static bool NoClip { get; set; }
    private static byte[] fakeImps = [];

    private static bool spamchat;

    public override void Load()
    {
        AlwaysImpostor = Config.Bind("Cheats", "Always Skibidi", false);
        RevealImps = Config.Bind("Cheats", "Reveal Skibidis", false);
        
        ReactorCredits.Register("USING SIGMA MENU", Version, false, _ => true);
        AddComponent<SigmaMenu>();
        Harmony.PatchAll();
    }
    
    [RegisterInIl2Cpp]
    public class SigmaMenu(IntPtr ptr) : MonoBehaviour(ptr)
    {
        [HideFromIl2Cpp]
        public DragWindow SigmaWindow { get; } = new(
            new Rect(10, 10, 0, 0),
            "SIGMA MENU",
            () =>
            {
                GUILayout.Label("Skibidi options");
                AlwaysImpostor.Value = GUILayout.Toggle(AlwaysImpostor.Value, "Always Skibidi");
                RevealImps.Value = GUILayout.Toggle(RevealImps.Value, "Reveal Skibidis");
                GUILayout.Label("Other sussy options");
                NoClip = GUILayout.Toggle(NoClip, "No Sigma Clip");
                spamchat = GUILayout.Toggle(spamchat, "Rizz up chat");
            })
        {
            Enabled = true,
        };

        public void OnGUI()
        {
            SigmaWindow.OnGUI();
        }
    }
    

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    public static class PlayerControlPatches
    {
        public static void Postfix(PlayerControl __instance)
        {
            SkibidiSprite = LoadSpriteFromPath("SigmaMenu.Resources.skbidi.png", Assembly.GetExecutingAssembly(), 200);
            var skibidi = new GameObject("Skibidi");
            var skibidiRenderer = skibidi.AddComponent<SpriteRenderer>();
            skibidiRenderer.sprite = SkibidiSprite;
            skibidiRenderer.material = HatManager.Instance.PlayerMaterial;
            skibidiRenderer.flipX = !__instance.cosmetics.FlipX;

            skibidi.transform.SetParent(__instance.transform, false);
            skibidi.transform.localPosition = new Vector3(0, 0, -0.1f);
            skibidi.layer = 8;
        }
    }
    

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    public static class chatspammer
    {
        private static string[] brainrot =
        [
            "gyats in ohoi",
            "skibidi toilet will be mine yeah ohio rizzer gyat",
            "Sigma Sigma on the Wall, who is the skibidiest of them all",
            "Skibidi Bop Yes Yes",
            "Rizz em up",
            "keep mewing bro",
            "Sigma grindset",
            "WHAT KINDA BOMBOCLAT DAWG ARE YA",
            "put the fries in the bag",
            "duke dennis did you pray today",
            "the rizzler is so goated with the sauce",
            "Fortnite Balls",
            "Im feeling romantical",
            "Sigmaing my gyat rizzer in ohio without hawk tuah",
            "skibidi skibidi hawk tuah hawk",
            "hawk tuah",
            "you gotta hawk tuah, spit on that thang",
        ];
        
        public static void Postfix(ChatController __instance)
        {
            if (spamchat)
            {
                __instance.AddChat(PlayerControl.LocalPlayer, brainrot.Random(), false);
            }
        }
    }
    
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class hudpatc
    {
        public static void Postfix(HudManager __instance)
        {
            if (AlwaysImpostor.Value &&  !PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                var plrs =
                    PlayerControl.LocalPlayer.Data.Role.GetPlayersInAbilityRangeSorted(
                        RoleBehaviour.GetTempPlayerList());
                var vent = Object.FindObjectsOfType<Vent>().Random();

                var localPlayer = PlayerControl.LocalPlayer;
                
                __instance.KillButton.gameObject.SetActive(true);
                __instance.SabotageButton.gameObject.SetActive(true);
                __instance.SabotageButton.GetComponent<PassiveButton>().AddOnClickListeners(
                    (Action)(() =>
                    {
                        __instance.ToggleMapVisible(new MapOptions()
                        {
                            Mode = MapOptions.Modes.Sabotage
                        });
                    }));
                __instance.ImpostorVentButton.gameObject.SetActive(true);
                __instance.ImpostorVentButton.GetComponent<PassiveButton>().AddOnClickListeners(
                    (Action)(() =>
                    {
                        localPlayer.NetTransform.SnapTo(vent.transform.position);
                        if (localPlayer.inVent && !localPlayer.walkingToVent)
                        {
                            localPlayer.MyPhysics.StartCoroutine(localPlayer.MyPhysics.CoExitVent(vent.Id));
                            vent.SetButtons(false);
                            return;
                        }
                        if (!localPlayer.walkingToVent)
                        {
                            localPlayer.MyPhysics.StartCoroutine(localPlayer.MyPhysics.CoEnterVent(vent.Id));
                            vent.SetButtons(true);
                        }                    
                    }));
                __instance.ImpostorVentButton.SetTarget(vent);
                if (plrs.Count > 0)
                {
                    __instance.KillButton.SetTarget(plrs.ToArray()[0]);
                }
            }
            else if (!PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                __instance.SabotageButton.gameObject.SetActive(false);
                __instance.ImpostorVentButton.gameObject.SetActive(false);
                __instance.KillButton.gameObject.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
    public static class Shippatc3h
    {
        public static void Postfix(ShipStatus __instance, ref float __result)
        {
            if (AlwaysImpostor.Value && !PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                __result = __instance.MaxLightRadius * GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.ImpostorLightMod);
            }
        }
    }


    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    public static class Shippatch
    {
        public static void Postfix()
        {
            var maxImp = GameManager.Instance.LogicOptions.GetAdjustedNumImpostors(GameData.Instance.PlayerCount);
            Logger<SigmaMenuPlugin>.Error(maxImp);
            fakeImps = new byte[maxImp];
            var plrs = PlayerControl.AllPlayerControls.ToArray().Where(x => !x.Data.Role.IsImpostor && !x.AmOwner).ToArray();

            for (var i = 0; i < maxImp; i++)
            {
                fakeImps[i] = plrs.Random().PlayerId;
                Logger<SigmaMenuPlugin>.Error(fakeImps[i]);
            }
        }
    }
    

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public static class PlayerControlYpgfaPatches
    {
        public static void Postfix(PlayerControl __instance)
        {
            var skibidi = __instance.transform.Find("Skibidi").GetComponent<SpriteRenderer>();
            __instance.SetPlayerMaterialColors(skibidi);
            skibidi.flipX = !__instance.cosmetics.FlipX;

            if (RevealImps.Value && fakeImps.Contains(__instance.PlayerId))
            {
                __instance.cosmetics.SetNameColor(Palette.ImpostorRed);
            }
            else if (!__instance.Data.Role.IsImpostor)
            {
                __instance.cosmetics.SetNameColor(Color.white);
            }
            
            if (!__instance.AmOwner)
            {
                return;
            }

            if (AlwaysImpostor.Value && !PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                __instance.cosmetics.SetNameColor(Palette.ImpostorRed);
            }
            
            if (NoClip && !lastNoClip)
            {
                LastPosition = __instance.transform.position;
            }
            if (!NoClip && lastNoClip)
            {
                __instance.NetTransform.SnapTo(LastPosition);
            }

            __instance.NetTransform.isPaused = NoClip;
            __instance.Collider.enabled = !NoClip;
            lastNoClip = NoClip;
        }
    }


    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
    public static class PlayerControlPatches2
    {
        public static bool Prefix(PlayerControl __instance, string chatText, ref bool __result)
        {
            if (!__instance.AmOwner)
            {
                return true;
            }

            chatText = Regex.Replace(chatText, "<.*?>", string.Empty);
            if (string.IsNullOrWhiteSpace(chatText))
            {
                __result = false;
                return false;
            }
            if (AmongUsClient.Instance.AmClient && HudManager.Instance)
            {
                HudManager.Instance.Chat.AddChat(__instance, chatText);
            }
            if (chatText.Contains("who", StringComparison.OrdinalIgnoreCase))
            {
                UnityTelemetry.Instance.SendWho();
            }
            
            var messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, 13);
            messageWriter.Write("im hacking, skibidi hawk tuah. report me");
            messageWriter.EndMessage();
            __result = true;
            return false;
        }
    }
    
    public static Sprite LoadSpriteFromPath(string resourcePath, Assembly assembly, float pixelsPerUnit)
    {
        var tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        var myStream = assembly.GetManifestResourceStream(resourcePath);
        var buttonTexture = myStream.ReadFully();
        tex.LoadImage(buttonTexture, false);

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
}