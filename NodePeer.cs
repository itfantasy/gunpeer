using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ExitGames.Client.Photon;
using itfantasy.gun;
using itfantasy.gun.nets;
using itfantasy.gun.nets.ws;
using itfantasy.gun.nets.kcp;
using itfantasy.gun.gnbuffers;

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
            this.ExtendsUnityTypes();
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
            this.netWorker.Update();
            if (curStatus != lstStatus)
            {
                bool setted = false;
                if (curStatus == StatusCode.Disconnect)
                {
                    lstStatus = curStatus;
                    setted = true;
                }
                this.Listener.OnStatusChanged(curStatus);
                if (!setted)
                {
                    lstStatus = curStatus;
                }
            }
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
                else if (proto == "kcp")
                {
                    this.netWorker = new KcpNetWorker();
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

                response.ReturnCode = parser.Short();
                response.OperationCode = parser.Byte();

                while (!parser.OverFlow())
                {
                    byte key = parser.Byte();
                    object value = parser.Object();
                    if (value.GetType() == typeof(Dictionary<object, object>))
                    {
                        response.Parameters[key] = DictToHashtable(value as Dictionary<object, object>);
                    }
                    else
                    {
                        response.Parameters[key] = value;
                    }
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
                    object value = parser.Object();
                    if (value.GetType() == typeof(Dictionary<object, object>))
                    {
                        eventData[key] = DictToHashtable(value as Dictionary<object, object>);
                    }
                    else
                    {
                        eventData[key] = value;
                    }
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
                    return "kcp";
                case ConnectionProtocol.Tcp:
                    return "tcp";
                case ConnectionProtocol.WebSocket:
                    return "ws";
            }
            return "";
        }

        private Hashtable DictToHashtable(Dictionary<object, object> dict)
        {
            Hashtable hash = new Hashtable();
            foreach (KeyValuePair<object, object> kv in dict)
            {
                hash[kv.Key] = kv.Value;
            }
            return hash;
        }

        private void ExtendsUnityTypes()
        {
            GnTypes.GnExtendCustomType(typeof(Vector2), (byte)'W', (buf, obj) =>
            {
                Vector2 val = (Vector2)obj;
                buf.PushFloat(val.x);
                buf.PushFloat(val.y);
            }, (parser) =>
            {
                Vector2 val = new Vector2();
                val.x = parser.Float();
                val.y = parser.Float();
                return val;
            });

            GnTypes.GnExtendCustomType(typeof(Vector3), (byte)'V', (buf, obj) =>
            {
                Vector3 val = (Vector3)obj;
                buf.PushFloat(val.x);
                buf.PushFloat(val.y);
                buf.PushFloat(val.z);
            }, (parser) =>
            {
                Vector3 val = new Vector3();
                val.x = parser.Float();
                val.y = parser.Float();
                val.z = parser.Float();
                return val;
            });

            GnTypes.GnExtendCustomType(typeof(Quaternion), (byte)'Q', (buf, obj) =>
            {
                Quaternion val = (Quaternion)obj;
                buf.PushFloat(val.w);
                buf.PushFloat(val.x);
                buf.PushFloat(val.y);
                buf.PushFloat(val.z);
            }, (parser) =>
            {
                Quaternion val = new Quaternion();
                val.w = parser.Float();
                val.x = parser.Float();
                val.y = parser.Float();
                val.z = parser.Float();
                return val;
            });
        }
    }
}
