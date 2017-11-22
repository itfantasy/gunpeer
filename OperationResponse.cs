using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace itfantasy.nodepeer
{
    public class OperationResponse
    {
        public string DebugMessage;
        
        public byte OperationCode;
        
        public Dictionary<byte, object> Parameters;
        
        public short ReturnCode;

        public OperationResponse()
        {
            this.Parameters = new Dictionary<byte, object>();
        }

        public object this[byte parameterCode]
        {
            get
            {
                return Parameters[parameterCode];
            }
            set
            {
                Parameters[parameterCode] = value;
            }
        }

        public override string ToString()
        {
            return "OperationResponse";
        }
        
        public string ToStringFull()
        {
            return "";
        }
    }
}
