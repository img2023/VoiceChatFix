using HarmonyLib;
using System.Collections.Generic;
using VoiceChat.Networking;

namespace VoiceChatFix
{
    [HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
    internal sealed class VoiceTransceiver_ServerReceiveMessage_Patch
    {
        private static void Prefix(ref VoiceMessage msg)
        {
            List<byte> data = new List<byte>();
            for (int i = 0; i < msg.DataLength; i++)
                data.Add(msg.Data[i]);
            msg.Data = data.ToArray();
        }
    }
}
