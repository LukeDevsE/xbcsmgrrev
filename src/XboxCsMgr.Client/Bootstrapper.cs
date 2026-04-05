using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stylet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using XboxCsMgr.Client.ViewModels;
using XboxCsMgr.Helpers.Win32;
using XboxCsMgr.XboxLive;
using XboxCsMgr.XboxLive.Model.Authentication;
using XboxCsMgr.XboxLive.Services;

namespace XboxCsMgr.Client
{
    public class AppBootstrapper : Bootstrapper<ShellViewModel>
    {
        public static XboxLiveConfig? XblConfig { get; internal set; }

        private AuthenticateService authenticateService;
        //private string DeviceToken { get; set; }
        //private string UserToken = "";
        public static string CLIENT_ID = "c36a9fb6-4f2a-41ff-90bd-ae7cc92031eb";
        protected override void ConfigureIoC(StyletIoC.IStyletIoCBuilder builder)
        {
            base.ConfigureIoC(builder);

            builder.Bind<IDialogFactory>().ToAbstractFactory();
        }
        // tenant set to common for now
        public static HttpClient devicecode = new HttpClient();
        protected override async void OnStart()
        {
            Debug.WriteLine("Start program");
            var RefToken = await GetRefreshToken();
            authenticateService = new AuthenticateService(XblConfig);
            if (RefToken != "")
            {
                HttpRequestMessage req3 = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/consumers/oauth2/v2.0/token");
                req3.Method = HttpMethod.Post;
                req3.Content = new StringContent($"client_id={CLIENT_ID}&scope=Xboxlive.signin%20Xboxlive.offline_access&grant_type=refresh_token&refresh_token={RefToken}", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"); // have fun anyone looking at this crappy code
                HttpResponseMessage refreshresponse = await devicecode.SendAsync(req3);
                string refreshedtoken = await refreshresponse.Content.ReadAsStringAsync();
                var TableRefToken = JObject.Parse(refreshedtoken);
                if (refreshresponse.IsSuccessStatusCode && TableRefToken != null && TableRefToken["access_token"] != null && TableRefToken["refresh_token"] != null)
                {
                    var finaltoken2 = await authenticateService.AuthenticateUser(TableRefToken["access_token"].ToString(), "d=");
                    var result2 = await authenticateService.AuthorizeXsts(finaltoken2.Token);
                    SaveRefreshToken(TableRefToken["refresh_token"].ToString());
                    if (result2 != null)
                    {
                        //Debug.WriteLine("Authorized! Token: " + result.Token);
                        XblConfig = new XboxLiveConfig(result2.Token, result2.DisplayClaims.XboxUserIdentity[0]);
                        this.RootViewModel.OnAuthComplete();
                    }
                    base.OnStart();
                    return;
                }
            }
            var dialog = new Dialogue();
            await Task.Yield();
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode");
            req.Method = HttpMethod.Post;
            req.Content = new StringContent($"client_id={CLIENT_ID}&scope=Xboxlive.signin%20Xboxlive.offline_access", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"); // have fun anyone looking at this crappy code
            HttpResponseMessage devicecoderesponse = await devicecode.SendAsync(req);
            string content = await devicecoderesponse.Content.ReadAsStringAsync();
            dynamic usercode = JsonConvert.DeserializeObject(content);
            dialog.usercodething = usercode["user_code"];
            dialog.txtQuestion.Text = "Open https://www.microsoft.com/link and enter the code " + usercode["user_code"];
            dialog.ShowDialog();
            //LoadXblTokenCredentials();
            HttpRequestMessage req2 = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/consumers/oauth2/v2.0/token");
            req2.Method = HttpMethod.Post;
            req2.Content = new StringContent($"grant_type=urn:ietf:params:oauth:grant-type:device_code&client_id={CLIENT_ID}&device_code={usercode["device_code"]}", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"); // have fun anyone looking at this crappy code
            HttpResponseMessage tokenresponse = await devicecode.SendAsync(req2);
            string content2 = await tokenresponse.Content.ReadAsStringAsync();
            dynamic tokencode = JsonConvert.DeserializeObject(content2);
            var finaltoken = await authenticateService.AuthenticateUser(tokencode["access_token"].ToString(), "d=");
            var result = await authenticateService.AuthorizeXsts(finaltoken.Token);
            SaveRefreshToken(tokencode["refresh_token"].ToString());
            if (result != null)
            {
                //Debug.WriteLine("Authorized! Token: " + result.Token);
                XblConfig = new XboxLiveConfig(result.Token, result.DisplayClaims.XboxUserIdentity[0]);
                this.RootViewModel.OnAuthComplete();
            }
            base.OnStart();
        }
        string AppDataxbcsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "xbcsmgrrev");
        private async Task<string> GetRefreshToken()
        {
            var filepath = Path.Combine(AppDataxbcsDir, "reftoken");
            if (File.Exists(filepath) && Directory.Exists(AppDataxbcsDir))
            {
                var readtoken = await File.ReadAllTextAsync(filepath);
                return readtoken;
            } 
            else
            {
                return "";
            }
            // you'll never know how ugly this code was before
        }
        private async void SaveRefreshToken(string token)
        {
            if (!Directory.Exists(AppDataxbcsDir))
            {
                Directory.CreateDirectory(AppDataxbcsDir);
            }
            await File.WriteAllTextAsync(Path.Combine(AppDataxbcsDir, "reftoken"), token); // maybe i could make some sort of cache system for the preview of the worlds in mc xb1.. hmmmm....
        }
        // nolonger needed
        /*
        private void LoadXblTokenCredentials()
        {
            // Lookup current Xbox Live authentication data stored via wincred
            Dictionary<string, string> currentCredentials = CredentialUtil.EnumerateCredentials();
            foreach (var cred in currentCredentials.Keys)
            {
                Debug.WriteLine(cred);
            }
            var xblCredentials = currentCredentials.Where(k => k.Key.Contains("Xbl|")
                    && k.Key.Contains("Dtoken") 
                    || k.Key.Contains("Utoken"))
                    .ToDictionary(p => p.Key, p => p.Value);

            foreach (var credential in xblCredentials)
            {
                // Remove trailing 'X' that is found on some credentials
                var fixedJson = credential.Value.TrimEnd('X').ToString();
                XboxLiveToken? token = JsonConvert.DeserializeObject<XboxLiveToken>(fixedJson);
                if (token.TokenData.NotAfter > DateTime.UtcNow)
                {
                    if (credential.Key.Contains("Dtoken"))
                    {
                        //DeviceToken = token.TokenData.Token;
                    }
                    else if (credential.Key.Contains("Utoken"))
                    {
                        //if (token.TokenData.Token != "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA") UserToken = token.TokenData.Token;
                    }
                }
            }
        }
        */
    }
}
