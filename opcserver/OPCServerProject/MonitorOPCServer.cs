﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace OPCServerProject
{
    class MonitorOPCServer
    {
        private static MonitorOPCServer server;
        public string ip;
        public int port;
        private Socket listener;
        public Thread listeningThread;
        public Thread sendCommandThread;
        public Thread clearResourceThread; 
        OPCServerUtil opc = new OPCServerUtil();
        public Dictionary<string, string> dbNameMapping = new Dictionary<string, string>();
        public Dictionary<string, LabelStructure> labels = new Dictionary<string, LabelStructure>(); 
        public Dictionary<DateTime, Socket> socketAll;
        public Dictionary<DateTime, Thread> threadAll;
        public Dictionary<string, Dictionary<string, uint>> handles = new Dictionary<string, Dictionary<string, uint>>(); 

        private MonitorOPCServer()
        {
        }

        public static MonitorOPCServer getInstance()
        {
            if (server == null)
                server = new MonitorOPCServer();
            return server;
        }

        private void loadInfo()
        {
            if (File.Exists("dbmapping.properties"))
            {
                string[] mappings = File.ReadAllLines("dbmapping.properties");
                for (int i = 0; i < mappings.Length; i++)
                {
                    string[] values = mappings[i].Split(',');
                    dbNameMapping.Add(values[0], values[1]);
                }
            }
            LabelStructure labelStructure = new LabelStructure();
            if (File.Exists("label.properties"))
            {
                string[] labels = File.ReadAllLines("label.properties");
                labelStructure.load(labels);
            }
            opc.registerOPCServer();
            handles = opc.addOPCLabels(labelStructure);
        }

        public void startMonitor(string ip,int port)
        {
            loadInfo();
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(50);
                //接收线程
                listeningThread = new Thread(new ParameterizedThreadStart(startListeningThread));
                listeningThread.Start(listener);
                //发送线程
                //sendCommandThread = new Thread(new ThreadStart(sendCommandWorkThread));
                //sendCommandThread.Start();
                //清理线程
               // clearResourceThread = new Thread(new ThreadStart(clearWorkThread));
               // clearResourceThread.Start();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("启动失败");
            }
        }

        public void shoutdownMonitor()
        {
        }

        private void startListeningThread(object serverSocket)
        {
            while (true)
            {
                Socket client = null;
                DateTime dt = DateTime.Now;
                try
                {
                    client = ((Socket)serverSocket).Accept();//把地址都接收进来，找到各个客户端
                    socketAll.Add(dt, client);
                }
                catch (Exception ex)
                {
                    break;
                }
                Thread receiveThread = new Thread(new ParameterizedThreadStart(ReceiveWorkThread));//调用接收数据线程方法
                receiveThread.Start(client);
                threadAll.Add(dt, receiveThread);
            }
        }

        private void ReceiveWorkThread(object obj)
        {
            Thread.CurrentThread.IsBackground = true;
            Socket socket = (Socket)obj;
            while (true)
            {
                try
                {
                    //接收数据
                    byte[] bytes_ = new byte[1024];
                    int length = 0;
                    try
                    {
                        length = socket.Receive(bytes_);
                        if (length <= 0)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        break;
                    }
                    //按规定长度存储
                    byte[] bytes = new byte[length];
                    for (int i = 0; i < length; i++)
                    {
                        bytes[i] = bytes_[i];
                    }
                    //检查粘包数据
                    List<byte[]> list = PacketUtil.CheckLong(length, bytes);
                    if (list.Count > 1)
                    {
                        string r = "";
                        for (int i = 0; i < length; i++)
                        {
                            r += string.Format("{0:X2}", bytes[i]);
                        }
                        LogUtil.writeLog(LogUtil.getFileName(), "[" + DateTime.Now.ToString() + "]:粘包数据是：<" + r + ">");
                    }
                    //数据处理
                    for (int kk = 0; kk < list.Count; kk++)
                    {
                        byte[] bytes1 = list[kk];
                        int length1 = bytes1.Length;
                        //先都转化为16进制字符
                        string result = "";
                        for (int i = 0; i < length1; i++)
                        {
                            result += string.Format("{0:X2}", bytes1[i]);
                        }
                        if (PacketUtil.CheckData(length1, bytes1))
                        {
                            if (result.StartsWith("68") && result.Substring(8, 2) == "00")
                            {
                                try
                                {
                                    //RTU连接数据 [68 00 xx 02 00 00 data1 cs 16]
                                    if (result.Substring(6, 2) == "02")
                                    {
                                        try
                                        {
                                            //解析从12开始的40个字节，作为data1的数据
                                            byte[] data1 = new byte[40];
                                            for (int i = 0; i < 40; i++)
                                            {
                                                data1[i] = bytes1[i + 6];
                                            }

                                            PacketData data1Packet = PacketData.resolveData1(data1);
                                            PacketUtil.savePacketContentToDb(data1Packet);
                                            opc.updateToOPCLabel(data1Packet,handles);
                                           
                                            //接收日志
                                            //LogUtil.writeLog(LogUtil.getFileName(), "[" + DateTime.Now.ToString() + "]:从RTU设备接收Modbus连接数据（Rtu=" + Rtu + "）成功；" + "接收地址:<" + socket.RemoteEndPoint.ToString() + ">，接收数据是：<" + result + ">");
                                                                                 
                                        }
                                        catch (Exception ex)
                                        {
                                            continue;
                                        }
                                    }
                                    //报警数据
                                    else if (result.Substring(6, 2) == "06")
                                    {
                                        try
                                        {
                                            //解析从12开始的40个字节，作为data1的数据
                                            byte[] data4 = new byte[40];
                                            for (int i = 0; i < 40; i++)
                                            {
                                                data4[i] = bytes1[i + 6];
                                            }
                                            
                                            PacketData data4Packet = PacketData.resolveData4(data4);
                                            PacketUtil.saveAlertPacketContentToDb(data4Packet);
                                            opc.updateAlertToOPCLabel(data4Packet,handles);

                                            //接收日志
                                            //LogUtil.writeLog(LogUtil.getFileName(), "[" + DateTime.Now.ToString() + "]:从RTU设备接收Modbus连接数据（Rtu=" + Rtu + "）成功；" + "接收地址:<" + socket.RemoteEndPoint.ToString() + ">，接收数据是：<" + result + ">");

                                        }
                                        catch (Exception ex)
                                        {
                                            continue;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            LogUtil.writeLog(LogUtil.getFileName(), "[" + DateTime.Now.ToString() + "]:" + "检验码错误，此时的数据是：<" + result + ">");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
        }
    }
}
