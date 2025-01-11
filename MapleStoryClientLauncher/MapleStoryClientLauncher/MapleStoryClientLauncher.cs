﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace MapleStoryClientLauncher
{
    public partial class MapleStoryClientLauncher : Form
    {
        private string settingsFile = "maplestory_server_settings.txt";
        private string defaultIP = "127.0.0.1";
        private string defaultPort = "8484";
        
        public MapleStoryClientLauncher()
        {
            InitializeComponent();
        }

        private void MapleStoryClientLauncher_Load(object sender, EventArgs e)
        {
            // Check for admin privileges
            if (!IsAdministrator())
            {
                MessageBox.Show("This program requires administrator privileges.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            // Load settings
            LoadSettings();
            
            // Set default values
            txtIP.Text = defaultIP;
            txtPort.Text = defaultPort;
        }

        private bool IsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        private void LoadSettings()
        {
            if (File.Exists(settingsFile))
            {
                try
                {
                    var settings = File.ReadAllText(settingsFile).Split(':');
                    if (settings.Length == 2)
                    {
                        defaultIP = settings[0];
                        defaultPort = settings[1];
                    }
                }
                catch
                {
                    MessageBox.Show("Error reading settings file. Using defaults.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void SaveSettings(string ip, string port)
        {
            try
            {
                File.WriteAllText(settingsFile, $"{ip}:{port}");
            }
            catch
            {
                MessageBox.Show("Error saving settings file.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ModifyHostsFile(string ip)
        {
            string hostsPath = Path.Combine(Environment.SystemDirectory, "drivers\\etc\\hosts");
            string hostEntry = $"{ip} tw.login.maplestory.gamania.com";
            
            try
            {
                var lines = File.ReadAllLines(hostsPath)
                    .Where(line => !line.Contains("tw.login.maplestory.gamania.com"))
                    .ToList();
                
                lines.Add(hostEntry);
                File.WriteAllLines(hostsPath, lines.ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error modifying hosts file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            string ip = txtIP.Text;
            string ipResolved = ip;

            string port = txtPort.Text;
            int portNum;

            // Validate IP or domain
            if (!System.Text.RegularExpressions.Regex.IsMatch(ip, @"^((\d{1,3}(\.\d{1,3}){3})|([a-zA-Z0-9\-]+(\.[a-zA-Z0-9\-]+)+))$"))
            {
                MessageBox.Show("Invalid server address. Must be a valid IP or domain name.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Resolve domain to IP if needed
            try
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(ip, @"^\d{1,3}(\.\d{1,3}){3}$"))
                {
                    var addresses = System.Net.Dns.GetHostAddresses(ip);
                    if (addresses.Length == 0)
                    {
                        MessageBox.Show("Could not resolve domain name.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    ipResolved = addresses[0].ToString();
                }
            }
            catch
            {
                MessageBox.Show("Error resolving domain name.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Validate Port
            if (!int.TryParse(port, out portNum) || portNum < 1 || portNum > 65535)
            {
                MessageBox.Show("Invalid port number. Must be between 1 and 65535.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Save settings
            SaveSettings(ip, port);

            // Modify hosts file
            ModifyHostsFile(ipResolved);

            // Launch MapleStory
            try
            {
                string exePath = Path.Combine(Application.StartupPath, "MapleStory.exe");
                Process.Start(exePath, $"{ip} {port}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start MapleStory: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
