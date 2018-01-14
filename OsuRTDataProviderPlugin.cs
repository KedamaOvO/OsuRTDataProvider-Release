﻿using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using Sync;
using Sync.Command;
using Sync.Plugins;
using Sync.Tools;
using System;
using static OsuRTDataProvider.Listen.OsuListenerManager;

namespace OsuRTDataProvider
{
    [SyncPluginID("7216787b-507b-4eef-96fb-e993722acf2e", VERSION)]
    public class OsuRTDataProviderPlugin : Plugin
    {
        public const string PLUGIN_NAME = "OsuRTDataProvider";
        public const string PLUGIN_AUTHOR = "KedamaOvO";
        public const string VERSION = "1.1.1";

        private OsuListenerManager[] m_listener_managers = new OsuListenerManager[16];
        private int m_listener_managers_count = 0;

        /// <summary>
        /// If EnableTourneyMode = false in config.ini, return 0.
        /// If EnableTourneyMode = true in config.ini, return TeamSize * 2.
        /// </summary>
        public int TourneyListenerManagersCount { get => Setting.EnableTourneyMode ? m_listener_managers_count : 0; }

        /// <summary>
        /// return a ListenerManager.
        /// </summary>
        public OsuListenerManager ListenerManager { get => m_listener_managers[0]; }

        /// <summary>
        /// If EnableTourneyMode = false in config.ini, return null.
        /// If EnableTourneyMode = true in config.ini, return all ListenerManagers.
        /// </summary>
        public OsuListenerManager[] TourneyListenerManagers { get => Setting.EnableTourneyMode ? m_listener_managers : null; }

        public OsuRTDataProviderPlugin() : base(PLUGIN_NAME, PLUGIN_AUTHOR)
        {
            I18n.Instance.ApplyLanguage(new DefaultLanguage());
            base.EventBus.BindEvent<PluginEvents.InitCommandEvent>(InitCommand);
        }

        public override void OnEnable()
        {
            Setting.PluginInstance = this;
            Sync.Tools.IO.CurrentIO.WriteColor(PLUGIN_NAME + " By " + PLUGIN_AUTHOR, ConsoleColor.DarkCyan);

            if (Setting.EnableTourneyMode)
            {
                m_listener_managers_count = Setting.TeamSize * 2;
                for (int i = 0; i < m_listener_managers_count; i++)
                    InitTourneyManager(i);
            }
            else
            {
                InitManager();
            }

            DebugOutput(Setting.DebugMode, true);
        }

        private void InitTourneyManager(int id)
        {
            m_listener_managers[id] = new OsuListenerManager(true, id);
            m_listener_managers[id].Start();
        }

        private void InitManager()
        {
            m_listener_managers[0] = new OsuListenerManager();
            m_listener_managers[0].Start();
        }

        private void InitCommand(PluginEvents.InitCommandEvent @e)
        {
            @e.Commands.Dispatch.bind("ortdp",(args)=>
            {
                if (args.Count >= 2)
                {
                    if (args[0] == "debug")
                    {
                        if (bool.TryParse(args[1], out bool f))
                        {
                            DebugOutput(f);
                            Sync.Tools.IO.CurrentIO.WriteColor($"Debug mode = {Setting.DebugMode}", ConsoleColor.Green);
                        }
                    }
                    return true;
                }
                return false;
            },"OsuRTDataProvider control panel");
        }



        private void DebugOutput(bool enable,bool first=false)
        {
            if (!first&&Setting.DebugMode == enable) return;

            if(Setting.EnableTourneyMode)
            {
                for (int i = 0; i < TourneyListenerManagersCount; i++)
                {
                    void OnTourneyStatusChanged(OsuStatus l, OsuStatus c) =>
                        IO.FileLogger.WriteColor($"[OsuRTDataProvider][{i}]Current Game Status:{c}", ConsoleColor.Blue);
                    void OnTourneyModsChanged(ModsInfo m) =>
                        IO.FileLogger.WriteColor($"[OsuRTDataProvider][{i}]Mods:{m}(0x{(uint)m.Mod:X8})", ConsoleColor.Blue);
                    if (enable)
                    {
                        m_listener_managers[i].OnStatusChanged += OnTourneyStatusChanged;
                        m_listener_managers[i].OnModsChanged += OnTourneyModsChanged;
                    }
                    else
                    {
                        m_listener_managers[i].OnStatusChanged -= OnTourneyStatusChanged;
                        m_listener_managers[i].OnModsChanged -= OnTourneyModsChanged;
                    }
                }
            }
            else
            {
                void OnStatusChanged(OsuStatus l, OsuStatus c) =>
                    IO.CurrentIO.WriteColor($"[OsuRTDataProvider]Current Game Status:{c}", ConsoleColor.Blue);
                void OnModsChanged(ModsInfo m) =>
                    IO.CurrentIO.WriteColor($"[OsuRTDataProvider]Mods:{m}(0x{(uint)m.Mod:X8})", ConsoleColor.Blue);

                if(enable)
                {
                    m_listener_managers[0].OnStatusChanged += OnStatusChanged;
                    m_listener_managers[0].OnModsChanged += OnModsChanged;
                }
                else
                {
                    m_listener_managers[0].OnStatusChanged -= OnStatusChanged;
                    m_listener_managers[0].OnModsChanged -= OnModsChanged;
                }
            }

            Setting.DebugMode = enable;
        }
    }
}