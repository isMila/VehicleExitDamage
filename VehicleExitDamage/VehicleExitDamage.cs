using HarmonyLib;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;
using Steamworks;

namespace VehicleExitDamage
{
    public class VehicleExitDamagePlugin : RocketPlugin<Config>
    {
        public static VehicleExitDamagePlugin Instance { get; private set; }
        private Harmony harmony;

        private const string SenderName = "VehicleExitDamage";
        private const string ChatIconUrl = "https://imgur.com/LK914gE.png";

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "damage_message", "You took {0} damage for jumping out at {1} km/h." }
        };

        protected override void Load()
        {
            Instance = this;
            harmony = new Harmony("com.milahosting.vehicleexitdamage");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Logger.Log("");
            Logger.Log("=============================================");
            Logger.Log("");
            Logger.Log("               ███╗   ███╗");
            Logger.Log("               ████╗ ████║");
            Logger.Log("               ██╔████╔██║");
            Logger.Log("               ██║╚██╔╝██║");
            Logger.Log("               ██║ ╚═╝ ██║");
            Logger.Log("               ╚═╝     ╚═╝");
            Logger.Log("");
            Logger.Log("---------------------------------------------");
            Logger.Log("       Plugin: VehicleExitDamage");
            Logger.Log("       Created by: Mila");
            Logger.Log("       Contact: milahosting.com");
            Logger.Log("=============================================");
            Logger.Log("");
        }

        protected override void Unload()
        {
            Instance = null;
            harmony.UnpatchAll(harmony.Id);
            Logger.Log("VehicleExitDamage Plugin unloaded!");
        }

        public void ApplyFallDamage(UnturnedPlayer player, float damage, float speed)
        {
            StartCoroutine(DamageCoroutine(player, damage, speed));
        }

        private IEnumerator DamageCoroutine(UnturnedPlayer player, float damage, float speed)
        {
            yield return new WaitForFixedUpdate();

            if (player == null || player.Dead)
            {
                yield break;
            }

            string message = Translate("damage_message", (int)damage, (int)speed);
            string richMessage = $"<color=yellow>[{SenderName}] {message}</color>";
            ChatManager.serverSendMessage(richMessage, Color.white, null, player.SteamPlayer(), EChatMode.SAY, ChatIconUrl, true);

            byte damageToApply = damage >= 100 ? (byte)101 : (byte)damage;
            player.Player.life.askDamage(damageToApply, Vector3.up, EDeathCause.VEHICLE, ELimb.SPINE, CSteamID.Nil, out _);
        }
    }

    [HarmonyPatch(typeof(InteractableVehicle), "removePlayer")]
    public static class Vehicle_RemovePlayer_Patch
    {
        public static void Prefix(InteractableVehicle __instance, byte seatIndex)
        {
            try
            {
                var plugin = VehicleExitDamagePlugin.Instance;
                var config = plugin.Configuration.Instance;

                if (!config.Enabled) return;

                if (__instance?.passengers == null || seatIndex >= __instance.passengers.Length) return;

                Passenger passenger = __instance.passengers[seatIndex];
                if (passenger?.player == null) return;

                UnturnedPlayer player = UnturnedPlayer.FromSteamPlayer(passenger.player);
                if (player == null) return;

                if (config.IgnoreAdmins && player.IsAdmin) return;

                float speedKPH = __instance.ReplicatedSpeed * 3.6f;
                var sortedTiers = config.DamageTiers.OrderByDescending(t => t.MinSpeed);

                foreach (var tier in sortedTiers)
                {
                    if (speedKPH >= tier.MinSpeed)
                    {
                        plugin.ApplyFallDamage(player, tier.Damage, speedKPH);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error in VehicleExitDamage patch.");
            }
        }
    }
}