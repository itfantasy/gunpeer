using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace itfantasy.nodepeer
{
    public interface INodePeerListener
    {
        void DebugReturn(DebugLevel level, string message);
        void OnEvent(EventData eventData);
        void OnOperationResponse(OperationResponse operationResponse);
        void OnStatusChanged(StatusCode statusCode);
    }
    
    public enum DebugLevel
    {
        OFF = 0,
        ERROR = 1,
        WARNING = 2,
        INFO = 3,
        ALL = 5,
    }

    public enum StatusCode
    {
        SecurityExceptionOnConnect = 1022,
        ExceptionOnConnect = 1023,
        Connect = 1024,
        Disconnect = 1025,
        Exception = 1026,
        SendError = 1030,
        ExceptionOnReceive = 1039,
        TimeoutDisconnect = 1040,
        DisconnectByServer = 1041,
        DisconnectByServerUserLimit = 1042,
        DisconnectByServerLogic = 1043,
        EncryptionEstablished = 1048,
        EncryptionFailedToEstablish = 1049,
    }
}
