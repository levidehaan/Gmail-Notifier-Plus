﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using Microsoft.Win32;

namespace GmailNotifierPlus {

	[DataContract][Flags]
	public enum SoundNotification {
		[EnumMember(Value="0")]
		None = 0,

		[EnumMember(Value="1")]
		Default = 1,

		[EnumMember(Value="2")]
		Custom = 2
	}

	[DataContract(Name = "config")]
	public class Config {

		private static readonly String _appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		private static readonly String _path = Path.Combine(_appData, GmailNotifierPlus.Resources.WindowTitle);
		private static readonly String _fileName = "app.config";

		private String _language;

		public event ConfigSavedEventHandler Saved;
		public event LanguageChangedEventHandler LanguageChanged;

		public Config() {
			this.Interval = 60;
			this.Language = "en-us";
			this.Sound = "default";
			this.FirstRun = true;
			this.FlashCount = 4;
			this.FlashTaskbar = true;

			this.Accounts = new AccountList();
		}

		public static void InitDefaults(Config config) {
			config.FirstRun = true;
			config.FlashCount = 4;
		}

		public static void Init() {
			if (!Directory.Exists(_path)) {
				Directory.CreateDirectory(_path);
			}

			Config config = new Config();
			String xml = null;
			FileInfo file = new FileInfo(Path.Combine(_path, _fileName));

			if (file.Exists) {
				using (StreamReader sr = file.OpenText()) {
					xml = sr.ReadToEnd();
				}

				if (!String.IsNullOrEmpty(xml)) {
					config = Utilities.Serializer.DeserializeContract<Config>(xml);
				}
			}
			else {
				InitDefaults(config);
				config.Save();
			}

			config.Language = config.Language;

			Config.Current = config;

			// Fire up the locale information and init localization
			Dictionary<String, String> locales = Utilities.ResourceHelper.AvailableLocales;

			LoadLocale(config);

			for (var i = 0; i < config.Accounts.Count; i++) {
				config.Accounts[i].Init();
			}

			// Check to see if recent document tracking is turned on.
			// If it isn't we cannot add custom categories to the jumplist.

			try {
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", false)) {
					object trackDocs = key.GetValue("Start_TrackDocs"); //DWORD value. 
					if (trackDocs != null) {
						int tracking = Convert.ToInt32(trackDocs);
						config.RecentDocsTracked = tracking == 1;
					}
					key.Close();
				}
			}
			catch (System.Security.SecurityException) {
			}


		}
	
		public static Config Current {
			get;
			private set;
		}

		[DataMember(Name = "Pinned")]
		public Boolean Pinned { get; set; }

		[DataMember(Name = "interval")]
		public int Interval { get; set; }

		[DataMember(Name = "language")]
		public String Language {
			get { return _language; }
			set {
				String previous = _language;

				_language = value;

				if (previous != _language && LanguageChanged != null) {
					Config.LoadLocale(this);
					LanguageChanged(this);
				}
			}
		}

		[DataMember(Name = "sound")]
		public String Sound { get; set; }

		[DataMember(Name = "soundnotification")]
		public SoundNotification SoundNotification { get; set; }

		[DataMember(Name = "accounts")]
		public AccountList Accounts { get; set; }

		[DataMember(Name = "firstrun")]
		public Boolean FirstRun { get; set; }

		[DataMember(Name = "flashtaskbar")]
		public Boolean FlashTaskbar { get; set; }

		[DataMember(Name = "flashcount")]
		public int FlashCount { get; set; }

		[DataMember(Name = "showtrayicon")]
		public Boolean ShowTrayIcon { get; set; }

		[DataMember(Name = "showtoast")]
		public Boolean ShowToast { get; set; }

		[DataMember(Name = "checkupdates")]
		public Boolean CheckForUpdates { get; set; }
	
		public Boolean RecentDocsTracked { get; private set; }

		public void Save() {

			String serialized = Utilities.Serializer.SerializeContract<Config>(this);

			using (FileStream fs = new FileStream(Path.Combine(_path, _fileName), FileMode.Create, FileAccess.ReadWrite)) {
				using (StreamWriter sw = new StreamWriter(fs)) {
					sw.Write(serialized);
				}
			}

			if (this.Saved != null) {
				this.Saved(this, EventArgs.Empty);
			}

		}

		private static void LoadLocale(Config config) {
			Locale locale = Utilities.ResourceHelper.GetLocale(config.Language);

			// in case the config xml gets hosed, or a user goes a-tamperin'
			if (locale == null) {
				locale = Utilities.ResourceHelper.GetLocale("en-US");
			}

			locale.Init();
		}
	}

}
