using System;
using System.ComponentModel;
using System.Reflection;
using System.Security.Cryptography;
using System.Net.Http;

using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System.Net.Configuration;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using SkyFrost.Base;
using System.Threading;
using Elements.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Drawing.Printing;

namespace ADSessionAnnouncer;
//More info on creating mods can be found https://github.com/resonite-modding-group/ResoniteModLoader/wiki/Creating-Mods
public class ADSessionAnnouncer : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.0.0"; //Changing the version here updates it in all locations needed
	public override string Name => "ADSessionAnnouncer";
	public override string Author => "Grand";
	public static ModConfiguration? Config;
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/GrandtheUK/ADSessionAnnouncer";

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<bool> Enabled =
		new("Enabled","Enable AD Session Announcer", () => false);

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<string> Server =
		new("Server","AD Session API Server", () => "");
		
	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<string> ServerKey =
		new("ServerKey","AD Session API Server", () => "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<string> CommunityName =
		new("CommunityName","Name of Community hosting AD Session", () => "No Community");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<string> DiscordLink =
		new("DiscordLink","Community Discord Link", () => "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<string> LogoUri =
		new("LogoUri","Community Logo Uri", () => "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<List<ADSession>> Sessions =
		new("Sessions","List of Configs for sessions", () => new List<ADSession>());

	public override void OnEngineInit() {
		if (!ModLoader.IsHeadless)
		{
			Msg("This mod should only be ran on headlesses. Please remove this mod on non-headlesses");
			return;
		}
		Config = GetConfiguration();
		Config.Save(true);
		Harmony harmony = new("GrandtheUK.ADSessionAnnouncer");
		harmony.PatchAll();
		Engine.Current.RunPostInit(() => {
			Engine.Current.WorldManager.WorldAdded += ADSessionAnnounce;
		});
	}

	private void ADSessionAnnounce(World w) {
		if (!(Config?.GetValue<bool>(Enabled)) ?? true) {
			Msg("ADSessionAnnouncer is not enabled. Not Announcing");
			return;
		}
		List<ADSession> ad_list = Config?.GetValue<List<ADSession>>(Sessions) ?? new List<ADSession>();
		if (!ad_list.Any()) {
			Msg("list of AD Session is empty. Not Announcing");
			return;
		}
		ADSession ad = ad_list.Find( ad => { return ad.SessionId == w.SessionId; });
		if (ad.SessionId==""){
			Msg("SessionId is not in list of AD Sessions. Not Announcing");
			return;
		}
		async void Announce() {
			List<ADSession> ad_list = Config?.GetValue(Sessions) ?? new List<ADSession>();
			string Logo = Config?.GetValue(LogoUri);
			string Community = Config?.GetValue(CommunityName);
			string Discord = Config?.GetValue(DiscordLink);
			ADSession ad = ad_list.Find( ad => { return ad.SessionId == w.SessionId; });
			if (w.AccessLevel!= SessionAccessLevel.RegisteredUsers || !w.HideFromListing) {
				Msg($"Session is not set to RegisteredUsers and isn't hidden from the Session Browser. Not Announcing. accessLevel: {w.AccessLevel}, hidden:{w.HideFromListing}");
				return;
			}
			List<FrooxEngine.User> users = new();
			String userlist = "";
			w.GetUsers(users);
			foreach (FrooxEngine.User user in users) {
				if (user != w.HostUser) {
					userlist+=$"{user.UserID},";
				}
			}

			String key = (Config?.GetValue<String>(ServerKey)) ?? "";
			var form = new Dictionary<string,string>
			{
				{"oper","push"},
				{"key", key},
				{"session", w.SessionId},
				{"communityname", Community},
				{"communitydiscord", Discord},
				{"communitylogo", Logo},
				{"sessionpreview", ad.PreviewUri},
				{"sessionname", w.Name},
				{"sessionhost", Engine.Current.LocalUserName},
				{"usercount", $"{w.UserCount-1}/{w.MaxUsers-1}"},
				{"userlist", userlist}
			};
			Msg($"Announcing Session ({w.SessionId}) with preview ({ad.PreviewUri})");

			HttpClient client = new();
			String requestUri = (Config?.GetValue(Server)) ?? "";

			HttpContent content = new FormUrlEncodedContent(form);
			Task<HttpResponseMessage> response = client.PostAsync(requestUri, content);

			String StringResponse = await response.Result.Content.ReadAsStringAsync();
			if (response.Result.IsSuccessStatusCode) {
				Msg($"Connection success");
			} else {
				Msg($"Connection unsuccessful");
			}
			w.RunInSeconds(30,Announce);
		}
		w.RunInSeconds(30,Announce);
	}
	
	public record struct ADSession(string SessionId, string PreviewUri);
}