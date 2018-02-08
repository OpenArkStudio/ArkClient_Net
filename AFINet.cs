using System.Collections;
using AFTCPClient;
using System;
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
public class ConstDefine
{
    public static UInt32 AF_PACKET_HEAD_SIZE = 22;
    public static UInt32 MAX_PACKET_LEN = 655360;
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class MsgHead
{
    public MsgHead()
    {
        unMsgID = 0;
        unDataLen = 0;
    }
    public UInt16 unMsgID;
    public UInt32 unDataLen;
    public Int64 nHead64;
    public Int64 nData64;
};

public abstract class AFINet
{
    public delegate void MsgDelegation(MsgHead head, MemoryStream stream);
    public delegate void ResultCodeDelegation(int eGameEventCode);

    public delegate void OnConnectDelegation();
    public delegate void OnDisConnectDelegation();

    public abstract void StartConnect(string strIP, int nPort);
    public abstract void SendMsg(MsgHead head, byte[] bodyByte);
    public abstract void Update();
    public abstract void Disconnect();
    public abstract bool RegisteredDelegation(int eMsgID, MsgDelegation msgDelegate);
    public abstract bool RegisteredResultCodeDelegation(int eCode, ResultCodeDelegation msgDelegate);
    public abstract bool RegisteredDisConnectDelegation(OnDisConnectDelegation onDisConnectDelegate);
    public abstract bool RegisteredConnectDelegation(OnConnectDelegation onConnectDelegate);
    public abstract bool DoResultCodeDelegation(int eCode);
}
}