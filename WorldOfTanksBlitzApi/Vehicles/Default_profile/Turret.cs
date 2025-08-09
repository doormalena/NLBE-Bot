using JsonObjectConverter;
using System;

namespace FMWOTB.Vehicles.Default_profile
{
    public class Turret
    {
        public int hp { get; private set; }
        public string name { get; private set; }
        public int tier { get; private set; }
        public int traverse_left_arc { get; private set; }
        public int traverse_right_arc { get; private set; }
        public int traverse_speed { get; private set; }
        public int view_range { get; private set; }
        public int weight { get; private set; }

        public Turret(Json json)
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
                            var valueToSet = Json.convertStringToType(temp, property.PropertyType);
                            property.SetValue(this, valueToSet);
                        }
                    }
                }
            }
        }
    }
}
