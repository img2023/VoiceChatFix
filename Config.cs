using Exiled.API.Interfaces;

namespace VoiceChatFix
{
    internal sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;
        public ushort UploadMbpsThreshold { get; set; } = 60;
    }
}
