using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParentControlsWinGui
{
    public class LoginData
    {
        public string request_type { get; set; }
        public UserData user { get; set; }
        public DeviceData device { get; set; }

        public class UserData
        {
            public string email { get; set; }
            public string password { get; set; }
        }

        public class DeviceData
        {
            public string device_name { get; set; }
            public LinkedList<String> child_accounts { get; set; }
        }
    }

    public class LoginTokenResponse
    {
        public string access_token { get; set; }
        public string renewal_token { get; set; }
    }

    public class GenericRequest
    {
        public string request_type { get; set;}
    }

    public class LoginManager
    {
        private String email;
        private String password;
        private String device_name;
        private LinkedList<String> child_accounts;

        private String access_token;
        private String renewal_token;

        public LoginManager() { }

        public bool LoadLoginTokens()
        {
            GenericRequest tokenRequestData = new GenericRequest
            {
                request_type = "verify_login"
            };

            string json = JsonConvert.SerializeObject(tokenRequestData);

            try
            {
                // run async function synchronously 
                json = PrivilegedServiceController.SendMessageAsync(json).Result;
            }
            catch (Exception ex) { }

            try
            {
                JObject jsonObjectData = JObject.Parse(json);

                this.access_token = jsonObjectData["access_token"].ToString();
                this.renewal_token = jsonObjectData["renewal_token"].ToString();
            } catch (Exception ex)
            {
                return false;
            }

            if (string.IsNullOrEmpty(this.access_token) || string.IsNullOrEmpty(this.renewal_token))
            {
                return false;
            }

            return true;
        }

        public bool CreateLoginTokens(string email, string password, string device_name, LinkedList<String> child_accounts)
        {
            this.email = email;
            this.password = password;
            this.device_name = device_name;
            this.child_accounts = child_accounts;

            var loginData = new LoginData
            {
                request_type = "create_login_tokens",
                user = new LoginData.UserData
                {
                    email = this.email,
                    password = this.password,
                },

                device = new LoginData.DeviceData
                {
                    device_name = this.device_name,
                    child_accounts = this.child_accounts
                }
            };

            // create json packet with user credentials, send to service
            string json = JsonConvert.SerializeObject(loginData);
            json = PrivilegedServiceController.SendMessageAsync(json).Result;

            return true;
        }

        public static async void StartWireguardServer()
        {
            // TODO this maybe shouldn't exist, consider doing this automatically?
            GenericRequest tokenRequestData = new GenericRequest
            {
                request_type = "restart_wireguard_server"
            };

            string json = JsonConvert.SerializeObject(tokenRequestData);
            json = await PrivilegedServiceController.SendMessageAsync(json);

            return;
        }

        public static async void SaveLocalCertificates()
        {
            GenericRequest certRequestData = new GenericRequest
            {
                request_type = "get_pub_certificates"
            };

            string json = JsonConvert.SerializeObject(certRequestData);
            json = await PrivilegedServiceController.SendMessageAsync(json); // Timeout happening here

            JObject jsonObjectData = JObject.Parse(json);

            string cert_content = jsonObjectData["root_cert"].ToString();

            string certificateName = "pcw-ca-cert.cer";
            // TODO certainly the wrong encoding scheme I'm guessing?
            //string cert_content = Encoding.ASCII.GetString(certCont);
            string certFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", certificateName);

            try
            {
                File.WriteAllText(certFilePath, cert_content);
                MessageBox.Show($"Certificates {certificateName} saved to path {certFilePath}.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured saving certificate {certificateName} to path {certFilePath}. {ex.Message}");
            }

            return;
        }
    }
}
