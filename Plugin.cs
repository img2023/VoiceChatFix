using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using HarmonyLib;
using MEC;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace VoiceChatFix
{
    internal sealed class Plugin : Plugin<Config>
    {
        private static long LastSent { get; set; }
        private static double UploadMbps { get; set; }
        private static CoroutineHandle MyCoroutineHandle { get; set; }
        private static Plugin Instance { get; set; }
        private static Harmony Harmony { get; set; }
        public override string Author => "img0";
        public override string Name => "VoiceChatFix";
        public override void OnEnabled()
        {
            Instance = this;
            Harmony = new Harmony($"VoiceChatFix-{DateTime.Now.Ticks}");
            Harmony.PatchAll();
            MyCoroutineHandle = Timing.RunCoroutine(UpdateMbps());
            Exiled.Events.Handlers.Player.VoiceChatting += OnVoiceChatting;
            base.OnEnabled();
        }
        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.VoiceChatting -= OnVoiceChatting;
            Timing.KillCoroutines(MyCoroutineHandle);
            Harmony.UnpatchAll();
            Harmony = null;
            Instance = null;
            base.OnDisabled();
        }
        private static void OnVoiceChatting(VoiceChattingEventArgs ev)
        {
            if (!CanVoiceChat())
                ev.IsAllowed = false;
        }
        private static IEnumerator<float> UpdateMbps()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(0.5f);
                IPv4InterfaceStatistics ipis = GetNetworkInterface().GetIPv4Statistics();
                if (LastSent != default)
                {
                    long uploadBytesDifferent;
                    if (ipis.BytesSent < 0 && LastSent > 0)
                        uploadBytesDifferent = Math.Abs(-ipis.BytesSent - LastSent);
                    else
                        uploadBytesDifferent = ipis.BytesSent - LastSent;
                    UploadMbps = Math.Round(uploadBytesDifferent * 2 / 125000f, 1);
                    foreach (var player in Player.List)
                    {
                        if (player.VoiceModule == null || !player.VoiceModule.ServerIsSending || CanVoiceChat()) continue;
                        player.PlayBeepSound();
                        player.Broadcast(new Exiled.API.Features.Broadcast($"服务器当前上传速度（{UploadMbps}Mbps）达到阈值，为避免服务器卡顿，你的语音已被限制，请稍后再发言", 1));
                    }
                }
                LastSent = ipis.BytesSent;
            }
        }
        private static bool CanVoiceChat() => UploadMbps < Instance.Config.UploadMbpsThreshold;
        private static NetworkInterface GetNetworkInterface()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!ni.Supports(NetworkInterfaceComponent.IPv4)) continue;
                return ni;
            }
            return default;
        }
    }
}
