using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO.Pipes;
using System.Management;
using System.Security.Principal;
using System.Diagnostics;
using Microsoft.Extensions.FileSystemGlobbing;
using Serilog;
using System.Security.AccessControl;
using System.IO;
using System.Linq.Expressions;

namespace ParentControlsWinService
{
    public class CertificateResponse
    {
        public byte[] root_cert { get; set; }
    }

    public class ParentControlsService : BackgroundService
    {
        // neither class is memory safe, TODO
        volatile ServiceLoginManager _loginManager;
        volatile WireguardManager? wireguardmanager;
        private readonly object _runningWireguardLock = new object();
        private DateTime lastProcessedEventTime;
        public ParentControlsService(ServiceLoginManager loginManager)
        {
            this._loginManager = loginManager;

            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ParentControlsServiceLog.txt";

            Log.Logger = new LoggerConfiguration()
            .WriteTo.File(
                filepath,
                // rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 1 * 1024 * 1024, // 1 MB
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 5 // Keep the last 5 files
            ).CreateLogger();
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            ManagementEventWatcher? logonWatcher = null;
            ManagementEventWatcher? logoffWatcher = null;
            try {

                // run pipe listener
                Task.Run(() => ListenForPipeRequests(token));

                // TODO may be helpful to add lock and unlock event codes, i.e. 4800 and 4801
                // TODO I think I added 4778 and 4779 but maybe don't need them
                logonWatcher = new ManagementEventWatcher(new WqlEventQuery(@"SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_NTLogEvent' AND TargetInstance.Logfile = 'Security' AND TargetInstance.SourceName = 'Microsoft-Windows-Security-Auditing' AND (TargetInstance.EventCode = '4624' OR TargetInstance.EventCode = '4801' OR TargetInstance.EventCode = '4778')"));
                logonWatcher.EventArrived += (sender, e) => OnUserLogin(sender, e, token); // new EventArrivedEventHandler(OnUserLogin);
                logonWatcher.Start();
                lastProcessedEventTime = DateTime.MinValue;

                // Watch for user logoff events
                logoffWatcher = new ManagementEventWatcher(new WqlEventQuery(@"SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_NTLogEvent' AND TargetInstance.Logfile = 'Security' AND TargetInstance.SourceName = 'Microsoft-Windows-Security-Auditing' AND (TargetInstance.EventCode = '4634' OR TargetInstance.EventCode = '4647' OR TargetInstance.EventCode = '4779' OR TargetInstance.EventCode = '4800')"));
                logoffWatcher.EventArrived += new EventArrivedEventHandler(OnUserLogoff);
                logoffWatcher.Start();
                SaveToLog("Started logon and logoff watcher");

                // consider adding wireguard stop here?
                token.Register(() =>
                {
                    logonWatcher.Stop();
                    logonWatcher.Dispose();
                    logoffWatcher.Stop();
                    logoffWatcher.Dispose();
                });

                await Task.Delay(Timeout.Infinite, token);

                SaveToLog("Outside ExecuteAsync loop");
            }
            catch (ManagementException mex)
            {
                SaveToLog("ManagementException: " + mex.Message);
                // Handle ManagementException (e.g., invalid query)
            }
            catch (OperationCanceledException)
            {
                // Handle the operation cancellation gracefully
                SaveToLog("normal cancellation");
            }
            catch (Exception ex)
            {
                SaveToLog("Exception: " + ex.Message);
                // Handle other exceptions
            }
            finally
            {
                if (logonWatcher != null)
                {
                    logonWatcher.Dispose();
                }
                if (logoffWatcher != null)
                {
                    logoffWatcher.Dispose();
                }

                // has been cancelled, check if we need to remove wireguard interface
                if (wireguardmanager != null)
                {
                    wireguardmanager.StopWireguardService();
                }
            }
        }

        public override async Task StopAsync(CancellationToken token)
        {
            if (wireguardmanager != null)
            {
                // object created, must stop
                wireguardmanager.StopWireguardService();
            }
        }

