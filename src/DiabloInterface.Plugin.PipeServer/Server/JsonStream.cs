using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zutatensuppe.DiabloInterface.Plugin.PipeServer.Server
{
    public class JsonStreamReader
    {
        BinaryReader reader;
        Encoding encoding;

        public JsonStreamReader(Stream stream, Encoding encoding)
        {
            reader = new BinaryReader(stream);
            this.encoding = encoding;
        }

        string ReadJsonString()
        {
            int length = reader.ReadInt32();
            byte[] buffer = new byte[length];
            reader.Read(buffer, 0, length);
            return encoding.GetString(buffer);
        }

        public T ReadJson<T>()
        {
            return JsonConvert.DeserializeObject<T>(ReadJsonString());
        }
    }

    public class JsonStreamWriter
    {
        BinaryWriter writer;
        Encoding encoding;
        JsonConverter[] converters;

        public JsonStreamWriter(Stream stream, Encoding encoding, params JsonConverter[] converters)
        {
            writer = new BinaryWriter(stream);
            this.encoding = encoding;
            this.converters = converters;
        }

        public JsonStreamWriter(Stream stream) : this(stream, Encoding.UTF8, new IsoDateTimeConverter()) { }

        void WriteJsonString(string data)
        {
            var buffer = encoding.GetBytes(data);
            writer.Write(buffer.Length);
            writer.Write(buffer);
        }

        public void WriteJson(object json)
        {
            WriteJsonString(JsonConvert.SerializeObject(json, converters));
        }

        public void Flush()
        {
            writer.Flush();
        }
    }
}
