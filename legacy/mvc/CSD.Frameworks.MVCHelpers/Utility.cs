using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Resources;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Collections.Specialized;
using System.Web.Mvc;
using System.Linq;
using Runtime = Microsoft.CSharp.RuntimeBinder;
using System.Dynamic;

namespace CSD.MVC.Helpers
{
    public static class Constants
    {
        public const string AntiForgerySalt = "Jackdaw, love my big sphinx of quartz. 95B3777D-9D24-4FCF-91BF-344A2F7B6E98";
        public const string CharacterResource = " jackdaws love my big sphinx of quartz THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG 0123456789";
        public const int GRID_PAGINATOR_COUNT = 10;
        public const string
            USER_CONTEXT = "UserContext",
            USER_NAME = "UserName",
            USER_PARAMS = "Params",
            CAPTCHA = "Captcha",
            ROLES = "Roles",
            MODEL_NAMESPACE_PREFIX = "Model",
            MODEL_ASSEMBLY_NAME = "Model";
        public const string
            ROLE_EVERYONE = "All";
        public const string
            MIME_JSON = "application/json",
            MIME_PNG = "img/png";
    }

    public static class Utilities
    {
        public static string GetResourceString(LambdaExpression expression)
        {
            if ((expression.Body is MemberExpression) && ((expression.Body as MemberExpression).Member is PropertyInfo))
            {
                var member = (expression.Body as MemberExpression).Member as PropertyInfo;
                var resource = new ResourceManager(string.Format(CultureInfo.InvariantCulture, "{0}.Resources.{1}", member.DeclaringType.Namespace, member.DeclaringType.Name), Assembly.Load(new AssemblyName("Model")));
                try
                {
                    return resource.GetString(member.Name);
                }
                catch (MissingManifestResourceException)
                {
                    return "";// Glossary.ResourceManager.GetString(member.Name);
                }
            }
            else if (expression.Body is DynamicExpression)
            {
                var argument = (expression.Body as DynamicExpression).Arguments[0];
                string propertyName = ((expression.Body as DynamicExpression).Binder as GetMemberBinder).Name;
                if (argument.Type == typeof(object))
                {
                    Type type = Type.GetType("Sama90.Model." + ((argument as DynamicExpression).Binder as GetMemberBinder).Name + ", Model");
                    var resource = new ResourceManager(string.Format(CultureInfo.InvariantCulture, "{0}.Resources.{1}", type.Namespace, type.Name), Assembly.Load(new AssemblyName("Model")));
                    return resource.GetString(propertyName);
                }
                else
                {
                    return "";// Glossary.ResourceManager.GetString(propertyName);
                }
            }
            else
            {
                return "";//Glossary.ResourceManager.GetString(expression.Body.ToString());
            }
        }

        public static int PythonHash(string input)
        {
            int retVal = 0;
            if (!string.IsNullOrEmpty(input))
            {
                retVal = input[0] << 7;
                foreach (char c in input)
                {
                    unchecked
                    {
                        retVal = (1000003 * retVal) ^ c;
                    }
                }
                retVal = retVal ^ input.Length;
            }
            return retVal;
        }

        public static short ShortHash(string input)
        {
            int hash = PythonHash(input);
            return (short)((hash >> 16) ^ (hash & 0xffff));
        }

        public static int PythonCombineHash(params int[] values)
        {
            int retVal = 0;
            if (values.Length != 0)
            {
                retVal = values[0];
                foreach (int i in values)
                {
                    unchecked
                    {
                        retVal = (1000003 * retVal) ^ i;
                    }
                }
                retVal = retVal ^ values.Length;
            }
            return retVal;
        }

