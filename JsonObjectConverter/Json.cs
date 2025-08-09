using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonObjectConverter
{
    public class Json
    {
        public string head { get; set; }
        public List<Tuple<string, Tuple<string, bool>>> tupleList { get; private set; } = new List<Tuple<string, Tuple<string, bool>>>();
        public List<Tuple<string, Json>> jsonArray = new List<Tuple<string, Json>>();
        public string jsonText { get; } = string.Empty;
        public string mainLine { get; } = string.Empty;
        public List<Json> subJsons { get; set; }
        public List<string> items { get; set; }
        private int _subClass;
        public int subClass
        {
            get
            {
                return _subClass;
            }
            set
            {
                _subClass = value;
                if (subJsons != null)
                {
                    for (int i = 0; i < subJsons.Count; i++)
                    {
                        subJsons[i].subClass = _subClass + 1;
                    }
                }
                if (jsonArray != null)
                {
                    for (int i = 0; i < jsonArray.Count; i++)
                    {
                        jsonArray[i].Item2.subClass = _subClass + 1;
                    }
                }
            }
        }
        public static DateTime startingDateTime = new DateTime(1970, 1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        public Json(string jsontext, string head) : this(jsontext, head, -1)
        {

        }
        public Json(string jsonText, string head, int superClass)
        {
            this.subClass = superClass + 1;
            this.head = head.Trim(' ').Trim('\"').Trim('\r').Trim('\n').Trim(' ').Trim('\"');
            if (jsonText.Length > 0)
            {
                this.jsonText = jsonText;
                converLineToDictionary();
                bool noSubjsonsAndNoTuplelistAndNoJsonArray = true;
                if (this.subJsons != null)
                {
                    if (this.subJsons.Count == 0)
                    {
                        this.subJsons = null;
                    }
                    else
                    {
                        noSubjsonsAndNoTuplelistAndNoJsonArray = false;
                    }
                }
                if (this.tupleList != null)
                {
                    if (this.tupleList.Count == 0)
                    {
                        this.tupleList = null;
                    }
                    else
                    {
                        noSubjsonsAndNoTuplelistAndNoJsonArray = false;
                    }
                }
                if (this.jsonArray != null)
                {
                    noSubjsonsAndNoTuplelistAndNoJsonArray = false;
                }
                if (noSubjsonsAndNoTuplelistAndNoJsonArray)
                {
                    List<Object> objectList = Json.convertStringToList(this.jsonText, typeof(string));
                    if (objectList != null && objectList.Count > 0)
                    {
                        this.items = new List<string>();
                        Parallel.For(0, objectList.Count, intCounter => 
                        {
                            this.items.Add(trim((string)objectList[intCounter]));
                        });
                        //foreach (object item in objectList)
                        //{
                        //    this.items.Add(trim((string)item));
                        //}
                    }
                }
            }
        }
        private void converLineToDictionary()
        {
            try
            {
                JsonHelper helper = getJsonHelperHelper(this.jsonText);
                if (helper != null)
                {
                    if (helper.mainLine.Replace(" ", string.Empty).Replace(",", string.Empty).Length > 0)
                    {
                        char[] chars = helper.mainLine.ToCharArray();
                        List<Tuple<Tuple<string, bool>, bool>> splittedList = new List<Tuple<Tuple<string, bool>, bool>>();
                        int beginCharCounter = 0;
                        StringBuilder partOne = new StringBuilder();
                        StringBuilder partTwo = new StringBuilder();
                        bool writeOnFirstPart = true;
                        bool secondPartIsSubJson = false;
                        bool containsDoublePoint = false;
                        bool sublinesUsed = false;
                        int helperCounter = 0;
                        if (chars.Contains(':'))
                        {
                            for (int i = 0; i < chars.Length; i++)
                            {
                                if (chars[i].Equals(':') && beginCharCounter % 2 == 0)
                                {
                                    writeOnFirstPart = false;
                                    containsDoublePoint = true;
                                }
                                else if (chars[i].Equals(',') && !writeOnFirstPart && beginCharCounter % 2 == 0)
                                {
                                    string tempPartTwo = partTwo.ToString();
                                    string tempReplacedPartTwo = tempPartTwo.Replace(" ", string.Empty);
                                    int tempPartTwoLength = tempReplacedPartTwo.Length;
                                    if (partTwo.ToString().Replace(" ", string.Empty).Length == 0 && helper.subLines != null && helper.subLines.Count > 0 && trim(helper.subLines[helperCounter].Item2).StartsWith("["))
                                    {
                                        partTwo.Append(helper.subLines[helperCounter]);
                                        helperCounter++;
                                        secondPartIsSubJson = true;
                                    }
                                    writeOnFirstPart = true;
                                    splittedList.Add(new Tuple<Tuple<string, bool>, bool>(new Tuple<string, bool>(partOne.ToString() + ':' + partTwo.ToString(), partTwo.ToString().Contains('"')), secondPartIsSubJson));
                                    partOne.Clear();
                                    partTwo.Clear();
                                    secondPartIsSubJson = false;
                                }
                                else
                                {
                                    if (chars[i].Equals('\"'))
                                    {
                                        beginCharCounter++;
                                    }
                                    if (writeOnFirstPart)
                                    {
                                        partOne.Append(chars[i]);
                                    }
                                    else
                                    {
                                        partTwo.Append(chars[i]);
                                    }
                                }
                                if ((i + 1) == chars.Length)
                                {
                                    splittedList.Add(new Tuple<Tuple<string, bool>, bool>(new Tuple<string, bool>(partOne.ToString() + ':' + partTwo.ToString(), partTwo.ToString().Contains('"')), secondPartIsSubJson));
                                    partOne = new StringBuilder();
                                    partTwo = new StringBuilder();
                                }
                            }
                        }
                        int counter = 0;

                        foreach (Tuple<Tuple<string, bool>, bool> line in splittedList)
                        {
                            string[] lineSplitted = line.Item1.Item1.Split(':');
                            StringBuilder secondPartSB = new StringBuilder();
                            for (int i = 1; i < lineSplitted.Length; i++)
                            {
                                if (i > 1)
                                {
                                    secondPartSB.Append(':');
                                }
                                secondPartSB.Append(lineSplitted[i]);
                            }
                            string secondPart = secondPartSB.ToString().Trim(' ');
                            if (line.Item2)
                            {
                                if (this.subJsons == null)
                                {
                                    this.subJsons = new List<Json>();
                                }
                                List<string> stringList = getArrayOfString(helper.subLines[counter++].Item2);
                                sublinesUsed = true;
                                if (stringList != null)
                                {
                                    //Parallel.For(0, stringList.Count, intCounter =>
                                    //{
                                    //    this.subJsons.Add(new Json(trim(stringList[intCounter]), trim(lineSplitted[0]), this.subClass));
                                    //});
                                    foreach (string item in stringList)
                                    {
                                        this.subJsons.Add(new Json(trim(item), trim(lineSplitted[0]), this.subClass));
                                    }
                                }
                            }
                            else if ((trim(secondPart).Length == 0 && !secondPart.Equals("\"\"") || secondPart.StartsWith("{")) && helper.subLines.Count > 0)
                            {
                                if (this.subJsons == null)
                                {
                                    this.subJsons = new List<Json>();
                                }
                                string jsonText = trim(helper.subLines[counter].Item2.TrimStart('[').TrimEnd(']'));
                                string tempstring = helper.subLines[counter].Item2.Trim();
                                if (tempstring.StartsWith("["))
                                {
                                    string originalText = tempstring;
                                    tempstring = tempstring.TrimStart('[').TrimEnd(']');
                                    tempstring = trim(tempstring);
                                    if (tempstring.Length == 0)
                                    {
                                        if (originalText.Contains('[') && originalText.Contains(']'))
                                        {
                                            if (this.jsonArray == null)
                                            {
                                                this.jsonArray = new List<Tuple<string, Json>>();
                                            }
                                        }
                                    }
                                    else if (tempstring.StartsWith("{"))
                                    {
                                        List<string> tempList = getObjectArray(tempstring, originalText);
                                        if (tempList != null && tempList.Count > 0)
                                        {
                                            if (this.jsonArray == null)
                                            {
                                                this.jsonArray = new List<Tuple<string, Json>>();
                                            }
                                            //Parallel.For(0, tempList.Count, intCounter =>
                                            //{
                                            //    string iets = trim(line.Item1.Item1);
                                            //    this.jsonArray.Add(
                                            //        new Tuple<string, Json>(iets, new Json(tempList[intCounter], string.Empty)));
                                            //});
                                            foreach (string item in tempList)
                                            {
                                                string iets = trim(line.Item1.Item1);
                                                this.jsonArray.Add(new Tuple<string, Json>(iets, new Json(item, string.Empty, subClass)));
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    this.subJsons.Add(new Json(jsonText, trim(lineSplitted[0]), this.subClass));
                                }
                                sublinesUsed = true;
                                counter++;
                            }
                            else if (containsDoublePoint)
                            {
                                secondPart = secondPart.Replace("\\n", "\n");
                                this.tupleList.Add(new Tuple<string, Tuple<string, bool>>(trim(lineSplitted[0]), new Tuple<string, bool>(trim(secondPart), line.Item1.Item2)));
                            }
                        }
                        if (!sublinesUsed && helper.subLines != null && helper.subLines.Count > 0)
                        {
                            //Parallel.For(0, helper.subLines.Count, i =>
                            //{
                            //    if (helper.subLines[i].Item2.StartsWith("{"))
                            //    {
                            //        this.jsonArray.Add(new Tuple<string, Json>(this.head, new Json(helper.subLines[i].Item2, string.Empty)));
                            //    }
                            //});
                            for (int i = 0; i < helper.subLines.Count; i++)
                            {
                                if (helper.subLines[i].Item2.StartsWith("{"))
                                {
                                    this.jsonArray.Add(new Tuple<string, Json>(this.head, new Json(helper.subLines[i].Item2, string.Empty, subClass)));
                                }
                            }
                        }
                    }
                    else
                    {
                        Parallel.For(0, helper.subLines.Count, i =>
                        {
                            if (this.subJsons == null)
                            {
                                this.subJsons = new List<Json>();
                            }
                            this.subJsons.Add(new Json(trim(helper.subLines[i].Item2), string.Empty, this.subClass));
                        });
                        //for (int i = 0; i < helper.subLines.Count; i++)
                        //{
                        //    if (this.subJsons == null)
                        //    {
                        //        this.subJsons = new List<Json>();
                        //    }
                        //    this.subJsons.Add(new Json(trim(helper.subLines[i].Item2), string.Empty, this.subClass));
                        //}
                    }
                }
            }
            catch
            {

            }
        }
        private List<string> getObjectArray(string text, string originalText)
        {
            if (text.Length > 0)
            {
                List<string> stringList = new List<string>();
                text = text.Trim(' ');
                if (text.Length > 0)
                {
                    int quotesCounter = 0;
                    int objectCharCounter = 0;
                    bool objectCharCounterHasIncreased = false;
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < text.Length; i++)
                    {
                        if (!text[i].Equals(','))
                        {
                            sb.Append(text[i]);
                            if (text[i].Equals('"'))
                            {
                                quotesCounter++;
                            }
                            else if (quotesCounter % 2 == 0)
                            {
                                switch (text[i])
                                {
                                    case '{': objectCharCounter--; objectCharCounterHasIncreased = true; break;
                                    case '}': objectCharCounter++; break;
                                }
                                if (objectCharCounter.Equals(0) && objectCharCounterHasIncreased)
                                {
                                    stringList.Add(trim(sb.ToString().TrimStart('[').TrimStart(',')));
                                    sb = new StringBuilder();
                                    objectCharCounterHasIncreased = false;
                                }
                            }
                        }
                        else
                        {
                            sb.Append(text[i]);
                        }
                    }
                    if (stringList.Count > 0)
                    {
                        return stringList;
                    }
                }
            }
            else if (originalText.Contains('[') && originalText.Contains(']'))
            {
                return new List<string>();
            }
            return null;
        }
        private List<string> getArrayOfString(string text)
        {
            if (text.Length > 0)
            {
                List<string> stringList = new List<string>();
                text = text.Trim(' ');
                if (text.Length > 0)
                {
                    int quotesCounter = 0;
                    int arrayCharCounter = 0;
                    int objectCharCounter = 0;
                    bool arrayBeginCharCounterHasIncreased = false;
                    bool objectCharCounterHasIncreased = false;
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < text.Length; i++)
                    {
                        if (!text[i].Equals(',') || arrayBeginCharCounterHasIncreased)
                        {
                            sb.Append(text[i]);
                            if (text[i].Equals('"'))
                            {
                                quotesCounter++;
                            }
                            else if (quotesCounter % 2 == 0)
                            {
                                switch (text[i])
                                {
                                    case '[': arrayCharCounter++; arrayBeginCharCounterHasIncreased = true; break;
                                    case ']': arrayCharCounter--; break;
                                    case '{': objectCharCounter--; objectCharCounterHasIncreased = true; break;
                                    case '}': objectCharCounter++; break;
                                }
                                if (arrayCharCounter.Equals(1) && arrayBeginCharCounterHasIncreased && objectCharCounter.Equals(0) && (text.Contains('{') ? objectCharCounterHasIncreased : text[i].Equals(',')))
                                {
                                    stringList.Add(trim(sb.ToString().TrimStart('[').TrimStart(',')));
                                    sb = new StringBuilder();
                                    objectCharCounterHasIncreased = false;
                                }
                            }
                        }
                    }
                    if (stringList.Count > 1)
                    {
                        return stringList;
                    }
                }
            }
            return null;
        }
        private JsonHelper getJsonHelperHelper(string line)
        {
            try
            {
                line = line.Substring(1, line.Length - 2);
                StringBuilder mainLine = new StringBuilder();
                List<Tuple<string, StringBuilder>> subLinesSB = new List<Tuple<string, StringBuilder>>();
                char[] chars = line.ToCharArray();
                short quotesCounter = 0;
                int beginCharCounter = -1;
                for (int i = 0; i < chars.Length; i++)
                {
                    string deze = string.Empty;
                    if (i > 7 && i + 5 < chars.Length)
                    {
                        deze = chars[i - 7].ToString() + chars[i - 6].ToString() + chars[i - 5].ToString() + chars[i - 4].ToString() + chars[i - 3].ToString() + chars[i - 2].ToString() + chars[i - 1].ToString() + chars[i].ToString();
                    }
                    else if (i > 2)
                    {
                        deze = chars[i - 3].ToString() + chars[i - 2].ToString() + chars[i - 1].ToString() + chars[i].ToString();
                    }
                    string iets = getAlreadyUsedString(chars);
                    if (chars[i].Equals('"'))
                    {
                        //i 200 is bij color
                        quotesCounter++;
                    }
                    if (quotesCounter % 2 == 0)
                    {
                        if (chars[i].Equals('{') || chars[i].Equals('['))
                        {
                            if (beginCharCounter < 0) //Gaat van -1 naar 0 dus is het een nieuwe subLine
                            {
                                string[] splitted = mainLine.ToString().Split(',');
                                string theHead = string.Empty;
                                if (splitted.Length > 0)
                                {
                                    theHead = trim(splitted[splitted.Length - 1]).TrimEnd(':').Trim('"');
                                }
                                subLinesSB.Add(new Tuple<string, StringBuilder>(theHead, new StringBuilder()));
                            }
                            //else
                            //{
                            //    mainLine.Append(chars[i]);
                            //}
                            subLinesSB[subLinesSB.Count - 1].Item2.Append(chars[i]);
                            beginCharCounter++;
                        }
                        else if (chars[i].Equals('}') || chars[i].Equals(']'))
                        {
                            if (beginCharCounter >= 0)
                            {
                                subLinesSB[subLinesSB.Count - 1].Item2.Append(chars[i]);
                            }
                            //else
                            //{
                            //    mainLine.Append(chars[i]);
                            //}
                            beginCharCounter--;
                        }
                        else if (beginCharCounter >= 0)
                        {
                            subLinesSB[subLinesSB.Count - 1].Item2.Append(chars[i]);
                        }
                        else
                        {
                            mainLine.Append(chars[i]);
                        }
                    }
                    else if (beginCharCounter >= 0)
                    {
                        subLinesSB[subLinesSB.Count - 1].Item2.Append(chars[i]);
                    }
                    else
                    {
                        mainLine.Append(chars[i]);
                    }
                }
                return new JsonHelper(line, mainLine, subLinesSB);
            }
            catch
            {
                return null;
            }
        }
        public string generateTabs()
        {
            return generateTabs(0);
        }
        public string generateTabs(int extraAmount)
        {
            return generateTabs(this.subClass, extraAmount);
        }
        public static string generateTabs(int subclass, int extraAmount)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < subclass + extraAmount; i++)
            {
                sb.Append("  ");
            }
            return sb.ToString();
        }
        public string generateJson(bool isPartOfArray = false)
        {
            StringBuilder sb = new StringBuilder();
            if (this.head.Length > 0 && !isPartOfArray)
            {
                sb.Append(generateTabs() + "\"" + this.head + "\" : ");
            }
            bool createNewAccolades = NewAccoladesRequired();

            if (createNewAccolades)
            {
                sb.AppendLine((this.head.Length > 0 && !isPartOfArray ? string.Empty : generateTabs()) + "{");
            }

            bool firstTime = true;
            if (this.tupleList != null && this.tupleList.Count > 0)
            {
                foreach (Tuple<string, Tuple<string, bool>> aTuple in this.tupleList)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                    }
                    else
                    {
                        sb.AppendLine(",");
                    }
                    sb.Append(generateTabs(1) + "\"" + aTuple.Item1 + "\": " + (aTuple.Item2.Item2 ? "\"" : string.Empty) + aTuple.Item2.Item1 + (aTuple.Item2.Item2 ? "\"" : string.Empty));
                }
            }
            if (this.jsonArray != null && this.jsonArray.Count > 0)
            {
                int subjsonsWithItems = 0;
                bool ft = true;
                foreach (var aJson in this.jsonArray)
                {
                    if (aJson.Item2.tupleList == null && aJson.Item2.subJsons == null && aJson.Item2.items != null && aJson.Item2.items.Count > 0)
                    {
                        subjsonsWithItems++;
                        if (firstTime)
                        {
                            firstTime = false;
                        }
                        else
                        {
                            sb.AppendLine(",");
                        }
                        StringBuilder tempSB = new StringBuilder();
                        foreach (string item in aJson.Item2.items)
                        {
                            if (ft)
                            {
                                ft = false;
                            }
                            else
                            {
                                tempSB.AppendLine(",");
                            }
                            tempSB.Append(generateTabs(2) + "\"" + item + "\"");
                        }
                        sb.AppendLine(generateTabs(1) + "\"" + aJson.Item1 + "\": [");
                        sb.Append(tempSB.ToString() + "\n" + generateTabs(1) + "]");
                    }
                }
                for (int i = 0; i < this.jsonArray.Count; i++)
                {
                    bool firstTimeThisHead = false;
                    bool lastTimeThisHead = false;
                    if (this.jsonArray.Count > 1)
                    {
                        for (int j = 0; j < this.jsonArray.Count; j++)
                        {
                            if (!this.jsonArray[i].Equals(this.jsonArray[j]))
                            {
                                if (this.jsonArray[j].Item1.Equals(this.jsonArray[i].Item1))
                                {
                                    if (i < j)
                                    {
                                        firstTimeThisHead = true;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    else if (this.jsonArray.Count == 1)
                    {
                        firstTimeThisHead = true;
                    }
                    for (int j = 0; j < this.jsonArray.Count; j++)
                    {
                        if (this.jsonArray[j].Item1.Equals(this.jsonArray[i].Item1))
                        {
                            if (i >= j)
                            {
                                lastTimeThisHead = true;
                            }
                            else
                            {
                                lastTimeThisHead = false;
                            }
                        }
                    }
                    if (subjsonsWithItems > 0 || firstTime)
                    {
                        firstTime = false;
                    }
                    else
                    {
                        sb.AppendLine(",");
                    }

                    if (firstTimeThisHead)
                    {
                        sb.Append(generateTabs(1) + "\"" + this.jsonArray[i].Item1 + "\" : [\n");
                    }
                    if (this.jsonArray[i].Item2.tupleList == null && this.jsonArray[i].Item2.subJsons == null)
                    {
                        firstTime = true;
                    }
                    else
                    {
                        sb.Append(this.jsonArray[i].Item2.generateJson());
                    }
                    if (lastTimeThisHead)
                    {
                        sb.Append("\n" + generateTabs(1) + "]");
                    }
                }
            }
            if (this.subJsons != null && this.subJsons.Count > 0)
            {
                int subjsonsWithItems = 0;
                bool ft = true;
                foreach (Json aJson in this.subJsons)
                {
                    if (aJson.tupleList == null && aJson.subJsons == null && aJson.items != null && aJson.items.Count > 0)
                    {
                        subjsonsWithItems++;
                        if (firstTime)
                        {
                            firstTime = false;
                        }
                        else
                        {
                            sb.AppendLine(",");
                        }
                        StringBuilder tempSB = new StringBuilder();
                        foreach (string item in aJson.items)
                        {
                            if (ft)
                            {
                                ft = false;
                            }
                            else
                            {
                                tempSB.AppendLine(",");
                            }
                            tempSB.Append(generateTabs(2) + "\"" + item + "\"");
                        }
                        sb.AppendLine(generateTabs(1) + "\"" + aJson.head + "\": [");
                        sb.Append(tempSB.ToString() + "\n" + generateTabs(1) + "]");
                    }
                }

                if (this.subJsons.Count > subjsonsWithItems)
                {
                    for (int i = 0; i < this.subJsons.Count; i++)
                    {
                        bool firstTimeThisHead = false;
                        bool lastTimeThisHead = false;
                        bool multipleWithThisHead = false;
                        if (this.subJsons.Count > 1)
                        {
                            for (int j = 0; j < this.subJsons.Count; j++)
                            {
                                if (this.subJsons[i] != this.subJsons[j])
                                {
                                    if (this.subJsons[j].head == this.subJsons[i].head)
                                    {
                                        multipleWithThisHead = true;
                                        if (i < j)
                                        {
                                            firstTimeThisHead = true;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        if (multipleWithThisHead)
                        {
                            for (int j = 0; j < this.subJsons.Count; j++)
                            {
                                if (this.subJsons[j].head.Equals(this.subJsons[i].head))
                                {
                                    if (i >= j)
                                    {
                                        lastTimeThisHead = true;
                                    }
                                    else
                                    {
                                        lastTimeThisHead = false;
                                    }
                                }
                            }
                        }
                        if (subjsonsWithItems > 0 || firstTime)
                        {
                            firstTime = false;
                        }
                        else
                        {
                            sb.AppendLine(",");
                        }

                        if (firstTimeThisHead && multipleWithThisHead)
                        {
                            sb.Append(generateTabs(1) + "\"" + this.subJsons[i].head + "\" : [\n");
                        }
                        sb.Append(this.subJsons[i].generateJson(multipleWithThisHead));
                        if (multipleWithThisHead && lastTimeThisHead)
                        {
                            sb.Append("\n" + generateTabs(1) + "]");
                        }
                    }
                }
            }

            if (createNewAccolades)
            {
                sb.Append("\n" + generateTabs() + "}");
            }
            return sb.ToString();
        }
        private bool NewAccoladesRequired()
        {
            return this.tupleList != null && this.tupleList.Count > 0 ||
             this.jsonArray != null && this.jsonArray.Count > 0 ||
             this.subJsons != null && this.subJsons.Count > 0;
        }
        public static string generateJsonFromObject(object anObject)
        {
            return generateJsonFromObject(anObject, 0);
        }
        public static string generateJsonFromObject(object anObject, int tabs)
        {
            //var bnObject = Convert.ChangeType(anObject, aType);
            if (anObject.GetType().GetProperties() != null)
            {
                if (anObject.GetType().GetProperties().Length > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    bool firstTime = true;
                    foreach (System.Reflection.PropertyInfo property in anObject.GetType().GetProperties())
                    {
                        if (!property.GetAccessors(true)[0].IsStatic)
                        {
                            if (property.GetValue(anObject, null) != null)
                            {
                                if (firstTime)
                                {
                                    firstTime = false;
                                    sb.Append(generateTabs(0, tabs) + (anObject.GetType().GetProperties() != null && anObject.GetType().GetProperties().Length > 0 ? "{\n" : string.Empty));
                                }
                                else
                                {
                                    sb.AppendLine(",");
                                }
                                sb.Append(generateTabs(0, 1 + tabs) + generateJsonFromVariabel(property.Name, property.GetValue(anObject, null).ToString()));
                            }
                        }
                    }
                    if (!firstTime)
                    {
                        sb.Append("\n" + generateTabs(0, tabs) + "}");
                    }
                    return sb.ToString();
                }
            }
            return string.Empty;
        }
        private static string generateJsonFromVariabel(string name, string value)
        {
            return "\"" + name + "\": \"" + value + "\"";
        }
        public static List<object> convertStringToList(string text, Type type)
        {
            if (type.GetGenericArguments().FirstOrDefault() != null)
            {
                type = type.GetGenericArguments().FirstOrDefault();
            }
            List<object> objectList = new List<object>();
            string[] splitted = text.TrimStart('[').TrimEnd(']').TrimStart('{').TrimEnd('}').Split(',');
            foreach (string item in splitted)
            {
                string tempItem = item.Trim(' ').Trim('\"');
                object iets = convertStringToType(tempItem, type);
                objectList.Add(iets);
            }
            return objectList;
        }
        public static object convertStringToType(string text, Type type)
        {
            return Convert.ChangeType(text, type);
        }
        public static DateTime convertStringToDateTime(string date)
        {
            try
            {
                date = convertToDate(date);
                string[] splitted = date.Split(' ');
                string[] firstPart = splitted[0].Split('-');
                string[] secondPart = splitted[1].Split(':');
                DateTime tempDateTime = new DateTime(Convert.ToInt32(firstPart[0]), Convert.ToInt32(firstPart[1]), Convert.ToInt32(firstPart[2]), Convert.ToInt32(secondPart[0]), Convert.ToInt32(secondPart[1]), Convert.ToInt32(secondPart[2]));
                if (isWinter(tempDateTime))
                {
                    tempDateTime = tempDateTime.AddHours(1);
                }
                return tempDateTime;
            }
            catch
            {
                DateTime dtDateTime = new DateTime(startingDateTime.Ticks);
                try
                {
                    Int32 remainingSeconds = Int32.Parse(date);
                    while (remainingSeconds > 0)
                    {
                        if (remainingSeconds > Int32.MaxValue)
                        {
                            dtDateTime = dtDateTime.AddSeconds(Int32.MaxValue);
                            remainingSeconds = remainingSeconds - Int32.MaxValue;
                        }
                        else
                        {
                            dtDateTime = dtDateTime.AddSeconds(remainingSeconds);
                            remainingSeconds = remainingSeconds - remainingSeconds;
                        }
                    }
                    if (isWinter(dtDateTime))
                    {
                        dtDateTime = dtDateTime.AddHours(1);
                    }
                    return dtDateTime;
                }
                catch
                {
                    return dtDateTime.Add(new TimeSpan(long.Parse(date)));
                }
            }
        }
        public static bool isWinter(DateTime aDatetime)
        {
            //eerste zondag van november
            DateTime lastSundayOfOctobre = new DateTime(aDatetime.Year, 10, 1);
            List<DateTime> zondagen = new List<DateTime>();
            for (int i = 0; i < 31; i++)
            {
                if (lastSundayOfOctobre.DayOfWeek == DayOfWeek.Sunday)
                {
                    zondagen.Add(lastSundayOfOctobre);
                }
                lastSundayOfOctobre = lastSundayOfOctobre.AddDays(1);
            }
            lastSundayOfOctobre = zondagen[zondagen.Count - 1];

            //2de zondag van maart
            DateTime lastSundayOfMarch = new DateTime(aDatetime.Year, 3, 1);
            zondagen = new List<DateTime>();
            for (int i = 0; i < 31; i++)
            {
                if (lastSundayOfMarch.DayOfWeek == DayOfWeek.Sunday)
                {
                    zondagen.Add(lastSundayOfMarch);
                }
                lastSundayOfMarch = lastSundayOfMarch.AddDays(1);
            }
            lastSundayOfMarch = zondagen[zondagen.Count - 1];

            if (aDatetime >= lastSundayOfOctobre)
            {
                return true;
            }
            else if (lastSundayOfMarch < aDatetime)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private static string convertToDate(string date)
        {
            string[] splitted = date.Replace('/', '-').Split(' ');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < splitted.Length; i++)
            {
                if (i < 2)
                {
                    if (i > 0)
                    {
                        sb.Append(' ');
                    }
                    sb.Append(splitted[i]);
                }
            }
            return sb.ToString();
        }
        public static string trim(string iets)
        {
            List<char> charList = new List<char>() { ' ', '"', '\r', '\n', ':' };
            for (int i = 0; i < charList.Count; i++)
            {
                foreach (char x in charList)
                {
                    iets = iets.Trim(x);
                }
            }
            return iets;
        }
        public List<Json> sortProperty(string propertyToSortBy, Json aJson, bool sortBySubJsonsAsWell, bool reverse)
        {
            return sortProperty(propertyToSortBy, new List<Json> { aJson }, sortBySubJsonsAsWell, reverse);
        }
        public List<Json> sortProperty(string propertyToSortBy, List<Json> jsonList, bool sortSubJsonsAsWell, bool reverse)
        {
            for (int i = 0; i < jsonList.Count; i++)
            {
                if (jsonList[i].tupleList != null && jsonList[i].tupleList.Count > 0)
                {
                    for (int j = 0; j < jsonList[i].tupleList.Count; j++)
                    {
                        if (jsonList[i].tupleList[j].Item1.ToLower().Equals(propertyToSortBy.ToLower()))
                        {
                            jsonList = jsonList.OrderBy(x => jsonList[i].GetType().GetProperties()[j].GetValue(x, null)).ToList();
                            if (reverse)
                            {
                                jsonList.Reverse();
                            }
                            break;
                        }
                    }
                    break;
                }
            }
            for (int i = 0; i < jsonList.Count; i++)
            {
                if (jsonList[i].subJsons != null && jsonList[i].subJsons.Count > 0)
                {
                    if (sortSubJsonsAsWell && jsonList[i].subJsons != null && jsonList[i].subJsons.Count > 0)
                    {
                        jsonList[i].subJsons = this.sortProperty(propertyToSortBy, jsonList[i].subJsons, sortSubJsonsAsWell, reverse);
                    }
                }
            }
            return jsonList;
        }
        private static string getAlreadyUsedString(char[] charList)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < charList.Length; i++)
            {
                sb.Append(charList[i]);
            }
            return sb.ToString();
        }
    }
}
