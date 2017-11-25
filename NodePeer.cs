using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExitGames.Client.Photon;
using itfantasy.nodepeer.nets;
using itfantasy.nodepeer.nets.ws;
using itfantasy.nodepeer.gnbuffers;

namespace itfantasy.nodepeer
{
    public class NodePeer : PhotonPeer, INetEventListener
    {
        INetWorker netWorker;
        ConnectionProtocol protocolType;

        public NodePeer(ConnectionProtocol protocolType)
            : base(protocolType)
        {
            this.protocolType = protocolType;
        }

        public NodePeer(IPhotonPeerListener listener, ConnectionProtocol protocolType)
            : base(listener, protocolType)
        {
            this.protocolType = protocolType;
        }

        public override bool Connect(string serverAddress, string applicationName, object custom)
        {
            var proto = this.protocolToString(this.protocolType);
            var err = this.InitNetWorker(proto, serverAddress);
            if (err != errors.nil)
            {
                this.OnError(err);
                return false;
            }
            this.netWorker.Connect(proto + "://" + serverAddress, applicationName);
            return true;
        }

        public override bool Connect(string serverAddress, string applicationName)
        {
            var proto = this.protocolToString(this.protocolType);
            var err = this.InitNetWorker(proto, serverAddress);
            if (err != errors.nil)
            {
                this.OnError(err);
                return false;
            }
            this.netWorker.Connect(proto + "://" + serverAddress, applicationName);
            return true;
        }

        public override void Disconnect()
        {
            this.netWorker.Close();
        }

        public override void Service()
        {
            this.netWorker.Update();
        }

        public override bool OpCustom(byte customOpCode, Dictionary<byte, object> customOpParameters, bool sendReliable)
        {
            return this.OpCustom(customOpCode, customOpParameters, sendReliable, 0);
        }

        public override bool OpCustom(byte customOpCode, Dictionary<byte, object> customOpParameters, bool sendReliable, byte channelId)
        {
            return this.OpCustom(customOpCode, customOpParameters, sendReliable, channelId, false);
        }

        public override bool OpCustom(byte customOpCode, Dictionary<byte, object> customOpParameters, bool sendReliable, byte channelId, bool encrypt)
        {
            var buffer = new GnBuffer(1024);
            buffer.PushByte(customOpCode);
            foreach (KeyValuePair<byte, object> kv in customOpParameters)
            {
                buffer.PushByte(kv.Key);
                Type valType = kv.Value.GetType();
                if (valType == typeof(byte))
                {
                    buffer.PushByte((byte)kv.Value);
                }
                else if (valType == typeof(int))
                {
                    buffer.PushInt((int)kv.Value);
                }
                else if (valType == typeof(long))
                {
                    buffer.PushLong((long)kv.Value);
                }
                else if (valType == typeof(string))
                {
                    buffer.PushString(kv.Value.ToString());
                }
            }
            this.netWorker.SendAsync(buffer.Bytes());
            return true;
        }

        private error InitNetWorker(string proto, string serverAddress)
        {
            try
            {
                if (proto == "ws")
                {
                    this.netWorker = new WSNetWorker();
                    this.netWorker.BindEventListener(this);
                    return errors.nil;
                }
            }
            catch (Exception e)
            {
                return errors.New(e.Message);
            }
            return errors.New("unknown net protocol!!");
        }

        public void OnConn()
        {
            this.Listener.OnStatusChanged(StatusCode.Connect);
        }

        public void OnMsg(byte[] msg)
        {
            var parser = new GnParser(msg, 0);
            byte sign = parser.Byte();
            if (sign == 0) // response
            {
                OperationResponse response = new OperationResponse();
                response.OperationCode = parser.Byte();
                response.ReturnCode = parser.Short();

                while (!parser.OverFlow())
                {
                    byte key = parser.Byte();
                    byte type = parser.Byte();
                    switch (type)
                    {
                        case 1: // byte
                            response.Parameters[key] = parser.Byte();
                            break;
                        case 2: // short
                            response.Parameters[key] = parser.Short();
                            break;
                        case 4: // int
                            response.Parameters[key] = parser.Int();
                            break;
                        case 8: // long
                            response.Parameters[key] = parser.Long();
                            break;
                        case 99: // string
                            response.Parameters[key] = parser.String();
                            break;
                    }
                }
                Listener.OnOperationResponse(response);
            }
            else if (sign == 1) // event
            {
                EventData eventData = new EventData();
                eventData.Code = parser.Byte();
                while (!parser.OverFlow())
                {
                    byte key = parser.Byte();
                    byte type = parser.Byte();
                    switch (type)
                    {
                        case 1: // byte
                            eventData.Parameters[key] = parser.Byte();
                            break;
                        case 2: // short
                            eventData.Parameters[key] = parser.Short();
                            break;
                        case 4: // int
                            eventData.Parameters[key] = parser.Int();
                            break;
                        case 8: // long
                            eventData.Parameters[key] = parser.Long();
                            break;
                        case 99: // string
                            eventData.Parameters[key] = parser.String();
                            break;
                    }
                }
                Listener.OnEvent(eventData);
            }
        }

        public void OnClose()
        {
            Listener.OnStatusChanged(StatusCode.Disconnect);
        }

        public void OnError(error err)
        {
            Listener.DebugReturn(DebugLevel.ERROR, err.Error());
        }

        private string protocolToString(ConnectionProtocol protocol)
        {
            switch (protocol)
            {
                case ConnectionProtocol.Udp:
                    return "udp";
                case ConnectionProtocol.Tcp:
                    return "tcp";
                case ConnectionProtocol.WebSocket:
                    return "ws";
            }
            return "";
        }
    }
}