        public static bool CompareStringArrays(string[] stringA, string[] stringB, StringComparison comparisonType)
        {
            if ((stringA != null) || (stringB != null))
            {
                if ((stringA == null) || (stringB == null))
                {
                    return false;
                }
                if (stringA.Length != stringB.Length)
                {
                    return false;
                }
                for (int i = 0; i < stringA.Length; i++)
                {
                    if (!string.Equals(stringA[i], stringB[i], comparisonType))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static string JoinAny<T>(IEnumerable<T> reader, string delimiter)
        {
            if (reader == null)
            {
                return string.Empty;
            }

            IEnumerator<T> enumerator = reader.GetEnumerator();
            bool hasMoreItems = enumerator.MoveNext();
            StringBuilder builder = new StringBuilder();
            while (hasMoreItems)
            {
                builder.Append(enumerator.Current);
                hasMoreItems = enumerator.MoveNext();
                if (hasMoreItems)
                {
                    builder.Append(delimiter);
                }
            }
            return builder.ToString();
        }

        public static string JoinAny<T>(IEnumerable<T> reader, string delimiter, string quoteLeft, string quoteRight)
        {
            if (reader == null)
            {
                return string.Empty;
            }

            IEnumerator<T> enumerator = reader.GetEnumerator();
            bool hasMoreItems = enumerator.MoveNext();
            StringBuilder builder = new StringBuilder();
            while (hasMoreItems)
            {
                builder.Append(enumerator.Current);
                hasMoreItems = enumerator.MoveNext();
                if (hasMoreItems)
                {
                    builder.Append(quoteLeft).Append(delimiter).Append(quoteRight);
                }
            }
            return builder.ToString();
        }

        public static string JoinAny<T>(IEnumerable<T> reader)
        {
            return JoinAny(reader, ",");
        }

        public static string ConvertAndJoin<T>(IEnumerable<T> reader, Converter<T, string> converter, string delimiter)
        {
            if (reader == null)
            {
                return string.Empty;
            }
            IEnumerator<T> enumerator = reader.GetEnumerator();
            bool hasMoreItems = enumerator.MoveNext();
            StringBuilder builder = new StringBuilder();
            while (hasMoreItems)
            {
                builder.Append(converter(enumerator.Current));
                hasMoreItems = enumerator.MoveNext();
                if (hasMoreItems)
                {
                    builder.Append(delimiter);
                }
            }
            return builder.ToString();
        }

        public static string[] JustConvert<T>(IEnumerable<T> reader, Converter<T, string> converter)
        {
            if (reader == null)
            {
                return null;
            }
            IEnumerator<T> enumerator = reader.GetEnumerator();
            List<string> result = new List<string>();
            while (enumerator.MoveNext())
            {
                result.Add(converter(enumerator.Current));
            }
            return result.ToArray();
        }

        public static IEnumerator<T> SplitAndConvert<T>(string input, Converter<string, T> converter)
        {
            if (string.IsNullOrEmpty(input))
            {
                yield break;
            }
            int startIndex = 0;
            int endIndex = input.IndexOf(',');
            while (endIndex != -1)
            {
                yield return converter(input.Substring(startIndex, endIndex));
                startIndex = endIndex + 1;
                endIndex = input.IndexOf(',', startIndex);
            }
            yield return converter(input.Substring(startIndex));
        }

        public static void SplitAndDoAction(string input, Action<string> action)
        {
            if (string.IsNullOrEmpty(input))
            {
                return;
            }
            int startIndex = 0;
            int endIndex = input.IndexOf(',');
            while (endIndex != -1)
            {
                action(input.Substring(startIndex, endIndex - startIndex));
                startIndex = endIndex + 1;
                endIndex = input.IndexOf(',', startIndex);
            }
            action(input.Substring(startIndex));
        }

        public static string RepairBadUrl(string url)
        {
            return url.Replace("&", "&amp;").Replace("\"", "&quote;");
        }

        public static void CheckArrayParameter(ref string[] param, bool checkForNull, bool checkIfEmpty, bool checkForCommas, int maxSize, string paramName)
        {
            object[] objectArray1;
            object[] objectArray2;
            if (param == null)
            {
                throw new ArgumentNullException(paramName);
            }
            if (param.Length < 1)
            {
                objectArray1 = new object[] { paramName };
                throw new ArgumentException("Parameter_array_empty", paramName);
            }
            Dictionary<string, string> paramsDictionary = new Dictionary<string, string>(param.Length);
            int i1 = (param.Length - 1);
            while (i1 >= 0)
            {
                CheckParameter(ref param[i1], checkForNull, checkIfEmpty, checkForCommas, maxSize, (paramName + "[ " + i1.ToString(((IFormatProvider)CultureInfo.InvariantCulture)) + " ]"));
                if (!paramsDictionary.ContainsKey(param[i1]))
                {
                    paramsDictionary.Add(param[i1], param[i1]);
                    i1--;
                    continue;
                }
                objectArray2 = new object[] { paramName };
                throw new ArgumentException("Parameter_duplicate_array_element", paramName);
            }
        }

        public static void CheckParameter(ref string param, bool checkForNull, bool checkIfEmpty, bool checkForCommas, int maxSize, string paramName)
        {
            object[] objectArray1;
            object[] objectArray2;
            if (param == null)
            {
                if (checkForNull)
                {
                    throw new ArgumentNullException(paramName);
                }
                else
                {
                    return;
                }
            }
            param = param.Trim();
            if (checkIfEmpty && (param.Length < 1))
            {
                objectArray1 = new object[] { paramName };
                throw new ArgumentException("Parameter_can_not_be_empty", paramName);
            }
            if ((maxSize > 0) && (param.Length > maxSize))
            {
                objectArray2 = new object[] { paramName, maxSize.ToString(((IFormatProvider)CultureInfo.InvariantCulture)) };
                throw new ArgumentException("Parameter_too_long", paramName);
            }
            if ((!checkForCommas) || (!param.Contains(",")))
            {
                return;
            }
            object[] objectArray3 = new object[] { paramName };
            throw new ArgumentException("Parameter_can_not_contain_comma", paramName);
        }

        public static int GetIntValue(NameValueCollection config, string valueName, int defaultValue, bool zeroAllowed, int maxValueAllowed)
        {
            int i1;
            object[] objectArray1;
            object[] objectArray2;
            object[] objectArray3;
            object[] objectArray4;
            string string1 = config[valueName];
            if (string1 == null)
            {
                return defaultValue;
            }
            if (!int.TryParse(string1, out i1))
            {
                if (zeroAllowed)
                {
                    objectArray1 = new object[] { valueName };
                    throw new ArgumentException("Value_must_be_non_negative_integer");
                }
                else
                {
                    objectArray2 = new object[] { valueName };
                    throw new ArgumentException("Value_must_be_positive_integer");
                }
            }
            if (zeroAllowed && (i1 < 0))
            {
                objectArray3 = new object[] { valueName };
                throw new ArgumentException("Value_must_be_non_negative_integer");
            }
            if ((!zeroAllowed) && (i1 <= 0))
            {
                objectArray4 = new object[] { valueName };
                throw new ArgumentException("Value_must_be_positive_integer");
            }
            if (maxValueAllowed <= 0)
            {
                return i1;
            }
            if (i1 <= maxValueAllowed)
            {
                return i1;
            }
            object[] objectArray5 = new object[] { valueName, maxValueAllowed.ToString(((IFormatProvider)CultureInfo.InvariantCulture)) };
            throw new ArgumentException("Value_too_big");
        }

        public static IEnumerable<string> EnumerateStringParts(string expression, params char[] delimiters)
        {
            string[] parts = expression.Split(delimiters);
            foreach (string part in parts)
            {
                yield return part;
            }
        }

        public static int CountOneBits(int number)
        {
            int result = 0;
            for (int i = 0; i < 64; i++)
            {
                result += (number & 1);
                number >>= 1;
            }
            return result;

        }

        public static string JavaScriptString(string input)
        {
            return input.Replace("\n", "\\n").Replace("\b", "\\b").Replace("\t", "\\t").Replace("\f", "\\f").Replace("\'", "\\'").Replace("\"", "\\\"");
        }

        public static string Map(string input, char[] mapSource, char[] mapDest)
        {
            StringBuilder builder = new StringBuilder(input.Length);
            int charLocation = -1;
            foreach (char c in input)
            {
                charLocation = ((IList<char>)mapSource).IndexOf(c);
                if (charLocation != -1)
                {
                    builder.Append(mapDest[charLocation]);
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }

        public static string Map(string input, string[] mapSource, string[] mapDest)
        {
            string result = input;
            for (int i = 0; i < mapSource.Length; i++)
            {
                result = result.Replace(mapSource[i], mapDest[i]);
            }
            return result;
        }

        public static byte[] Compress(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            MemoryStream memoryStream = new MemoryStream();
            using (GZipStream zip = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;
            MemoryStream outStream = new MemoryStream();

            byte[] compressed = new byte[memoryStream.Length];
            memoryStream.Read(compressed, 0, compressed.Length);

            byte[] gzBuffer = new byte[compressed.Length + 4];
            Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            return gzBuffer;
        }

        public static byte[] CompressArray(byte[] buffer)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (GZipStream zip = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;
            MemoryStream outStream = new MemoryStream();

            byte[] compressed = new byte[memoryStream.Length];
            memoryStream.Read(compressed, 0, compressed.Length);

            byte[] gzBuffer = new byte[compressed.Length + 4];
            Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            return gzBuffer;
        }

        public static string Decompress(byte[] gzBuffer)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                int msgLength = BitConverter.ToInt32(gzBuffer, 0);
                ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

                byte[] buffer = new byte[msgLength];

                ms.Position = 0;
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    zip.Read(buffer, 0, buffer.Length);
                }

                return Encoding.ASCII.GetString(buffer);
            }
        }

        public static byte[] DecompressArray(byte[] gzBuffer)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                int msgLength = BitConverter.ToInt32(gzBuffer, 0);
                ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

                byte[] buffer = new byte[msgLength];

                ms.Position = 0;
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    zip.Read(buffer, 0, buffer.Length);
                }

                return buffer;
            }
        }

