using CommandSystem;

using SlApi.Extensions;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.AdminVoiceStates;
using SlApi.Features.Voice.AdminVoice;

using System;

namespace SlApi.Commands
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class AdminVoiceChannelCommand : ICommand
	{
		public string Command { get; } = "adminvoicechannel";
		public string[] Aliases { get; } = new string[] { "avch" };
		public string Description { get; } = "Adds/removes you to the admin only voice channel.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (arguments.Count != 2)
			{
				response = $"Missing arguments! adminvoicechannel <target> <id>\n\nActive channels:\n";

				foreach (var cchannel in AdminVoiceProcessor.CustomChannels)
					response += $"	- [{cchannel.Id}] {cchannel.Name} ({cchannel.VoiceChannel})\n";

				response += "\nPredefined channels:\n";

				foreach (var cchannel in AdminVoiceProcessor.PredefinedChannels)
                    response += $"	- [{cchannel.Id}] {cchannel.Name} ({cchannel.VoiceChannel})\n";

                return false;
			}

			var player = HubExtensions.GetHub(arguments.At(0));

			if (player is null)
			{
				response = $"Player does not exist.";
				return false;
			}

			if (!byte.TryParse(arguments.At(1), out var channelId))
			{
				response = $"Failed to parse byte: {arguments.At(1)} (value must be between {byte.MinValue} and {byte.MaxValue}).";
				return false;
			}

			if (!AdminVoiceProcessor.TryGetChannel(channelId, out var channel))
			{
				response = $"Failed to find a channel with ID {channel}.";
				return false;
			}

			if (!player.TryGetState<AdminVoiceState>(out var adminVoiceState))
				player.TryAddState(adminVoiceState = new AdminVoiceState(player));

			if (adminVoiceState.CurrentChannel.HasValue && adminVoiceState.CurrentChannel.Value == channelId)
			{
				adminVoiceState.DeclineFromChannel(channelId);
				response = $"You were removed from {channelId}.";
				return true;
			}
			else
			{
				adminVoiceState.AllowToChannel(channelId);
				response = $"You were added to {channelId}.";
				return true;
			}
		}
	}
}