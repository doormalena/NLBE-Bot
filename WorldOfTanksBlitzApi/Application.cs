using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FMWOTB.Exceptions;

namespace FMWOTB
{
    public class Application
    {
        public string application_id { get; }

        public Application(string application_id)
        {
            this.application_id = application_id;
        }
        public async Task<Tuple<string, string>> requestURL(Tuple<string, string> tuple, string requestString)
        {
            List<Tuple<string, string>> tupleList = new List<Tuple<string, string>>();
            tupleList.Add(tuple);
            return await requestURL(tupleList, requestString);
        }
        public async Task<Tuple<string, string>> requestURL(List<Tuple<string, string>> tupleList, string requestString)
        {
            if (tupleList.Count > 0)
            {
                if (!tupleList[0].Item1.ToLower().Equals("application_id"))
                {
                    tupleList.Insert(0, getApplicationTuple());
                }
            }
            else
            {
                tupleList.Add(getApplicationTuple());
            }
            HttpClient httpClient = new HttpClient();
            MultipartFormDataContent form1 = new MultipartFormDataContent();
            foreach (Tuple<string, string> tuple in tupleList)
            {
                form1.Add(new StringContent(tuple.Item2), tuple.Item1);
            }
            HttpResponseMessage response = await httpClient.PostAsync(requestString, form1);
            if ((int)response.StatusCode >= 500){
                throw new InternalServerErrorException();
            }
            return new Tuple<string, string>(response.Headers.Location.AbsoluteUri, await response.Content.ReadAsStringAsync());
        }
        private Tuple<string, string> getApplicationTuple()
        {//application_id
            return new Tuple<string, string>("application_id", this.application_id);
        }
    }
}
