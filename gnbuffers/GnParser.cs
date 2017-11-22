using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace itfantasy.nodepeer.gnbuffers
{
    public class GnParser
    {
        byte[] buffer;
        int offset;

        public GnParser(byte[] buffer, int offset)
        {
            this.buffer = buffer;
            this.offset = offset;
        }

        public byte Byte()
        {
            return this.buffer[this.offset++];
        }

        public short Short()
        {
            short ret = BitConverter.ToInt16(this.buffer, this.offset);
            this.offset += 2;
            return ret;
        }

        public int Int()
        {
            int ret = BitConverter.ToInt32(this.buffer, this.offset);
            this.offset += 4;
            return ret;
        }

        public long Long()
        {
            long ret = BitConverter.ToInt64(this.buffer, this.offset);
            this.offset += 8;
            return ret;
        }

        public string String()
        {
            int length = this.Int();
            string ret = BitConverter.ToString(this.buffer, this.offset, length);
            this.offset += length;
            return ret;
        }

        public bool OverFlow()
        {
            return this.offset >= this.buffer.Length;   
        }
    }
}
