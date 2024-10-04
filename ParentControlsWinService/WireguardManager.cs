using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ParentControlsWinService
{
    internal class WireguardManager
    {
        private static readonly string userDirectory = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Config");
        private static readonly string configFile = Path.Combine(userDirectory, "wg0.conf");
        private ServiceLoginManager loginManager;
        private volatile bool wg_started;

        private readonly object _initializeManager = new object();
        // no startManager lock, since it's an async call
        private readonly object _startStopManager = new object();

        public WireguardManager(ServiceLoginManager login_manager_in)
        {
            lock (_initializeManager)
            {
                makeConfigDirectory();

                this.loginManager = login_manager_in;
                this.wg_started = false;
            }
        }

        private void makeConfigDirectory()
        {
            try
            {
                DirectoryInfo dirInfo = Directory.CreateDirectory(userDirectory);
                DirectorySecurity ds = new DirectorySecurity();
                // Set the security descriptor
                ds.SetSecurityDescriptorSddlForm("O:BAG:BAD:PAI(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)");
                dirInfo.SetAccessControl(ds);
            }
            catch (UnauthorizedAccessException ex)
            {
                ParentControlsService.SaveToLog($"Access denied: {ex.Message}");
            }
        }

        public async Task<bool> StartWireguardService()
        {
            // kill if already running
            //if (this.wg_started)
            //{
            //    return false;
            //}

            String config;
            this.wg_started = true;

            try
            {
                config = this.loginManager.getClientConfig();
                // ParentControlsService.SaveToLog(config);

                await File.WriteAllBytesAsync(configFile, Encoding.UTF8.GetBytes(config));

                await Task.Run(() => Tunnel.Service.Add(configFile, true));
            }
            catch (Exception ex)
            {
                try { File.Delete(configFile); } catch { }

                return false;
            }

            return true;
        }

        public void StopWireguardService()
        {
            lock (_startStopManager)
            {
                ParentControlsService.SaveToLog("StopWireguardService started");
                if (!wg_started)
                {
                    // service isn't started, nothing to stop
                    return;
                }

                try
                {
                    ParentControlsService.SaveToLog("StopWireguardService removing old tunnel");
                    Tunnel.Service.Remove(configFile, true);
                }
                catch
                {
                    // do nothing
                    ParentControlsService.SaveToLog("Failed to remove Tunnel with Tunnel.Service.Remove(configFile, true)");
                }

                // wg shut down, this could all be in a mutex btw
                wg_started = false;
                ParentControlsService.SaveToLog("StopWireguardService finished");
            }
        }

        public bool IsWireguardRunning()
        {
            return this.wg_started;
        }
    }
}
