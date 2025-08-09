using JsonObjectConverter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FMWOTB.Exceptions;

namespace FMWOTB.Tournament
{
    public class Tournaments
    {
        public string description { get; set; }
        public DateTime? end_at { get; set; }
        public DateTime? matches_start_at { get; set; }
        public DateTime? registration_end_at { get; set; }
        public DateTime? registration_start_at { get; set; }
        public DateTime? start_at { get; set; }
        public string status { get; set; }
        public string title { get; set; }
        public long tournament_id { get; set; }
        public Award award { get; set; }
        public Logo logo { get; set; }
        public Fee fee { get; set; }
        public Winner_award winner_award { get; set; }
        public Tournaments(Json json)
        {
            setValues(json);
        }
        private void setValues(Json helper)
        {
            if (helper.tupleList != null)
            {
                foreach (var tuple in helper.tupleList)
                {
                    string temp = tuple.Item2.Item1.Trim(' ').Trim('\"');
                    if (!temp.ToLower().Equals("null"))
                    {
                        string item1 = tuple.Item1.Trim(' ').Trim('\"');
                        var property = this.GetType().GetProperty(item1);
                        if (property != null)
                        {
                            object valueToSet;
                            if (item1.StartsWith("[") || item1.StartsWith("{"))
                            {
                                valueToSet = Json.convertStringToList(item1, property.PropertyType);
                            }
                            else if (property.PropertyType == typeof(DateTime?))
                            {
                                if (property.GetValue(this) == null)
                                {
                                    property.SetValue(this, new DateTime());
                                }
                                valueToSet = Json.convertStringToDateTime(temp);
                            }
                            else
                            {
                                valueToSet = Json.convertStringToType(temp, property.PropertyType);
                            }
                            property.SetValue(this, valueToSet);
                        }
                    }
                }
            }
            if (helper.subJsons != null)
            {
                foreach (Json subJson in helper.subJsons)
                {
                    try
                    {
                        switch (subJson.head.ToLower())
                        {
                            case "award": this.award = new Award(subJson); break;
                            case "logo": this.logo = new Logo(subJson); break;
                            case "fee": this.fee = new Fee(subJson); break;
                            case "winner_award": this.winner_award = new Winner_award(subJson); break;
                            default:
                                if (subJson.tupleList == null)
                                {
                                    var property = this.GetType().GetProperty(subJson.head);
                                    if (property != null)
                                    {
                                        string item1 = subJson.jsonText.Trim(' ').Trim('\"');
                                        var valueToSet = Json.convertStringToList(item1, property.PropertyType);
                                        object instance = Activator.CreateInstance(property.PropertyType);
                                        // List<T> implements the non-generic IList interface
                                        IList list = (IList)instance;
                                        foreach (object item in valueToSet)
                                        {
                                            list.Add(item);
                                        }
                                        property.SetValue(this, list, null);
                                    }
                                }
                                else
                                {
                                    var property = this.GetType().GetProperty(subJson.head.Trim(' ').Trim('\"'));
                                    if (property != null)
                                    {
                                        List<Tuple<string, string>> tupleList = new List<Tuple<string, string>>();
                                        foreach (var item in subJson.tupleList)
                                        {
                                            tupleList.Add(new Tuple<string, string>(item.Item1.Trim(' ').Trim('\"'), item.Item2.Item1.Trim(' ').Trim('\"')));
                                        }
                                        object instance = Activator.CreateInstance(property.PropertyType);
                                        // List<T> implements the non-generic IList interface
                                        IList list = (IList)instance;
                                        foreach (object item in tupleList)
                                        {
                                            list.Add(item);
                                        }
                                        property.SetValue(this, list, null);
                                    }
                                }
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        bool ok = true;
                    }
                    setValues(subJson);
                }
            }
        }
        public static async Task<string> tournamentsToString(string key)
        {
            string url = @"https://api.wotblitz.eu/wotb/tournaments/list/?application_id=" + key;
            HttpClient httpClient = new HttpClient();
            MultipartFormDataContent form1 = new MultipartFormDataContent();
            HttpResponseMessage response = await httpClient.PostAsync(url, form1);
            if ((int)response.StatusCode >= 500){
                throw new InternalServerErrorException();
            }
            return await response.Content.ReadAsStringAsync();
        }
    }
}
