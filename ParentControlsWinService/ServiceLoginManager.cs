using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Grpc.Net.Client;
using Grpc.Core;
using System.Diagnostics;
using Google.Protobuf;
using System.Net.Http.Headers;

namespace ParentControlsWinService
{
    public class LoginTokenResponse
    {
        public string access_token { get; set; }
        public string renewal_token { get; set; }
    }

    public class LoginData
    {
        public UserData user { get; set; }

        public class UserData
        {
            public string email { get; set; }
            public string password { get; set; }
        }
    }

    public class DeviceData
    {
        public DeviceDetails device { get; set; }

        public class DeviceDetails
        {
            public string name { get; set; }
        }
    }

    public class ServiceLoginManager
    {
        private String email;
        private String deviceId;
        private String access_token;
        private String renewal_token;
        private LinkedList<String> childAccounts;

        private String serverPubKey;
        private Int32 portNumber;
        private String serverIPAddr;
        private byte[] certificateFileCrt;

        private String clientPubKey;
        private String clientPrivKey;

        private IConfigurationRoot configuration;
        private readonly object _loginSaveLock = new object();

        public ServiceLoginManager()
        {
            try
            {
                // attempt to read in application configuration
                configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("application.json")
                    .Build();

                this.email = configuration["user_email"];
                this.deviceId = configuration["device_id"];
                this.access_token = configuration["access_token"];
                this.renewal_token = configuration["renewal_token"];
                List<string> list = JsonConvert.DeserializeObject<List<string>>(configuration["child_accounts"]);
                this.childAccounts = new LinkedList<string>(list);
            }
            catch
            {
                // No values are saved
                this.email = "";
                this.deviceId = "";
                this.childAccounts = new LinkedList<string>();
                this.access_token = null;
                this.renewal_token = null;
            }
        }

        public string GetTokens()
        {
            LoginTokenResponse loginTokenResponse;

            if (string.IsNullOrEmpty(this.access_token) || string.IsNullOrEmpty(this.renewal_token))
            {
                // return empty strings if no existing data
                loginTokenResponse = new LoginTokenResponse
                {
                    access_token = "",
                    renewal_token = ""
                };

                return JsonConvert.SerializeObject(loginTokenResponse);
            }

            loginTokenResponse = new LoginTokenResponse
            {
                access_token = this.access_token,
                renewal_token = this.renewal_token
            };

            return JsonConvert.SerializeObject(loginTokenResponse);
        }

        public bool HasValidDeviceId()
        {
            // ensures deviceId exists
            return this.deviceId != null && this.deviceId != "";
        }

        public void SetChildAccounts(LinkedList<string> childAccounts)
        {
            this.childAccounts = childAccounts;

            this.saveDeviceInfo();
        }

        public LinkedList<string> GetChildAccounts()
        {
            return this.childAccounts;
        }

        public async Task<bool> Login(string user_email, string user_password)
        {
            String login_api = "https://www.parentcontrols.win/api/v1/session";

            var loginData = new LoginData
            {
                user = new LoginData.UserData
                {
                    email = user_email,
                    password = user_password
                }
            };

            using (HttpClient client = new HttpClient())
            {
                string json = JsonConvert.SerializeObject(loginData);
                HttpContent jsonMessage = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(login_api, jsonMessage);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    //LoginTokenResponse tokenResponse = JsonConvert.DeserializeObject<LoginTokenResponse>(responseContent);
                    JObject jsonObject = JObject.Parse(responseContent);

                    this.access_token = jsonObject["data"]["access_token"].ToString();
                    this.renewal_token = jsonObject["data"]["renewal_token"].ToString();
                    this.email = user_email;
                }
                else
                {
                    //throw new Exception("Login failed");
                    return false;
                }
            }

            // save credentials
            this.saveDeviceInfo();

            return true;
        }

        public bool CreateDevice(string device_name)
        {
            if (this.access_token == null || this.renewal_token == null)
            {
                // must be logged in to make create device request
                return false;
            }

            String create_device_url = "https://www.parentcontrols.win/api/v1/devices";


            var deviceData = new DeviceData
            {
                device = new DeviceData.DeviceDetails
                {
                    name = device_name
                }
            };

            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, create_device_url);
                request.Headers.Add("Authorization", this.access_token);

