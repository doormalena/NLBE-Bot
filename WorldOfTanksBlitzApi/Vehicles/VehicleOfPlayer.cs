using JsonObjectConverter;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace FMWOTB.Vehicles
{
    public class VehicleOfPlayer
    {
        public long account_id { get; private set; }
        public TimeSpan battle_life_time { get; private set; }
        public DateTime in_garage_updated { get; private set; }
        public DateTime last_battle_time { get; private set; }
        public MasteryBadge mark_of_mastery { get; private set; }
        public short max_frags { get; private set; }
        public int max_xp { get; private set; }
        public int tank_id { get; private set; }
        public long frags { get; private set; }
        public bool in_garage { get; private set; }
        public VehicleStatistics VehicleStatistics { get; private set; }

        public VehicleOfPlayer(Json json)
        {
            setValues(json);
        }
        private void setValues(Json helper)
        {
            if (helper.tupleList != null)
            {
                Parallel.For(0, helper.tupleList.Count, intCounter =>
                {
                    string temp = helper.tupleList[intCounter].Item2.Item1.Trim(' ').Trim('\"');
                    if (!temp.ToLower().Equals("null"))
                    {
                        string item1 = helper.tupleList[intCounter].Item1.Trim(' ').Trim('\"');
                        var property = this.GetType().GetProperty(item1);
                        if (item1.StartsWith("[") || item1.StartsWith("{"))
                        {
                            var valueToSet = Json.convertStringToList(item1, property.PropertyType);
                            object instance = Activator.CreateInstance(property.PropertyType);
                            IList list = (IList)instance;
                            foreach (object item in valueToSet)
                            {
                                list.Add(item);
                            }
                            property.SetValue(this, list, null);
                        }
                        else if (property.PropertyType == typeof(DateTime))
                        {
                            if (property.GetValue(this) == null)
                            {
                                property.SetValue(this, new DateTime());
                            }
                            var valueToSet = Json.convertStringToDateTime(temp);
                            property.SetValue(this, valueToSet);
                        }
                        else if (property.PropertyType == typeof(TimeSpan))
                        {
                            if (property.GetValue(this) == null)
                            {
                                property.SetValue(this, new TimeSpan());
                            }
                            var valueToSet = TimeSpan.FromMilliseconds(double.Parse(temp));
                            property.SetValue(this, valueToSet);
                        }
                        else if (property.Name == "mark_of_mastery")
                        {
                            this.mark_of_mastery = (MasteryBadge)Enum.GetValues(typeof(MasteryBadge)).GetValue(Int32.Parse(temp));
                        }
                        else
                        {
                            var valueToSet = Json.convertStringToType(temp, property.PropertyType);
                            property.SetValue(this, valueToSet);
                        }
                    }
                });
                //foreach (var tuple in helper.tupleList)
                //{
                //    string temp = tuple.Item2.Item1.Trim(' ').Trim('\"');
                //    if (!temp.ToLower().Equals("null"))
                //    {
                //        string item1 = tuple.Item1.Trim(' ').Trim('\"');
                //        var property = this.GetType().GetProperty(item1);
                //        if (item1.StartsWith("[") || item1.StartsWith("{"))
                //        {
                //            var valueToSet = Json.convertStringToList(item1, property.PropertyType);
                //            object instance = Activator.CreateInstance(property.PropertyType);
                //            IList list = (IList)instance;
                //            foreach (object item in valueToSet)
                //            {
                //                list.Add(item);
                //            }
                //            property.SetValue(this, list, null);
                //        }
                //        else if (property.PropertyType == typeof(DateTime))
                //        {
                //            if (property.GetValue(this) == null)
                //            {
                //                property.SetValue(this, new DateTime());
                //            }
                //            var valueToSet = Json.convertStringToDateTime(temp);
                //            property.SetValue(this, valueToSet);
                //        }
                //        else if (property.PropertyType == typeof(TimeSpan))
                //        {
                //            if (property.GetValue(this) == null)
                //            {
                //                property.SetValue(this, new TimeSpan());
                //            }
                //            var valueToSet = TimeSpan.FromMilliseconds(double.Parse(temp));
                //            property.SetValue(this, valueToSet);
                //        }
                //        else if (property.Name == "mark_of_mastery")
                //        {
                //            this.mark_of_mastery = (MasteryBadge)Enum.GetValues(typeof(MasteryBadge)).GetValue(Int32.Parse(temp));
                //        }
                //        else
                //        {
                //            var valueToSet = Json.convertStringToType(temp, property.PropertyType);
                //            property.SetValue(this, valueToSet);
                //        }
                //    }
                //}
            }
            if (helper.subJsons != null && helper.subJsons.Count > 0)
            {
                this.VehicleStatistics = new VehicleStatistics(helper.subJsons[0]);
            }
        }
    }
}
