using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientServerUsingNamedPipes.Utilities
{

    public class BufferReading
    {
        public readonly byte[] Buffer;
        public readonly StringBuilder StringBuilder;
        public int BufferSize { get; } = 2048;

        public BufferReading()
        {
            Buffer = new byte[BufferSize];
            StringBuilder = new StringBuilder();
        }
    }
}
