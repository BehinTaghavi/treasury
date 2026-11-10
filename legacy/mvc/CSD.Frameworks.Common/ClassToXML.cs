using System.Xml.Serialization;

namespace CSD.Common
{
    public class ClassToXML
    {
        public static string Serialize(object obj)
        {
            XmlSerializer ser = new XmlSerializer(obj.GetType());
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            System.IO.StringWriter writer = new System.IO.StringWriter(sb);
            ser.Serialize(writer, obj);
            return sb.ToString();
        }

        public static T Deserialize<T>(string xml) where T:class
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));            
            System.IO.StringReader reader = new System.IO.StringReader(xml);
            object obj = ser.Deserialize(reader);
            // Then you just need to cast obj into whatever type it is eg:
            T myObj = (T)obj;
            return myObj;
        }
    }
}
