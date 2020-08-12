﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Collections.Specialized;
using NLog;
using System.IO;

namespace BgAssist
{
    public partial class BgAssist : Form
    {
        //Alow form to close flag
        public bool AllowClose { get; set; }

        public BgAssist()
        {
            InitializeComponent();
            TrayMenuContext();
        }

        //Setup system tray right-click icon menu
        private void TrayMenuContext()
        {
            this.notifyIcon1.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            this.notifyIcon1.ContextMenuStrip.Items.Add("Refresh", null, this.btnRefresh_Click);
            this.notifyIcon1.ContextMenuStrip.Items.Add("View Log", null, this.btnViewLog_Click);
            this.notifyIcon1.ContextMenuStrip.Items.Add("Exit", null, this.MenuExit_Click);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Fire up logging
            Logger logger = LogManager.GetLogger("fileLogger");
            logger.Info("BgAssist started.");

            //Register event listeners
            SystemEvents.DisplaySettingsChanging += SystemEvents_DisplaySettingsChanging;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            //Load configuration; validate BGinfo64.exe and .bgi configuration file
            string bginfoArgs = ConfigurationManager.AppSettings.Get("BGinfoArgs");

            string configPath = Directory.GetCurrentDirectory() + "\\BgAssist-Config.exe";

            Configuration config = ConfigurationManager.OpenExeConfiguration(configPath);
            string bginfoPath = config.AppSettings.Settings["BgInfoPath"].Value;


            try
            {
                if (File.Exists(bginfoPath))
                {
                    logger.Info("BGinfo64 executable found in configured path.");

                    //Set form text field value
                    txtBginfoPath.Text = bginfoPath;
                } else
                {
                    logger.Info("BGinfo64 executable not found in configured path!");
                    txtBginfoPath.Text = "Not Found!";
                }
            } catch (Exception ex)
            {
                logger.Error(ex, "Error locating Bginfo64.exe in configured path! ");
            }

            //Refresh the wallpaper
            logger.Info("Wallpaper refreshed on startup.");
            RefreshWallpaper();

            txtBginfoArgs.Text = buildBginfoArguments();
        }


        static string buildBginfoArguments()
        {
            string configPath = Directory.GetCurrentDirectory() + "\\BgAssist-Config.exe";
            Configuration config = ConfigurationManager.OpenExeConfiguration(configPath);

            string bginfoConfig = config.AppSettings.Settings["BgInfoConfigPath"].Value;

            var builder = new StringBuilder();
            
            string bginfoTimer = config.AppSettings.Settings["BgInfoTimer"].Value;
            builder.Append(bginfoConfig + " /timer:" + bginfoTimer);

            if (Convert.ToBoolean(config.AppSettings.Settings["BgInfoPopup"].Value))
            {
                builder.Append(" /popup");
            }

            if (Convert.ToBoolean(config.AppSettings.Settings["BgInfoSilent"].Value))
            {
                builder.Append(" /silent");
            }

            if (Convert.ToBoolean(config.AppSettings.Settings["BgInfoTaskbar"].Value))
            {
                builder.Append(" /taskbar");
            }

            if (Convert.ToBoolean(config.AppSettings.Settings["BgInfoLog"].Value))
            {
                builder.Append(" /log");
            }

            if (Convert.ToBoolean(config.AppSettings.Settings["BgInfoRTF"].Value))
            {
                builder.Append(" /rtf");
            }

            return builder.ToString();
        }

        static void SystemEvents_DisplaySettingsChanging(object sender, EventArgs e)
        {
            //Nothing to do here, but could be handy
        }

        static void RefreshWallpaper()
        {
            Logger logger = LogManager.GetLogger("fileLogger");

            try
            {
                string configPath = Directory.GetCurrentDirectory() + "\\BgAssist-Config.exe";
                Configuration config = ConfigurationManager.OpenExeConfiguration(configPath);

                string bginfoPath = config.AppSettings.Settings["BgInfoPath"].Value;

                if (File.Exists(bginfoPath))
                { 
                    logger.Debug(buildBginfoArguments());

                    Process process = new Process();
                    process.StartInfo.FileName = bginfoPath;
                    process.StartInfo.Arguments = buildBginfoArguments();
                    process.Start();
                }
                else
                {
                    logger.Error("Wallpaper refresh couldn't be completed due to missing BGinfo executable.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error refreshing wallpaper!");
            }

            //RAM usage grew by .1 MB per refresh in testing; requesting garbage collection to keep memory footprint low
            //https://stackoverflow.com/questions/1852929/can-i-force-memory-cleanup-in-c
            System.GC.Collect();
        }

        static void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            Logger logger = LogManager.GetLogger("fileLogger");
            logger.Info("Display settings change triggered wallpaper refresh.");

            RefreshWallpaper();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            Logger logger = LogManager.GetLogger("fileLogger");
            logger.Info("User manually triggered wallpaper refresh.");

            RefreshWallpaper();
        }

        private void BgAssist_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Prevents form from closing
            if (!AllowClose)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
        }

        private void btnViewLog_Click(object sender, EventArgs e)
        {
            Logger logger = LogManager.GetLogger("fileLogger");

            try
            {
                string logPath = Environment.GetEnvironmentVariable("LocalAppData") + "\\WallpaperRefresher-Log.txt";
                Process.Start("notepad.exe",logPath);
            } catch (Exception ex)
            {
                logger.Error(ex, "Error opening log file! ");
            }
        }

        private void BgAssist_Shown(object sender, EventArgs e)
        {
            Hide();
        }

        void MenuExit_Click(object sender, EventArgs e)
        {
            //If form close is sent from system tray Exit menu item, allow form to close
            Logger logger = LogManager.GetLogger("fileLogger");
            logger.Info("BgAssist closed by user.");
            this.AllowClose = true;
            Application.Exit();
        }
    }
}
