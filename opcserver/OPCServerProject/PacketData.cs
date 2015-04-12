﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OPCServerProject
{
    public class PacketData
    {
        public Dictionary<string, object> packetDataMap = new Dictionary<string, object>();
        public string moduleID;
        public string sendTime;
        public bool isAlert;
        public int alertPos;
        public int alertValue;

        public string resolvePos(int pos)
        {
            if (pos == 1)
                return "AI1/AC1";
            else if (pos == 2)
                return "AI2/AC2";
            else if (pos == 3)
                return "AI3/AC3";
            else if (pos == 4)
                return "AI4/AC4";
            else if (pos == 5)
                return "AI5/AC5";
            else if (pos == 6)
                return "AI6/AC6";
            else if (pos == 7)
                return "DI1";
            else if (pos == 8)
                return "DI2";
            else if (pos == 9)
                return "DI3";
            else if (pos == 10)
                return "DI4";
            else if (pos == 11)
                return "DI5";
            else if (pos == 12)
                return "DI6";
            else if (pos == 13)
                return "DO1";
            else if (pos == 14)
                return "DO2";
            else if (pos == 15)
                return "DO3";
            else if (pos == 16)
                return "DO4";
            else if (pos == 17)
                return "DO5";
            else 
                return "DO6";
        }

        public PacketData()
        {
            packetDataMap.Add("GPRS-Level", 0);
            packetDataMap.Add("AI1/AC1", 0);
            packetDataMap.Add("AI2/AC2", 0);
            packetDataMap.Add("AI3/AC3", 0);
            packetDataMap.Add("AI4/AC4", 0);
            packetDataMap.Add("AI5/AC5", 0);
            packetDataMap.Add("AI6/AC6", 0);
            packetDataMap.Add("DI1", false);
            packetDataMap.Add("DI2", false);
            packetDataMap.Add("DI3", false);
            packetDataMap.Add("DI4", false);
            packetDataMap.Add("DI5", false);
            packetDataMap.Add("DI6", false);
            packetDataMap.Add("DO1", false);
            packetDataMap.Add("DO2", false);
            packetDataMap.Add("DO3", false);
            packetDataMap.Add("DO4", false);
            packetDataMap.Add("DO5", false);
            packetDataMap.Add("DO6", false);
        }

        public static PacketData resolveData1(byte[] data1)
        {
            PacketData packet = new PacketData();
            packet.isAlert = false;
            byte[] moduleID = new byte[12];
            for (int i = 0; i < 12; i++)
            {
                moduleID[i] = data1[i];
            }
            packet.moduleID = Encoding.ASCII.GetString(moduleID);
            int year = Convert.ToInt16(data1[12]);
            int month = Convert.ToInt16(data1[13]);
            int day = Convert.ToInt16(data1[14]);
            int hour = Convert.ToInt16(data1[15]);
            int minute = Convert.ToInt16(data1[16]);
            int second = Convert.ToInt16(data1[17]);

            packet.packetDataMap["GPRS-Level"] = Convert.ToInt16(data1[18]);
            packet.packetDataMap["AI1/AC1"] = BitConverter.ToInt16(data1, 19);
            packet.packetDataMap["AI2/AC2"] = BitConverter.ToInt16(data1, 21);
            packet.packetDataMap["AI3/AC3"] = BitConverter.ToInt16(data1, 23);
            packet.packetDataMap["AI4/AC4"] = BitConverter.ToInt16(data1, 25);
            packet.packetDataMap["AI5/AC5"] = BitConverter.ToInt16(data1, 27);
            packet.packetDataMap["AI6/AC6"] = BitConverter.ToInt16(data1, 29);

            string value = Convert.ToString(data1[31], 2);
            packet.packetDataMap["DI1"] = Convert.ToBoolean(value[0]);
            packet.packetDataMap["DI2"] = Convert.ToBoolean(value[1]);
            packet.packetDataMap["DI3"] = Convert.ToBoolean(value[2]);
            packet.packetDataMap["DI4"] = Convert.ToBoolean(value[3]);
            packet.packetDataMap["DI5"] = Convert.ToBoolean(value[4]);
            packet.packetDataMap["DI6"] = Convert.ToBoolean(value[5]);

            value = Convert.ToString(data1[32], 2);
            packet.packetDataMap["DO1"] = Convert.ToBoolean(value[0]);
            packet.packetDataMap["DO2"] = Convert.ToBoolean(value[1]);
            packet.packetDataMap["DO3"] = Convert.ToBoolean(value[2]);
            packet.packetDataMap["DO4"] = Convert.ToBoolean(value[3]);
            packet.packetDataMap["DO5"] = Convert.ToBoolean(value[4]);
            packet.packetDataMap["DO6"] = Convert.ToBoolean(value[5]);
            return packet;
        }

        public static PacketData resolveData4(byte[] data4)
        {
            PacketData packet = new PacketData();
            packet.isAlert = true;

            byte[] moduleID = new byte[12];
            for (int i = 0; i < 12; i++)
            {
                moduleID[i] = data4[i];
            }
            packet.moduleID = Encoding.ASCII.GetString(moduleID);

            packet.alertPos = Convert.ToInt32(data4[12]);
            packet.alertValue = BitConverter.ToInt16(data4,13);

            return packet;
        }
    }
}
