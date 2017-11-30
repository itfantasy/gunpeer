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
            string ret = System.Text.Encoding.UTF8.GetString(this.buffer, this.offset, length);
            this.offset += length;
            return ret;
        }

        public Dictionary<object, object> Hash()
        {
            int length = this.Int();
            Dictionary<object, object> hash = new Dictionary<object, object>();
            for (int i = 0; i < length; i++)
            {
                object k = this.Object();
                object v = this.Object();
                hash[k] = v;
            }
            return hash;
        }

        public int[] IntArray()
        {
            int length = this.Int();
            int[] array = new int[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = length;
            }
            return array;
        }

        public object Object()
        {
            char c = (char)this.Byte();
            switch (c)
            {
                case 'b':
                    return this.Byte();
                case 't':
                    return this.Short();
                case 'i':
                    return this.Int();
                case 'l':
                    return this.Long();
                case 's':
                    return this.String();
                case 'H':
                    return this.Hash();
                case 'I':
                    return this.IntArray();
            }
            return null;
        }

        public bool OverFlow()
        {
            return this.offset >= this.buffer.Length;   
        }
    }
}
