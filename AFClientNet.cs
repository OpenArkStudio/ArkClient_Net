using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace AFTCPClient
{
    public enum AFTCPClientState
    {
        Connecting,
        Connected,
        Disconnected
    }
	
	public enum AFTCPEventType
    {
        None,
        Connected,
        Disconnected,
        ConnectionRefused,
        DataReceived
    }

    public class AFTCPEvent
    {
        public AFTCPEventType Type;
        // Type == DataReceived
        public AFSocketPacket Data;
        public AFTCPEvent(AFTCPEventType t, AFSocketPacket data = null)
        {
            Type = t;
            Data = data;
        }
    }

    public class AFSocketPacket
    {
        public byte[] bytes = null;
        public int bytesCount = 0;

        public AFSocketPacket(byte[] bytes, int bytesCount)
        {
            this.bytes = bytes;
            this.bytesCount = bytesCount;
        }

    }

    public class AFTCPEventParams
    {
        public AFClientNet client = null;
        public int clientID = 0;
        public TcpClient socket = null;
        public AFTCPEventType eventType = AFTCPEventType.None;
        public string message = "";
        public AFSocketPacket packet = null;

    }

    class StructureTransform
    {
        static bool bBig = false;//defalut little
        public static void Reverse(byte[] msg)
        {
            if (!bBig)
            {
                Array.Reverse(msg);
            }
        }

        public static void Reverse(byte[] msg, int nOffest, int nSize)
        {
            if (!bBig)
            {
                Array.Reverse(msg, nOffest, nSize);
            }
        }


        public static bool SetEndian(bool bIsBig)
        {
            bBig = bIsBig;
            return bBig;
        }

        public static void ByteArrayToStructureEndian(byte[] bytearray, ref object obj, int startoffset)
        {
            int len = Marshal.SizeOf(obj);
            IntPtr i = Marshal.AllocHGlobal(len);
            byte[] temparray = (byte[])bytearray.Clone();
          
            obj = Marshal.PtrToStructure(i, obj.GetType());
           
            object thisBoxed = obj;
            Type test = thisBoxed.GetType();
            int reversestartoffset = startoffset;
            
            foreach (var field in test.GetFields())
            {
                object fieldValue = field.GetValue(thisBoxed); // Get value

                TypeCode typeCode = Type.GetTypeCode(fieldValue.GetType());  //Get Type
                if (typeCode != TypeCode.Object)  //Èç¹ûÎªÖµÀàÐÍ
                {
                    Reverse(temparray, reversestartoffset, Marshal.SizeOf(fieldValue));
                    reversestartoffset += Marshal.SizeOf(fieldValue);
                }
                else 
                {
                    reversestartoffset += ((byte[])fieldValue).Length;
                }
            }
            try
            {
                Marshal.Copy(temparray, startoffset, i, len);
            }
            catch (Exception ex) { Console.WriteLine("ByteArrayToStructure FAIL: error " + ex.ToString()); }
            obj = Marshal.PtrToStructure(i, obj.GetType());
            Marshal.FreeHGlobal(i);  
        }

        public static byte[] StructureToByteArrayEndian(object obj)
        {
            object thisBoxed = obj; 
            Type test = thisBoxed.GetType();

            int offset = 0;
            byte[] data = new byte[Marshal.SizeOf(thisBoxed)];

            object fieldValue;
            TypeCode typeCode;
            byte[] temp;
            
            foreach (var field in test.GetFields())
            {
                fieldValue = field.GetValue(thisBoxed); // Get value

                typeCode = Type.GetTypeCode(fieldValue.GetType());  // get type

                switch (typeCode)
                {
                    case TypeCode.Single: // float
                        {
                            temp = BitConverter.GetBytes((Single)fieldValue);
                            StructureTransform.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Single));
                            break;
                        }
                    case TypeCode.Int32:
                        {
                            temp = BitConverter.GetBytes((Int32)fieldValue);
                            StructureTransform.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Int32));
                            break;
                        }
                    case TypeCode.UInt32:
                        {
                            temp = BitConverter.GetBytes((UInt32)fieldValue);
                            StructureTransform.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(UInt32));
                            break;
                        }
                    case TypeCode.Int16:
                        {
                            temp = BitConverter.GetBytes((Int16)fieldValue);
                            StructureTransform.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Int16));
                            break;
                        }
                    case TypeCode.UInt16:
                        {
                            temp = BitConverter.GetBytes((UInt16)fieldValue);
                            StructureTransform.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(UInt16));
                            break;
                        }
                    case TypeCode.Int64:
                        {
                            temp = BitConverter.GetBytes((Int64)fieldValue);
                            StructureTransform.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Int64));
                            break;
                        }
                    case TypeCode.UInt64:
                        {
                            temp = BitConverter.GetBytes((UInt64)fieldValue);
                            StructureTransform.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(UInt64));
                            break;
                        }
                    case TypeCode.Double:
                        {
                            temp = BitConverter.GetBytes((Double)fieldValue);
                            StructureTransform.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Double));
                            break;
                        }
                    case TypeCode.Byte:
                        {
                            data[offset] = (Byte)fieldValue;
                            break;
                        }
                    default:
                        {
                            //System.Diagnostics.Debug.Fail("No conversion provided for this type : " + typeCode.ToString());
                            break;
                        }
                }; // switch
                if (typeCode == TypeCode.Object)
                {
                    int length = ((byte[])fieldValue).Length;
                    Array.Copy(((byte[])fieldValue), 0, data, offset, length);
                    offset += length;
                }
                else
                {
                    offset += Marshal.SizeOf(fieldValue);
                }
            } // foreach

            return data;
        } // Swap
    };
	
    public class AFClientNet
    {
        public AFCNet net = null;

        public AFClientNet(AFCNet xnet)
        {
            net = xnet;
            Init();
        }

        void Init()
        {
            mxState = AFTCPClientState.Disconnected;
            mxEventQueue = new Queue<AFTCPEvent>();
        }
        // MonoBehaviour
        private int bufferSize = 65536;

        private AFTCPClientState mxState;
        private NetworkStream mxStream;
        private StreamWriter mxWriter;
        private StreamReader mxReader;
        private Thread mxReadThread;
        private TcpClient mxClient;


        private Queue<AFTCPEvent> mxEventQueue;

        public bool IsConnected()
        {
            return mxState == AFTCPClientState.Connected;
        }

        public AFTCPClientState GetState()
        {
            return mxState;
        }

        public void Update()
        {
            AFTCPEvent[] eventArray = null;
            lock(mxEventQueue)
            {
                eventArray = mxEventQueue.ToArray();
                mxEventQueue.Clear();
            }

            if (eventArray != null && eventArray.Length > 0)
            {
                for (int i = 0; i < eventArray.Length; i++)
                {
                    var ev = eventArray[i];
                    AFTCPEventParams eventParams = new AFTCPEventParams();
                    eventParams.eventType = ev.Type;
                    eventParams.client = this;
                    eventParams.socket = mxClient;
                    eventParams.packet = ev.Data;

                    switch (ev.Type)
                    {
                        case AFTCPEventType.Connected:
                            OnClientConnect(eventParams);
                            break;
                        case AFTCPEventType.Disconnected:
                            OnClientDisconnect(eventParams);
                            break;
                        case AFTCPEventType.ConnectionRefused:
                            break;
                        case AFTCPEventType.DataReceived:
                            OnDataReceived(eventParams);
                            break;
                        default:
                            break;

                    }
                }
            }
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {

                TcpClient tcpClient = (TcpClient)ar.AsyncState;
                tcpClient.EndConnect(ar);

                SetTcpClient(tcpClient);

            }
            catch (Exception e)
            {
                e.ToString();
                lock(mxEventQueue)
                {
                    mxEventQueue.Enqueue(new AFTCPEvent(AFTCPEventType.ConnectionRefused));
                }
            }

        }

        private void ReadData()
        {
            bool endOfStream = false;

            while (!endOfStream)
            {
               int bytesRead = 0;
               byte[] bytes = new byte[bufferSize];
               try
               {
                   bytesRead = mxStream.Read(bytes, 0, bufferSize);
               }
               catch (Exception e)
               {
                   e.ToString();
               }

               if (bytesRead == 0)
               {

                   endOfStream = true;

               }
               else
               {
                   lock(mxEventQueue)
                   {
                        mxEventQueue.Enqueue(new AFTCPEvent(AFTCPEventType.DataReceived, new AFSocketPacket(bytes, bytesRead)));
                   }
               }
            }
            mxClient.Close();
            lock(mxEventQueue)
            {
                mxEventQueue.Enqueue(new AFTCPEvent(AFTCPEventType.Disconnected));
            }

        }

        // Public
        public void Connect(string hostname, int port)
        {
            if (mxState == AFTCPClientState.Connected)
            {
                return;
            }

            mxState = AFTCPClientState.Connecting;


            mxEventQueue.Clear();

            mxClient = new TcpClient();

            mxClient.BeginConnect(hostname,
                                 port,
                                 new AsyncCallback(ConnectCallback),
                                 mxClient);

        }

        public void Disconnect()
        {

            mxState = AFTCPClientState.Disconnected;

            try { if (mxReader != null) mxReader.Close(); }
            catch (Exception e) { e.ToString(); }
            try { if (mxWriter != null) mxWriter.Close(); }
            catch (Exception e) { e.ToString(); }
            try { if (mxClient != null) mxClient.Close(); }
            catch (Exception e) { e.ToString(); }

        }

        public void SendBytes(byte[] bytes)
        {
            SendBytes(bytes, 0, bytes.Length);
        }

        public void SendBytes(byte[] bytes, int offset, int size)
        {

            if (!IsConnected())
                return;

            mxStream.Write(bytes, offset, size);
        }

        public void SetTcpClient(TcpClient tcpClient)
        {
            mxClient = tcpClient;

            if (mxClient.Connected)
            {
                mxStream = mxClient.GetStream();
                mxReader = new StreamReader(mxStream);
                mxWriter = new StreamWriter(mxStream);

                mxState = AFTCPClientState.Connected;
                lock(mxEventQueue)
                {
                    mxEventQueue.Enqueue(new AFTCPEvent(AFTCPEventType.Connected));
                }
                mxReadThread = new Thread(ReadData);
                mxReadThread.IsBackground = true;
                mxReadThread.Start();
            }
            else
            {
                mxState = AFTCPClientState.Disconnected;
            }
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////////////////Listener
        /// </summary>
        private UInt32 mnPacketSize = 0;
        private byte[] mPacket = new byte[ConstDefine.MAX_PACKET_LEN];

        public void OnClientConnect(AFTCPEventParams eventParams)
        {
            net.OnConnect();
        }

        public void OnClientDisconnect(AFTCPEventParams eventParams)
        {
            if (IsConnected())
            {
                Disconnect();
            }

            net.OnDisConnect();

        }

        public void OnClientConnectionRefused(AFTCPEventParams eventParams)
        {
            net.Log("Client refused");
        }

        public void OnDataReceived(AFTCPEventParams eventParams)
        {
            byte[] bytes = eventParams.packet.bytes;
            int bytesCount = eventParams.packet.bytesCount;
            UInt32 left = (UInt32)bytesCount;

            net.Log("OnDataReceived:" + mnPacketSize + "|" + bytesCount);
            while (left > 0)
            {
                UInt32 copyCount = left < (ConstDefine.MAX_PACKET_LEN - mnPacketSize) ? left : (ConstDefine.MAX_PACKET_LEN - mnPacketSize);
                Array.Copy(bytes, bytesCount - left, mPacket, mnPacketSize, copyCount);
                mnPacketSize += copyCount;
                left -= copyCount;
                OnDataReceived();
            }


        }

        void OnDataReceived()
        {
            UInt32 left = mnPacketSize;
            while(left >= ConstDefine.AF_PACKET_HEAD_SIZE)
            {
                object structType = new MsgHead();
                byte[] headBytes = new byte[Marshal.SizeOf(structType)];

                Array.Copy(mPacket, mnPacketSize-left, headBytes, 0, Marshal.SizeOf(structType));
                StructureTransform.ByteArrayToStructureEndian(headBytes, ref structType, 0);
                MsgHead head = (MsgHead)structType;

                if (head.unDataLen >= left)
                {
                    byte[] body_head = new byte[head.unDataLen];
                    Array.Copy(mPacket, mnPacketSize-left, body_head, 0, head.unDataLen);
                    left -= head.unDataLen;
                    if (false == OnDataReceived(this, body_head, head.unDataLen))
                    {
                        OnClientDisconnect(new AFTCPEventParams());
                    }
                } 
                else
                {
                    Array.Copy(mPacket, mnPacketSize - left, mPacket, 0, left);
                    break;
                }
            }
            mnPacketSize = left;
        }

        bool OnDataReceived(AFClientNet client, byte[] bytes, UInt32 bytesCount)
        {
            if (bytes.Length == bytesCount)
            {
                object structType = new MsgHead();
                StructureTransform.ByteArrayToStructureEndian(bytes, ref structType, 0);
                MsgHead head = (MsgHead)structType;

                Int32 nBodyLen = (Int32)bytesCount - (Int32)ConstDefine.AF_PACKET_HEAD_SIZE;
                if (nBodyLen >= 0)
                {
                    byte[] body = new byte[nBodyLen];
                    Array.Copy(bytes, ConstDefine.AF_PACKET_HEAD_SIZE, body, 0, nBodyLen);

                    client.net.OnMessageEvent(head, body);
                    return true;
                }
                else
                {
                }
            }

            return false;
        }

    }
}