                string json2 = JsonConvert.SerializeObject(deviceData);
                request.Content = new StringContent(json2, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = client.SendAsync(request).Result;
                }
                catch (Exception ex)
                {
                    // Console.WriteLine
                    ParentControlsService.SaveToLog("Exception occurred: " + ex.Message);
                    return false;
                }


                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    // Extract and return the access token and renewal token from responseContent
                    // Assuming the response is in the format {"access_token": "your_access_token", "renewal_token": "your_renewal_token"}
                    // You need to adjust this based on the actual response structure
                    JObject jsonObject = JObject.Parse(responseContent);

                    this.deviceId = jsonObject["data"]["id"].ToString();

                    // save device id before anything can go wrong lol
                    this.saveDeviceInfo();
                }
                else
                {
                    throw new Exception("Device creation failed");
                }
            }

            return true;
        }

        public async Task<(String, String)> RenewToken()
        {
            String renew_device_url = "https://www.parentcontrols.win/api/v1/session/renew";

            String cur_access_token = this.access_token;
            String cur_renewal_token = this.renewal_token;

            using (HttpClient client = new HttpClient())
            {

                var request = new HttpRequestMessage(HttpMethod.Post, renew_device_url);

                // Replace 'your_token_here' with the actual token
                request.Headers.Add("Authorization", this.renewal_token);

                HttpResponseMessage response;
                try
                {
                    response = client.SendAsync(request).Result;
                    // response = await client.SendAsync(request);
                }
                catch (Exception ex)
                {
                    ParentControlsService.SaveToLog("Exception occurred during renew SendAsync: " + ex.Message);
                    return (cur_access_token, cur_renewal_token);
                }

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Extract and return the access token and renewal token from responseContent
                    // Assuming the response is in the format {"access_token": "your_access_token", "renewal_token": "your_renewal_token"}
                    // You need to adjust this based on the actual response structure
                    JObject jsonObject = JObject.Parse(responseContent);

                    try
                    {
                        // success, so overwrite placeholder values
                        cur_access_token = jsonObject["data"]["access_token"].ToString();
                        cur_renewal_token = jsonObject["data"]["renewal_token"].ToString();
                    }
                    catch (Exception ex)
                    {
                        // should already have this assignment
                        cur_access_token = this.access_token;
                        cur_renewal_token = this.renewal_token;
                        ParentControlsService.SaveToLog("Error: Can't assign jsonObject, error: " + ex.Message);
                    }

                    if (!string.IsNullOrEmpty(cur_access_token) && !string.IsNullOrEmpty(cur_renewal_token))
                    {
                        ParentControlsService.SaveToLog("Setting new tokens: " + cur_access_token);
                        // we have content from json
                        this.access_token = cur_access_token;
                        this.renewal_token = cur_renewal_token;

                        // save tokens, saved through global variables
                        this.saveDeviceInfo();
                    }
                }
                else
                {
                    ParentControlsService.SaveToLog("Renew failed with status code: " + response.StatusCode);
                    return (null, null);
                }

            }

            return (this.access_token, this.renewal_token);
        }

        public void saveDeviceInfo()
        {
            // save credentials
            var jsonObj2 = new JObject
            {
                ["user_email"] = this.email,
                ["device_id"] = this.deviceId,
                ["access_token"] = this.access_token,
                ["renewal_token"] = this.renewal_token,
                ["child_accounts"] = JsonConvert.SerializeObject(this.childAccounts)
            };

            try
            {
                string appJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "application.json");

                // write to applicaiton.json for future loading
                string json_output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj2, Newtonsoft.Json.Formatting.Indented);

                // prevent multiple writes at once
                lock (_loginSaveLock)
                {
                    File.WriteAllText(appJsonPath, json_output);
                    try // Will fail if no admin rights
                    {
                        // Set read permissions for all users
                        FileInfo fileInfo = new FileInfo(appJsonPath);
                        FileSecurity fileSecurity = fileInfo.GetAccessControl();
                        fileSecurity.AddAccessRule(new FileSystemAccessRule(
                            new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                            FileSystemRights.ReadData,
                            InheritanceFlags.None,
                            PropagationFlags.NoPropagateInherit,
                            AccessControlType.Allow));
                        fileInfo.SetAccessControl(fileSecurity);
                    }
                    catch (Exception ex)
                    {
                        ParentControlsService.SaveToLog("Error failed to save login info, likely due to insufficient permissions. " + ex.Message);
                    }
                }

                // This now builds the configuration path I just saved
                this.configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("application.json")
                    .Build();

            }
            catch (Exception ex)
            {
                // Fallback to non-interactive handling, like logging
                ParentControlsService.SaveToLog("Error saving device information, may encoutner issues on future sign-in. " + ex.Message);
            }
        }

        ///////////////////////////// Wireguard Functions ////////////////////////////////////////
        ///  <summary>
        /// Could be in another class, but for simplicity I'm leaving them here
        /// </summary>
        /// <returns></returns>
        /// 

        public async Task<bool> ConnectToWireguard()
        {
            ParentControlsService.SaveToLog("Inside ConnectToWireguard");

            // generate keys
            var keyPair = Tunnel.Keypair.Generate();

            // I'll want to save these to a config file
            var publicKey = keyPair.Public;
            var privateKey = keyPair.Private;

            // generate keys
            this.clientPrivKey = keyPair.Private.ToString();
            this.clientPubKey = keyPair.Public.ToString();

            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };

            // var channel = GrpcChannel.ForAddress("https://155.138.242.76:50059", new GrpcChannelOptions
            var channel = GrpcChannel.ForAddress("https://vps.parentcontrols.win:50059", new GrpcChannelOptions
            {
                HttpHandler = httpHandler
            });

            // Create a client for the CreateConnection service
            var client = new CreateWGConnection.CreateWGConnectionClient(channel);

            // Prepare the request message
            var request = new ConnectionInit
            {
                Email = this.email,
                ClientPubKey = this.clientPubKey,
                DeviceId = this.deviceId,
                AcessToken = this.access_token
            };

            try
            {
                // Call the gRPC service to send the message and receive a response
                var response = await client.StartConnectionAsync(request);
                this.serverPubKey = response.ServerPubKey;
                this.portNumber = response.PortNumber;
                this.serverIPAddr = response.ServerIPAddr;
                this.certificateFileCrt = response.CertificateFileCrt.ToByteArray();

                // insert newly aquired certificate to root
                this.SaveRootCertificate();

            }
            catch (Exception ex)
            {
                // failure in something, still must prevent internet access
                ParentControlsService.SaveToLog("Error in ConnectToWireguard: " + ex.Message);

                return false;
            }

            // renew tokens but don't want on them
            // waiting to see if this fixes issues?
            await Task.Run(async () => await this.RenewToken());

            return true;
        }

        /* Example;
            [Interface]
            PrivateKey = AbzsxT8XDfIeBv72AukfTCttytb9BZ0lABB93BBfm/U=
            Address = 10.0.0.1/32
            DNS = 10.0.0.53

            [Peer]
            PublicKey = mehPR0K4Nl0gwOYxbLLt5hZUrqJsWw76UqwiuhH0J0I=
            AllowedIPs = 0.0.0.0/0
            Endpoint = 45.76.232.143:51820
        */
        public string getClientConfig()
        {
            // ParentControlsService.SaveToLog("getClientConfig");
            String interfaceConn = $"[Interface]{Environment.NewLine}PrivateKey = {this.clientPrivKey}{Environment.NewLine}Address = 10.0.0.1/24{Environment.NewLine}DNS = 10.0.0.53{Environment.NewLine}";
            String peerConn = $"[PEER]{Environment.NewLine}PublicKey = {this.serverPubKey}{Environment.NewLine}AllowedIPs = 0.0.0.0/0{Environment.NewLine}Endpoint = {this.serverIPAddr}:{this.portNumber}{Environment.NewLine}PersistentKeepalive = 30{Environment.NewLine}";

            ParentControlsService.SaveToLog(interfaceConn + peerConn);
            return interfaceConn + peerConn;
        }

        public byte[] GetRootCertificates() {
            return this.certificateFileCrt;
        }

        private bool SaveRootCertificate()
        {
            try
            {
                byte[] rootCertBytes = this.certificateFileCrt;

                // This will error out if they're not in the right format
                X509Certificate2 certificate = new X509Certificate2(rootCertBytes);
                X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);

                // adds the certificate and requires admin privileges
                store.Add(certificate);

                store.Close();
            }
            catch (Exception ex)
            {
                ParentControlsService.SaveToLog("Failed to save root certificate: " + ex.Message);
                return false;
            }

            return true;
        }

    } // class

} // namespace
