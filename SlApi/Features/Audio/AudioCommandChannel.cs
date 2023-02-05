using PluginAPI.Core;

namespace SlApi.Audio
{
    public class AudioCommandChannel
    {
        public readonly ReferenceHub Owner;

        public AudioCommandChannel(ReferenceHub owner)
        {
            Owner = owner;
        }

        public void Write(object message)
        {
            if (Owner == null)
                return;

            if (Owner.Mode != ClientInstanceMode.ReadyClient)
                return;

            if (Owner.connectionToClient == null)
                return;

            Owner.characterClassManager.ConsolePrint($"[Audio Info] {message}", "red");
            Owner.queryProcessor.TargetReply(Owner.connectionToClient, $"[Audio Info] {message}", false, true, "");

            Log.Info($"{Owner.nicknameSync.MyNick} ({Owner.characterClassManager.UserId}): [Audio Info] {message}", "Command Channel");
        }
    }
}
