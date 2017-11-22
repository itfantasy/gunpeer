using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace itfantasy.nodepeer
{
    public class EventData
    {
        public byte Code;
     
        public Dictionary<byte, object> Parameters;

        public EventData()
        {
            this.Code = 0;
            this.Parameters = new Dictionary<byte, object>();
        }
   
        public object this[byte key] 
        {
            get
            {
                return Parameters[key];
            }
            set
            {
                Parameters[key] = value;
            }
        }
        
        public override string ToString()
        {
            return "EventData";
        }
        
        public string ToStringFull()
        {
            return "";
        }
    }
}
