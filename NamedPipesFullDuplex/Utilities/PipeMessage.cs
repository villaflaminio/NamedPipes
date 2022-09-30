using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace NamedPipesFullDuplex.Utilities
{

    [Serializable]
    public class PipeMessage
    {
        public string topic { get; set; }
        public string Message { get; set; }

        //constructor
        public PipeMessage()
        {

        }
        public PipeMessage(string topic, string message)
        {
            this.topic = topic;
            this.Message = message;
        }
        public byte[] Serialize()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(topic);
                    writer.Write(Message);
                }
                return m.ToArray();
            }
        }

        public static PipeMessage Deserialize(byte[] data)
        {
            PipeMessage result = new PipeMessage();

            using (MemoryStream m = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    result.topic = reader.ReadString();
                    result.Message = reader.ReadString();
                }
            }
            return result;
        }

        //tostring
        public override string ToString()
        {
            return string.Format("[topic: {0} , Message: {1}]", topic, Message);
        }
    }


}