        public static byte[] TrimArray(byte[] array)
        {
            int l = array.Length;
            byte zeroCount = 0;
            List<byte> list = new List<byte>();
            for (int i = 0; i < l; i++)
            {
                if (array[i] == 0)
                {
                    if (zeroCount == 0) list.Add(0);
                    zeroCount++;
                }
                else
                {
                    if (zeroCount > 0)
                    {
                        list.Add(zeroCount);
                        zeroCount = 0;
                    }
                    list.Add(array[i]);
                }
            }
            if (zeroCount > 1) list.Add(zeroCount);

            return list.ToArray();
        }

        public static byte[] UntrimArray(byte[] array)
        {
            int l = array.Length;
            bool zeroSeen = false;
            List<byte> list = new List<byte>();
            for (int i = 0; i < l; i++)
            {
                if (zeroSeen)
                {
                    byte[] zeroArray = new byte[array[i]];
                    zeroArray.Initialize();
                    list.AddRange(zeroArray);
                    zeroSeen = false;
                }
                else
                {
                    if (array[i] == 0)
                    {
                        zeroSeen = true;
                    }
                    else
                    {
                        list.Add(array[i]);
                    }
                }
            }
            if (zeroSeen) list.Add(0);

            return list.ToArray();
        }

        public static byte[] GreyCode(byte[] array, bool reverse)
        {
            int l = array.Length;
            byte[] result = new byte[l];
            array.CopyTo(result, 0);
            if (reverse)
            {
                for (int i = l - 1; i >= 0; i--)
                {
                    if (i < l - 1)
                    {
                        result[i] ^= result[i + 1];
                    }
                }
            }
            else
            {
                for (int i = 0; i < l; i++)
                {
                    if (i < l - 1)
                    {
                        result[i] ^= result[i + 1];
                    }
                }
            }
            return result;
        }

