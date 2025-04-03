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
using ADSession = Dictionary<string,string>;
//More info on creating mods can be found https://github.com/resonite-modding-group/ResoniteModLoader/wiki/Creating-Mods
public class ADSessionAnnouncer : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.1.0"; //Changing the version here updates it in all locations needed
	public override string Name => "ADSessionAnnouncer";
	public override string Author => "Grand";
	public static ModConfiguration? Config;
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/GrandtheUK/ADSessionAnnouncer";
	private Thread _thread;
	private bool _loop;
	private List<World> _worlds;
	private HttpClient _client;
	private string key;

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
		// start Announce thread and create onShutdown hook to stop it
		Engine.Current.RunPostInit(() => {
			_thread = new Thread(() => Announce());
			Engine.Current.OnShutdown += StopAnnouce;
			_thread.Start();
			if (_thread.IsAlive){
				Msg("Announce Thread is alive");
			}
		});
	}

	void StopAnnouce() {
		_loop = false;
		_thread.Join();
	}
	async void Announce() {
		_worlds = new();
		_client = new();
		List<ADSession> ADList = new();
		key = (Config?.GetValue(ServerKey)) ?? "";

		// initialise loop if mod is enabled.
		_loop = (Config?.GetValue(Enabled)) ?? false;
		while (_loop) {
			string Logo = Config?.GetValue(LogoUri) ?? "";
			string Community = Config?.GetValue(CommunityName) ?? "No Community";
			string Discord = Config?.GetValue(DiscordLink) ?? "";
			Engine.Current.WorldManager.GetWorlds(_worlds);

			
			// Filter world list to only those that have an entry in the AD session list config
			ADList = Config?.GetValue(Sessions) ?? new();
			List<string> SessionFilter = new();
			foreach(ADSession session in ADList) {
				SessionFilter.Add(session["SessionId"]);
			}
			
			_worlds = _worlds.Where( w => {return SessionFilter.Contains(w.SessionId);}).ToList();
			

			//If no world remain then sleep for 30s before retrying on the next interval
			if (!_worlds.Any()) {
				Thread.Sleep(30000);
				continue;
			}

			// Announce each world remaining in the world list
			foreach (World w in _worlds) {
				ADSession ad = ADList.Find( ad => { return ad["SessionId"] == w.SessionId; });
				// If a session isn't set to RegisteredUsers and hidden from the normal session browser, do not announce
				if (w.AccessLevel!= SessionAccessLevel.RegisteredUsers || !w.HideFromListing) {
					Msg($"Session {w.SessionId} is not set to RegisteredUsers and isn't hidden from the Session Browser. Not Announcing. accessLevel: {w.AccessLevel}, hidden:{w.HideFromListing}");
					continue;
				}

				// Get users in a session and put them in a comma separated string
				List<FrooxEngine.User> users = new();
				String userlist = "";
				w.GetUsers(users);
				foreach (FrooxEngine.User user in users) {
					// if the user is a headless, do not list
					if (user.HeadDevice != HeadOutputDevice.Headless) {
						userlist+=$"{user.UserID},";
					}
				}
				// format for the AD Session Browser POST request
				var form = new ADSession
				{
					{"oper","push"},
					{"key", key},
					{"session", w.SessionId},
					{"communityname", Community},
					{"communitydiscord", Discord},
					{"communitylogo", Logo},
					{"sessionpreview", ad["PreviewUri"]},
					{"sessionname", w.Name},
					{"sessionhost", Engine.Current.LocalUserName},
					{"usercount", $"{w.UserCount-1}/{w.MaxUsers-1}"},
					{"userlist", userlist}
				};
				String requestUri = (Config?.GetValue(Server)) ?? "";

				// Convert the form to URLEncoded and send the ping
				HttpContent content = new FormUrlEncodedContent(form);
				try {
					Task<HttpResponseMessage> response = _client.PostAsync(requestUri, content);
					// Wait for response to check if successful and log the success or failure
					String StringResponse = await response.Result.Content.ReadAsStringAsync();
					if (!response.Result.IsSuccessStatusCode) {
						Error($"Announcing {w.SessionId} was unsuccessful at {DateTime.Now}");
					}
				} catch (Exception ex) {
					// if connection has an exception for any reason the announce has failed
					Error($"Announcing unsuccessful at {DateTime.Now}");
				}
			}
			// Sleep for 30s until the next time to ping
			Thread.Sleep(30000);
		}
		Msg("Ending Announce Thread");
	}	
}