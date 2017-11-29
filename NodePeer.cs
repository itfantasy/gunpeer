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

        StatusCode curStatus;
        StatusCode lstStatus;

        public NodePeer(ConnectionProtocol protocolType)
            : base(protocolType)
        {
            this.protocolType = protocolType;
            this.protocolType = ConnectionProtocol.WebSocket;
        }

        public NodePeer(IPhotonPeerListener listener, ConnectionProtocol protocolType)
            : base(listener, protocolType)
        {
            this.protocolType = protocolType;
        }

        public override bool Connect(string serverAddress, string applicationName, object custom)
        {
            if (applicationName == "")
            {
                applicationName = "lobby";
            }
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

        public bool EstablishEncryption()
        {
            this.Listener.OnStatusChanged(StatusCode.EncryptionEstablished);
            return true;
        }

        public override void Service()
        {
            if (curStatus != lstStatus)
            {
                this.Listener.OnStatusChanged(curStatus);
                lstStatus = curStatus;
            }
            this.netWorker.Update();
        }

        public override bool SendOutgoingCommands()
        {
            Service();
            return false;
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
                buffer.PushObject(kv.Value);
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
            //this.Listener.OnStatusChanged(StatusCode.Connect);
            curStatus = StatusCode.Connect;
        }

        public void OnMsg(byte[] msg)
        {
            var parser = new GnParser(msg, 0);
            byte sign = parser.Byte();
            if (sign == 0) // response
            {
                OperationResponse response = new OperationResponse();
                response.Parameters = new Dictionary<byte, object>();

                response.OperationCode = parser.Byte();
                response.ReturnCode = parser.Short();

                while (!parser.OverFlow())
                {
                    byte key = parser.Byte();
                    response.Parameters[key] = parser.Object();
                }
                Listener.OnOperationResponse(response);
            }
            else if (sign == 1) // event
            {
                EventData eventData = new EventData();
                eventData.Parameters = new Dictionary<byte, object>();

                eventData.Code = parser.Byte();
                while (!parser.OverFlow())
                {
                    byte key = parser.Byte();
                    eventData.Parameters[key] = parser.Object();
                }
                Listener.OnEvent(eventData);
            }
        }

        public void OnClose()
        {
            //Listener.OnStatusChanged(StatusCode.Disconnect);
            curStatus = StatusCode.Disconnect;
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