        public static byte[] XorArray(byte[] array, byte key)
        {
            int l = array.Length;
            byte[] result = new byte[l];
            for (int i = 0; i < l; i++)
            {
                result[i] = (byte)(array[i] ^ key);
            }
            return result;
        }

        public static string ArrayToHex(byte[] array)
        {
            string result;
            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                writer.Write("0x");
                for (int i = 0; i < array.Length; i++)
                {
                    writer.Write("{0:x2}", array[i]);
                }
                result = writer.ToString();
            }
            return result;
        }

        public static byte[] HexToArray(string hex)
        {
            int startindex = hex.StartsWith("0x") ? 2 : 0;
            string realHex = ((hex.Length & 1) == 1 ? "0" : "") + hex.Substring(startindex);

            if (realHex.Length <= 2)
                return new byte[] { byte.Parse(realHex, NumberStyles.HexNumber) };

            byte[] result = new byte[realHex.Length >> 1];
            for (int i = 0; i < (realHex.Length >> 1); i++)
            {
                result[i] = byte.Parse(realHex.Substring(realHex.Length - (i << 1) - 2, 2), NumberStyles.HexNumber);
            }
            return result;
        }

        public static int HashDict(NameValueCollection collection)
        {
            List<int> itemHash = new List<int>();
            foreach (string item in collection)
            {
                itemHash.Add(PythonHash(item) ^ PythonHash(collection[item]));
            }
            return PythonCombineHash(itemHash.ToArray());
        }

