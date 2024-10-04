using Microsoft.Extensions.Hosting;
using ParentControlsWinService;
using System.Diagnostics;
using System.ServiceProcess;

public class Program
{
    public static void Main(string[] args)
    {
        // TODO I don't understand this code but nothing works unless
        // I include it
        if (args.Length == 3 && args[0] == "/service")
        {
            ParentControlsService.SaveToLog("SERVICE: " + args[0] + " ; " + args[1] + " ; " + args[2]);
            var t = new Thread(() =>
            {
                try
                {
                    var currentProcess = Process.GetCurrentProcess();
                    var uiProcess = Process.GetProcessById(int.Parse(args[2]));
                    if (uiProcess.MainModule.FileName != currentProcess.MainModule.FileName)
                        return;
                    uiProcess.WaitForExit();
                    Tunnel.Service.Remove(args[1], false);
                }
                catch { }
            });
            //Console.Write("New thread is about to start\n\n");
            t.Start();
            Tunnel.Service.Run(args[1]);
            t.Interrupt();
            return;
        }

        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<ServiceLoginManager>();
                services.AddHostedService<ParentControlsService>();
            });

}