        private async void OnUserLogin(object sender, EventArrivedEventArgs e, CancellationToken token)
        {
            string account_sid = null;
            try
            {
                ManagementBaseObject eventInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                string[] insertionStrings = (string[])eventInstance["InsertionStrings"];
                // insertionStrings[0] = Account group, i.e. S-1-5-18
                // insertionStrings[4] = Account SID, i.e. S-1-5-21-2458554291-791756082-4270351871-1001
                // insertionStrings[5] = Full account email

                account_sid = insertionStrings[4];
            }
            catch (Exception ex)
            {
                SaveToLog("Exception in OnUserLogin second event: " + ex.Message);
                // Handle other exceptions
            }

            // TODO check here if the user logging in should be filtered or not
            if (!this.IsCurrentUserInList(account_sid))
            {
                SaveToLog("OnUserLogin: Current user signin not on list");
                // if not in list, don't start wireguard service
                return;
            }

            DateTime currentEventTime = DateTime.Now;
            if ((currentEventTime - lastProcessedEventTime) > TimeSpan.FromSeconds(6))
            {
                lastProcessedEventTime = currentEventTime;
                SaveToLog("Current user in list, but skipping due to repeat login attempt");
                // won't try again if attempted within last 6 seconds
                return;
            }
            SaveToLog("OnUserLogin: Current user in list! Starting wireguard");

            // TODO should this be in a loop? Should I do any hosue cleaning here?
            // Start wireguard server
            //while (!InitializeWireguard() && !token.IsCancellationRequested)
            //{
            //    Task.Delay(10000); // 10 second wait to try again
            //}

            // really doesn't need to be an if block
            if (InitializeWireguard())
            {
                SaveToLog("Successfully ran InitializeWireguard");
            } else
            {
                SaveToLog("Error when running InitializeWireguard");
            }
        }

        private async void OnUserLogoff(object sender, EventArrivedEventArgs e)
        {
            SaveToLog("OnUserLogoff Started");
            string account_sid = null;
            try
            {
                ManagementBaseObject eventInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                string[] insertionStrings = (string[])eventInstance["InsertionStrings"];
                // insertionStrings[0] = Account SID, i.e. S-1-5-21-2458554291-791756082-4270351871-1001
                // insertionStrings[1] = Account SID we want

                if (NonsenseAccount(insertionStrings[0]))
                {
                    SaveToLog("OnUserLogoff: Account we can ignore, group: " + insertionStrings[0]);
                    return;
                }

                for (int i = 0; i < insertionStrings.Length; i++)
                {
                    SaveToLog($"InsertionStrings[{i}]: {insertionStrings[i]}");
                }

                account_sid = insertionStrings[1];
            }
            catch (Exception ex)
            {
                SaveToLog("Exception in OnUserLogoff second event: " + ex.Message);
                // Handle other exceptions
            }

            // TODO check here if the user logging in should be filtered or not
            try {
                if (!this.IsCurrentUserInList(account_sid) || this.wireguardmanager == null)
                {
                    SaveToLog("Current user (" + account_sid + ") logoff not on list or wireguardmanager isn't initialized.");
                    // if not in list, don't start wireguard service
                    return;
                }
                SaveToLog("OnUserLogoff: Current user in list! Stopping wireguard");
                this.wireguardmanager.StopWireguardService();
            }
            catch (Exception ex)
            {
                SaveToLog("Exception in OnUserLogoff: " + ex.Message);
            }
        }

        private bool NonsenseAccount(string group_id)
        {
            // TODO there's nothing more permanent than a temporary solution
            // https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dtyp/81d92bba-d22b-4a8c-908a-554ab29148ab
            return "S-1-5-90".Contains(group_id) || "S-1-5-96".Contains(group_id) || "S-1-5-84".Contains(group_id);


        }

