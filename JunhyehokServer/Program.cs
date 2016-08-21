﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Junhaehok;
using static Junhaehok.HhhHelper;
using System.Web;
using System.Net.WebSockets;

namespace JunhyehokServer
{
    class Program
    {
        static void Main(string[] args)
        {
            string host = null;     //Default
            string clientPort = "30000";  //Default
            string mmfName = "JunhyehokMmf"; //Default
            TcpServer echoc;

            //=========================GET ARGS=================================
            if (args.Length == 0)
            {
                Console.WriteLine("Format: JunhyehokServer -cp [client port] -mmf [MMF name]");
                Environment.Exit(0);
            }

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--help":
                        Console.WriteLine("Format: JunhyehokServer -cp [client port] -mmf [MMF name]");
                        Environment.Exit(0);
                        break;
                    case "-mmf":
                        mmfName = args[++i];
                        break;
                    case "-cp":
                        clientPort = args[++i];
                        break;
                    default:
                        Console.Error.WriteLine("ERROR: incorrect inputs \nFormat: JunhyehokServer -cp [client port] -mmf [MMF name]");
                        Environment.Exit(0);
                        break;
                }
            }

            //======================SOCKET BIND/LISTEN==========================
            /* if only given port, host is ANY */
            echoc = new TcpServer(host, clientPort);

            //======================BACKEND CONNECT===============================
            Console.WriteLine("Connecting to Backend...");
            string backendInfo = "";
            try { backendInfo = System.IO.File.ReadAllText("backend.conf"); }
            catch (Exception e) { Console.WriteLine("\n" + e.Message); Environment.Exit(0); }
            Socket backendSocket = Connect(backendInfo);
            //Socket backendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            ClientHandle backend = new ClientHandle(backendSocket);

            //=================ADVERTISE IP:PORT TO BACKEND======================
            FBAdvertiseRequest fbAdvertiseRequest;
            char[] ip = ((IPEndPoint)backend.So.LocalEndPoint).Address.ToString().ToCharArray();
            char[] ipBuffer = new char[15];
            Array.Copy(ip, ipBuffer, ip.Length);
            fbAdvertiseRequest.ip = ipBuffer;
            fbAdvertiseRequest.port = int.Parse(clientPort);
            byte[] advertiseBytes = Serializer.StructureToByte(fbAdvertiseRequest);
            backend.So.SendBytes(new Packet(new Header(Code.ADVERTISE, (ushort)advertiseBytes.Length), advertiseBytes));
            //FIRE OFF TASK
            backend.StartSequence();

            //======================INITIALIZE==================================
            Console.WriteLine("Initializing lobby and rooms...");
            ReceiveHandle recvHandle = new ReceiveHandle(backendSocket, mmfName);

            //===================CLIENT SOCKET ACCEPT===========================
            Console.WriteLine("Accepting clients...");
            while (true)
            {
                /*
                if (clientPort == "80")
                {
                    var wstesthost = "ws://example.microsoft.com";
                    var ws = new WebSocket(wstesthost);
                }
                */
                Socket s = echoc.so.Accept();
                ClientHandle client = new ClientHandle(s);
                client.StartSequence();
            }
        }
        public static Socket Connect(string info)
        {
            string host;
            int port;
            string[] hostport = info.Split(':');
            host = hostport[0];
            if (!int.TryParse(hostport[1], out port))
            {
                Console.Error.WriteLine("port must be int. given: {0}", hostport[1]);
                Environment.Exit(0);
            }

            Socket so = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAddress = IPAddress.Parse(host);

            Console.WriteLine("Establishing connection to {0}:{1} ...", host, port);

            try
            {
                so.Connect(ipAddress, port);
                Console.WriteLine("Connection established.\n");
            }
            catch (Exception)
            {
                Console.WriteLine("Peer is not alive.");
            }

            return so;
        }
    }
}
