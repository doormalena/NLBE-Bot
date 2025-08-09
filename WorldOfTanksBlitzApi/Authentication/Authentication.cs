using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FMWOTB.Authentication
{
    public class Authentication
    {
        public string login_url { get; } = "https://api.worldoftanks.eu/wot/auth/login/?application_id=";
        public string logout_url { get; } = "https://api.worldoftanks.eu/wot/auth/logout/?application_id=";
        public string prolongate_url { get; } = "https://api.worldoftanks.eu/wot/auth/prolongate/?application_id=";
        public string access_token { get; private set; }
        public long account_id { get; private set; }
        public DateTime? expires_at { get; private set; }
        private Application application;

        public Authentication(Application application)
        {
            this.application = application;
        }
        public string getLogoutURL()
        {
            return logout_url + this.application.application_id;
        }
        public string getLoginURL()
        {
            return login_url + this.application.application_id;
        }
        public string getProlongateURL()
        {
            return prolongate_url + this.application.application_id;
        }
        public async Task<Tuple<string, string>> openIDLogin()
        {
            return await this.application.requestURL(new List<Tuple<string, string>>(), this.login_url);
        }
        public async Task<Tuple<string, string>> prolongateAccess(string access_token)
        {
            this.access_token = access_token;
            return await this.application.requestURL(getAccesTokenTuple(), this.prolongate_url);
        }
        private Tuple<string, string> getAccesTokenTuple()
        {
            return new Tuple<string, string>("access_token", this.access_token);
        }
    }
}
