using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs.Player;
using HarmonyLib;
using MEC;
using Mirror;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using VoiceChat.Networking;

namespace VoiceChatFix
{
    internal sealed class Plugin : Plugin<Config>
    {
        private static long LastSent;
        private static double UploadMbps;
        private static CoroutineHandle MyCoroutineHandle;
        private static Plugin Instance;
        private static readonly Harmony harmony = new Harmony($"VoiceChatFix-{DateTime.Now.Ticks}");
        public override string Author => "img0";
        public override string Name => "VoiceChatFix";
        public override Version Version => new Version(1, 0, 0);
        public override void OnEnabled()
        {
            Instance = this;
            harmony.PatchAll();
            MyCoroutineHandle = Timing.RunCoroutine(UpdateMbps());
            Exiled.Events.Handlers.Player.VoiceChatting += OnVoiceChatting;
            base.OnEnabled();
        }
        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.VoiceChatting -= OnVoiceChatting;
            Timing.KillCoroutines(MyCoroutineHandle);
            harmony.UnpatchAll();
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
                    long uploadBps;
                    if (ipis.BytesSent < 0 && LastSent > 0)
                        uploadBps = Math.Abs(-ipis.BytesSent - LastSent);
                    else
                        uploadBps = ipis.BytesSent - LastSent;
                    UploadMbps = Math.Round(uploadBps * 2 / 125000f, 1);
                    foreach (var player in Player.List)
                    {
                        if (player.VoiceModule == null || !player.VoiceModule.ServerIsSending || CanVoiceChat()) continue;
                        player.PlayBeepSound();
                        player.Broadcast(new Exiled.API.Features.Broadcast($"<color=red>服务器当前上传速度（{UploadMbps}Mbps）达到阈值，为避免服务器卡顿，你的语音已被限制，请稍后再发言</color>"));
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
    [HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
    internal sealed class VoiceTransceiverPatch
    {
        private static void Prefix(ref VoiceMessage msg)
        {
            List<byte> data = new List<byte>();
            for (int i = 0; i < msg.DataLength; i++)
                data.Add(msg.Data[i]);
            msg.Data = data.ToArray();
        }
    }
    internal sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;
        public ushort UploadMbpsThreshold { get; set; } = 60;
    }
}
