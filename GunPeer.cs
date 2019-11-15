#define GUN_SDK
//#define FULL_LOG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ExitGames.Client.Photon;
using itfantasy.gun;
using itfantasy.gun.nets;
#if UNITY_EDITOR
using itfantasy.gun.nets.ws;
#endif
using itfantasy.gun.nets.kcp;
using itfantasy.gun.nets.tcp;
using itfantasy.gun.core.binbuf;

using Types = itfantasy.gun.core.binbuf.Types;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace itfantasy.gunpeer
{
    public class GunPeer : PhotonPeer, INetEventListener
    {

        INetWorker netWorker;
        ConnectionProtocol protocolType;

        StatusCode curStatus;
        StatusCode lstStatus;

        public GunPeer(ConnectionProtocol protocolType)
            : base(protocolType)
        {
#if GUN_SDK
            this.protocolType = protocolType;
            this.ExtendsUnityTypes();
#endif
        }

        public GunPeer(IPhotonPeerListener listener, ConnectionProtocol protocolType)
            : base(listener, protocolType)
        {
#if GUN_SDK
            this.protocolType = protocolType;
#endif
        }

        public override bool Connect(string serverAddress, string applicationName, object custom)
        {
#if GUN_SDK
            if (applicationName == "")
            {
                applicationName = "lobby";
            }
            var proto = this.protocolToString(this.protocolType);
            var err = this.InitNetWorker(proto, serverAddress);
            if (!err.nil)
            {
                this.OnError(err);
                return false;
            }
            this.netWorker.Connect(proto + "://" + serverAddress);
            return true;
#else
            return base.Connect(serverAddress, applicationName, custom);
#endif
        }

        public override bool Connect(string serverAddress, string applicationName)
        {
#if GUN_SDK
            var proto = this.protocolToString(this.protocolType);
            var err = this.InitNetWorker(proto, serverAddress);
            if (!err.nil)
            {
                this.OnError(err);
                return false;
            }
            this.netWorker.Connect(proto + "://" + serverAddress);
            return true;
#else
            return base.Connect(serverAddress, applicationName);
#endif
        }

        public override void Disconnect()
        {
#if GUN_SDK
            this.netWorker.Close();
#else
            base.Disconnect();
#endif
        }

        public bool EstablishEncryption()
        {
#if GUN_SDK
            this.Listener.OnStatusChanged(StatusCode.EncryptionEstablished);
            return true;
#else
            return base.EstablishEncryption();
#endif
        }

        public override void Service()
        {
#if GUN_SDK
            this.service();
#else
            base.Service();
#endif
        }

        private bool service()
        {
            bool ret = this.netWorker.Update();
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
            return ret;
        }

        public override bool SendOutgoingCommands()
        {
#if GUN_SDK
            return this.service();
#else
            return base.SendOutgoingCommands();
#endif
        }

        public override bool DispatchIncomingCommands()
        {
#if GUN_SDK
            return this.service();
#else
            return base.DispatchIncomingCommands();
#endif
        }

        public override bool OpCustom(byte customOpCode, Dictionary<byte, object> customOpParameters, bool sendReliable)
        {
            LogCustomOp(customOpCode, customOpParameters);
#if GUN_SDK
            return this.OpCustom(customOpCode, customOpParameters, sendReliable, 0);
#else
            return base.OpCustom(customOpCode, customOpParameters, sendReliable);
#endif
        }

        public override bool OpCustom(byte customOpCode, Dictionary<byte, object> customOpParameters, bool sendReliable, byte channelId)
        {
            LogCustomOp(customOpCode, customOpParameters);
#if GUN_SDK
            return this.OpCustom(customOpCode, customOpParameters, sendReliable, channelId, false);
#else
            return base.OpCustom(customOpCode, customOpParameters, sendReliable, channelId);
#endif
        }

        public override bool OpCustom(byte customOpCode, Dictionary<byte, object> customOpParameters, bool sendReliable, byte channelId, bool encrypt)
        {
            LogCustomOp(customOpCode, customOpParameters);
#if GUN_SDK
            if (customOpCode == 253)
            {
                if (!customOpParameters.ContainsKey(246))
                {
                    customOpParameters[246] = (byte)0;
                }
                if (!customOpParameters.ContainsKey(247))
                {
                    customOpParameters[247] = (byte)0;
                }
            }
            var buffer = new BinBuffer(1024);
            buffer.PushByte(customOpCode);
            if (customOpParameters != null)
            {
                object eventData = null;
                foreach (KeyValuePair<byte, object> kv in customOpParameters)
                {
                    if (kv.Key == 245)
                    {
                        eventData = kv.Value;
                    }
                    else
                    {
                        buffer.PushByte(kv.Key);
                        buffer.PushObject(kv.Value);
                    }
                }
                if (eventData != null)
                {
                    buffer.PushByte(245);
                    buffer.PushObject(eventData);
                }
            }
            this.netWorker.SendAsync(buffer.Bytes());
            return true;
#else
            return base.OpCustom(customOpCode, customOpParameters, sendReliable, channelId, encrypt);
#endif
        }

        private error InitNetWorker(string proto, string serverAddress)
        {
            try
            {
                if (proto == "kcp")
                {
                    this.netWorker = new KcpNetWorker();
                    this.netWorker.BindEventListener(this);
                    return errors.nil;
                }
                else if (proto == "tcp")
                {
                    this.netWorker = new TcpNetWorker();
                    this.netWorker.BindEventListener(this);
                    return errors.nil;
                }
                else if (proto == "ws")
                {
#if UNITY_EDITOR
                    this.netWorker = new WSNetWorker();
                    this.netWorker.BindEventListener(this);
                    return errors.nil;
#else
                    return errors.New("不支持的协议...");
#endif
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
            var parser = new BinParser(msg, 0);
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

        public void OnClose(error reason)
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
            Types.BinExtendCustomType(typeof(Vector2), (byte)'W', (buf, obj) =>
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

            Types.BinExtendCustomType(typeof(Vector3), (byte)'V', (buf, obj) =>
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

            Types.BinExtendCustomType(typeof(Quaternion), (byte)'Q', (buf, obj) =>
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

#if UNITY_EDITOR
            Types.BinExtendCustomType(typeof(PhotonPlayer), (byte)'P', (buf, obj) =>
            {
                PhotonPlayer val = (PhotonPlayer)obj;
                buf.PushInt(val.ID);
            }, (parser) =>
            {
                int ID = parser.Int();
                if (PhotonNetwork.networkingPeer.mActors.ContainsKey(ID))
                {
                    return PhotonNetwork.networkingPeer.mActors[ID];
                }
                else
                {
                    return null;
                }
            });
#endif
        }

        public static void LogCustomOp(byte customOpCode, Dictionary<byte, object> customOpParameters)
        {
#if FULL_LOG
            Debug.Log("==============> Sending a message...");
#endif
            Debug.Log("customOpCode:" + customOpCode.ToString());
#if FULL_LOG
            Debug.Log("customOpParameters:");
            try
            {
                Debug.Log(JsonConvert.SerializeObject(customOpParameters));
            }
            catch
            {
                Debug.Log("some values...");
            }
#endif
        }

        public static void LogOperationResponse(OperationResponse operationResponse)
        {
#if FULL_LOG
            Debug.Log("==============> Receiving a Response...");
#endif
            Debug.Log("OperationCode:" + operationResponse.OperationCode.ToString());
            Debug.Log("ReturnCode:" + operationResponse.ReturnCode.ToString());
#if FULL_LOG
            Debug.Log("operationResponseParameters:");
            try
            {
                Debug.Log(JsonConvert.SerializeObject(operationResponse.Parameters));
            }
            catch
            {
                Debug.Log("some values...");
            }
#endif
        }

        public static void LogEventData(EventData eventData)
        {
#if FULL_LOG
            Debug.Log("==============> Receiving a Event...");
#endif
            Debug.Log("eventDataCode:" + eventData.Code.ToString());
#if FULL_LOG
            Debug.Log("eventDataParameters:");
            try
            {
                Debug.Log(JsonConvert.SerializeObject(eventData.Parameters));
            }
            catch
            {
                Debug.Log("some values...");
            }
#endif
        }
    }
}