        private async Task ListenForPipeRequests(CancellationToken token)
        {
            while (!token.IsCancellationRequested) {
                System.IO.Pipes.PipeSecurity security = CreatePipeSecurity();                

                try
                {
                    using (var server = NamedPipeServerStreamAcl.Create(
                        "parent_controls_win_pipe",
                        PipeDirection.InOut,
                        1, // maxNumberOfServerInstances
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous,
                        2048, // inBufferSize
                        2048, // outBufferSize
                        security)) // pipeSecurity
                    {
                        SaveToLog("created server in ListenForPipeRequests");

                        try
                        {
                            // await server.WaitForConnectionAsync(token);
                            server.WaitForConnection();

                            using (var reader = new StreamReader(server))
                            using (var writer = new StreamWriter(server) { AutoFlush = true })
                            {
                                string? message;
                                while ((message = reader.ReadLine()) != null)
                                {
                                    // can contain passwords, don't save
                                    // SaveToLog("pipe handling " + message);
                                    // Process the received message
                                    string response2 = HandleRequest(message);

                                    // SaveToLog("pipe response " + response2);
                                    // Optionally send a response
                                    writer.WriteLine(response2);
                                }
                            }

                        }
                        catch (OperationCanceledException ex)
                        {
                            // Handle cancellation
                            SaveToLog("Cancelation: " + ex.Message);
                        }
                        catch (IOException ioex)
                        {
                            // Handle pipe broken or other IO exceptions
                            SaveToLog("IO Exception: " + ioex.Message);
                        }
                        catch (Exception ex)
                        {
                            SaveToLog("Exception: " + ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SaveToLog("Pipe Error: " + ex.Message);
                }
            }
        }

        private PipeSecurity CreatePipeSecurity()
        {
            var pipeSecurity = new PipeSecurity();

            // Grant read/write access to everyone
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow));

            return pipeSecurity;
        }

        private string HandleRequest(string request)
        {
            JObject jsonObject;
            string request_type = "";

            try
            {
                jsonObject = JObject.Parse(request);
                request_type = jsonObject["request_type"].ToString().Trim();
            }
            catch (Exception ex)
            {
                SaveToLog("ERROR: Malformed json request_type: " + request + " request. " + ex.Message);
                // TODO will probably want to change when we have more case statements
                return "";
            }

            switch (request_type)
            {
                case "verify_login":
                    // make sure that 
                    if (this._loginManager.HasValidDeviceId())
                    {
                        SaveToLog("verify_login case statement" + this._loginManager.GetTokens() + " json");
                        // return tokens
                        return this._loginManager.GetTokens();
                    }

                    SaveToLog("verify_login Invalid save details, returning empty string");
                    return "";

                case "get_tokens":
                    SaveToLog("get_tokens case statement" + this._loginManager.GetTokens() + " json");
                    // return LoginTokenResponse in string form
                    return this._loginManager.GetTokens();

                // consider adding try/catch blocks for json object 
                case "create_login_tokens":
                    SaveToLog("create_login_tokens case statement");
                    string user_email = "";
                    string user_password = "";
                    string device_name = "";
                    LinkedList<string> childAccounts = new LinkedList<string>();

                    try
                    {
                        user_email = jsonObject["user"]["email"].ToString();
                        user_password = jsonObject["user"]["password"].ToString();
                        device_name = jsonObject["device"]["device_name"].ToString();
                        List<string> list = JsonConvert.DeserializeObject<List<string>>(jsonObject["device"]["child_accounts"].ToString());
                        childAccounts = new LinkedList<string>(list);
                    }
                    catch
                    {
                        SaveToLog("ERROR: create_login_tokens failed to read json, " + jsonObject.ToString());
                        return this._loginManager.GetTokens();
                    }

                    this._loginManager.SetChildAccounts(childAccounts);
                    bool SuccessfulLogin = this._loginManager.Login(user_email, user_password).Result;
                    if (SuccessfulLogin)
                    {
                        // don't try to create a device if it's borked
                        this._loginManager.CreateDevice(device_name);
                    }

                    // will return empty tokens if the login failed
                    return this._loginManager.GetTokens();

                case "restart_wireguard_server":
                    SaveToLog("restart_wireguard_server case statement");

                    try
                    {
                        this.wireguardmanager.StopWireguardService();
                    } catch (Exception ex)
                    {
                        SaveToLog("Failed to stop service. " + ex.Message);
                    }

                    InitializeWireguard();

                    SaveToLog("leaving start_wireguard_server case statement");

                    return this._loginManager.GetTokens();

                case "get_pub_certificates":
                    SaveToLog("get_pub_certificates case statement");

                    byte[] rootCert = this._loginManager.GetRootCertificates();

                    CertificateResponse certResponse = new CertificateResponse
                    {
                        root_cert = rootCert
                    };

                    return JsonConvert.SerializeObject(certResponse);

                default:
                    SaveToLog("Unknown request type " + request_type);
                    break;
            }

            SaveToLog("Out of switch-case: " + request);
            // Implement your request handling logic here
            return request;
        }

        public bool InitializeWireguard()
        {
            // so it can only connect once at a time
            // we sometimes get multiple wireguard startups at login
            if (Monitor.TryEnter(_runningWireguardLock))
            {

                try 
                {
                    int retryCounter = 0;
                    while (!this._loginManager.ConnectToWireguard().Result)
                    {
                        SaveToLog("Failed ConnectToWireguard in start_wireguard_server");
                        retryCounter++;

                        if (retryCounter > 3)
                        {
                            SaveToLog("Failed ConnectToWireguard in start_wireguard_server after " + retryCounter + " retrys");
                            return false;
                        }
                        // wait 1 second after failure
                        Task.Delay(1000);
                    }

                    if (this.wireguardmanager == null)
                    {
                        this.wireguardmanager = new WireguardManager(this._loginManager);
                    }

                    // startWireguardService has bool which will cancel repeat, may cause 
                    // token issues?
                    if (this.wireguardmanager.StartWireguardService().Result)
                    {
                        SaveToLog("Started wireguard service");
                        // Monitor.Exit(_startingWireguardLock);
                        return true;
                    }
                    else
                    {
                        SaveToLog("Failed to start wireguard");
                        // Monitor.Exit(_startingWireguardLock);
                        return false;
                    }
                }
                finally
                {
                    Monitor.Exit(_runningWireguardLock);
                }
            }

            // returns without error but didn't connect
            return true;
        }

        public static void SaveToLog(string Message)
        {
            Log.Information(Message);
        }

        public bool IsCurrentUserInList(String loggedInUserSid)
        {
            LinkedList<string> userAccounts = this._loginManager.GetChildAccounts();
            if (userAccounts == null)
            {
                SaveToLog("No assigned userAccounts");
                // TODO maybe should be true in case app is broken?
                return false;
            }

            string? loggedInUserName;
            try
            {
                SaveToLog("User SID: " + loggedInUserSid);
                loggedInUserName = ConvertSidToUsername(loggedInUserSid);
            }
            catch (Exception ex)
            {
                SaveToLog("Failed to get account from SID. Exception " + ex.Message);
                return false;
            }

            if (loggedInUserName == null)
            {
                SaveToLog("loggedInUserName is null when it shouldn't be.");
                return false;
            }

            string ss = "[";
            foreach (string s in userAccounts)
            {
                ss += s + ", ";
            }
            SaveToLog(ss + "] : " + loggedInUserName);

            // Check if the current user's username exists in the linked list
            return userAccounts.Contains(loggedInUserName);
        }

        private string ConvertSidToUsername(string sid)
        {
            try
            {
                SaveToLog("sid: "+sid);
                SecurityIdentifier securityIdentifier = new SecurityIdentifier(sid);
                NTAccount ntAccount = (NTAccount)securityIdentifier.Translate(typeof(NTAccount));
                return ntAccount.Value.Split('\\')[1]; // Assuming the format DOMAIN\Username
            }
            catch
            {
                // This whole function may not even be needed, as ntAccount is a system account
                return sid;
            }
        }
    }
}
