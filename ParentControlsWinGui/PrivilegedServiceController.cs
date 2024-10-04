using System;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Eventing.Reader;

namespace ParentControlsWinGui
{
    internal class PrivilegedServiceController
    {
        private static readonly string privilegedServiceName = "ParentControlsWinServiceWireguard";

        public PrivilegedServiceController() { }

        // probably make private at some point
        public static async Task<string> SendMessageAsync(string message)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", "parent_controls_win_pipe", PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    // await client.ConnectAsync(8000);
                    client.Connect(8000);
                    string response = "";

                    using (var reader = new StreamReader(client))
                    using (var writer = new StreamWriter(client) { AutoFlush = true })
                    {
                        //await writer.WriteLineAsync(message);
                        //response = await reader.ReadLineAsync();
                        writer.WriteLine(message);
                        response = reader.ReadLine();
                    }

                    if (response == null)
                    {
                        return "";
                    }
                    else
                    {
                        return response;
                    }
                }
            }
             catch (Exception e) {
                MessageBox.Show("Error Sending message to pipe: " + e.Message);
                return "";
            }
        }

        public bool IsServiceRunning()
        {
            // Add back after implementing service
            try
            {
                using (ServiceController sc = new ServiceController(privilegedServiceName))
                {
                    // Check the status of the service
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        //Console.WriteLine($"The service '{serviceName}' is currently running.");
                        return true;
                    }
                    else
                    {
                        //Console.WriteLine($"The service '{serviceName}' is not running. Current status: {sc.Status}");
                        return false;
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                return false;
                // Console.WriteLine($"Error: The service '{serviceName}' does not exist. {ex.Message}");
            }
            catch (Exception ex)
            {
                return false;
                // Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

    }
}