        public static string GetFullPropertyName<T, TProperty>
        (Expression<Func<T, TProperty>> exp)
        {
            MemberExpression memberExp;
            if (!TryFindMemberExpression(exp.Body, out memberExp))
                return string.Empty;

            var memberNames = new Stack<string>();
            do
            {
                memberNames.Push(memberExp.Member.Name);
            }
            while (TryFindMemberExpression(memberExp.Expression, out memberExp));

            return string.Join(".", memberNames.ToArray());
        }

        public static string GetFullPropertyName(LambdaExpression exp)
        {
            return GetFullPropertyName(exp.Body);
        }

        public static string GetFullPropertyName(Expression exp)
        {
            MemberExpression memberExp;
            if (!TryFindMemberExpression(exp, out memberExp))
                return string.Empty;

            var memberNames = new Stack<string>();
            do
            {
                memberNames.Push(memberExp.Member.Name);
            }
            while (TryFindMemberExpression(memberExp.Expression, out memberExp));

            return string.Join(".", memberNames.ToArray());
        }

        // code adjusted to prevent horizontal overflow
        private static bool TryFindMemberExpression
        (Expression exp, out MemberExpression memberExp)
        {
            memberExp = exp as MemberExpression;
            if (memberExp != null)
            {
                // heyo! that was easy enough
                return true;
            }

            // if the compiler created an automatic conversion,
            // it'll look something like...
            // obj => Convert(obj.Property) [e.g., int -> object]
            // OR:
            // obj => ConvertChecked(obj.Property) [e.g., int -> long]
            // ...which are the cases checked in IsConversion
            if (IsConversion(exp) && exp is UnaryExpression)
            {
                memberExp = ((UnaryExpression)exp).Operand as MemberExpression;
                if (memberExp != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsConversion(Expression exp)
        {
            return (
                exp.NodeType == ExpressionType.Convert ||
                exp.NodeType == ExpressionType.ConvertChecked
            );
        }

        public static MemberExpression GetPropertyExpression(Expression expression, string name)
        {
            if (name.Contains('.'))
            {
                string[] items = name.Split(".".ToCharArray(), 2);
                return GetPropertyExpression(Expression.Property(expression, items[0]), items[1]);
            }
            else
            {
                return Expression.Property(expression, name);
            }
        }


        public static Expression GetDynamicPropertyExpression(Expression expression, string name)
        {
            if (name.Contains('.'))
            {
                string[] items = name.Split(".".ToCharArray(), 2);
                var binder = Runtime.Binder.GetMember(Runtime.CSharpBinderFlags.None, items[0], typeof(Utils), new Runtime.CSharpArgumentInfo[] { Runtime.CSharpArgumentInfo.Create(Runtime.CSharpArgumentInfoFlags.None, null) });
                return GetDynamicPropertyExpression(Expression.Dynamic(binder, typeof(object), expression), items[1]);
            }
            else
            {
                var binder = Runtime.Binder.GetMember(Runtime.CSharpBinderFlags.None, name, typeof(Utils), new Runtime.CSharpArgumentInfo[] { Runtime.CSharpArgumentInfo.Create(Runtime.CSharpArgumentInfoFlags.None, null) });
                return Expression.Dynamic(binder, typeof(object), expression);
            }
        }


        public static SelectList GetWeekDayItems()
        {
            //Resources.Glossary.ResourceManager.GetString(Enum.GetName(typeof(DayOfWeek), (value + 6) % 7)) })
            return new SelectList(new int[] { 0, 1, 2, 3, 4, 5, 6 }.Select(value => new
            {
                Value = value,
                Name = Enum.GetName(typeof(DayOfWeek), value).ToString()
            }),
                "Value", "Name");
        }

        public static string GenerateRandomText(int length)
        {
            Random rand = new Random();
            int order;
            StringBuilder builder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                order = rand.Next(Constants.CharacterResource.Length);
                if (Constants.CharacterResource[order] == ' ')
                {
                    builder.Append(rand.Next(8) + 1);
                }
                else
                {
                    builder.Append(Constants.CharacterResource[order]);
                }
            }
            return builder.ToString();
        }

    }
}