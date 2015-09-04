﻿using System;
using System.IO;
using System.Net;
using System.Text;
using Hassium.HassiumObjects;
using Hassium.HassiumObjects.Random;
using Hassium.HassiumObjects.IO;
using Hassium.HassiumObjects.List;
using Hassium.HassiumObjects.Networking;
using Hassium.HassiumObjects.Networking.HTTP;
using Hassium.HassiumObjects.Text;
using Hassium.HassiumObjects.Types;

namespace Hassium.Functions
{
    public class Constructors : ILibrary
    {
        [IntFunc("WebClient", true)]
        public static HassiumObject WebClient(HassiumObject[] args)
        {
            return new HassiumWebClient(new WebClient());
        }

        [IntFunc("Object", true)]
        public static HassiumObject Object(HassiumObject[] args)
        {
            return new HassiumObject();
        }

        [IntFunc("Date", true)]
        public static HassiumObject Date(HassiumObject[] args)
        {
            return new HassiumDate(DateTime.Now);
        }

        [IntFunc("TcpClient", true)]
        public static HassiumObject TcpClient(HassiumObject[] args)
        {
            return new HassiumTcpClient(new System.Net.Sockets.TcpClient());
        }

        [IntFunc("Array", true)]
        public static HassiumObject Array(HassiumObject[] args)
        {
            return new HassiumArray(new HassiumObject[Convert.ToInt32(args[0].ToString())]);
        }

        [IntFunc("StreamWriter", true)]
        public static HassiumObject StreamWriter(HassiumObject[] args)
        {
            return new HassiumStreamWriter(new System.IO.StreamWriter(((HassiumStream)args[0]).Value));
        }

        [IntFunc("StreamReader", true)]
        public static HassiumObject StreamReader(HassiumObject[] args)
        {
            return new HassiumStreamReader(new System.IO.StreamReader(((HassiumStream)args[0]).Value));
        }

        [IntFunc("NetworkStream", true)]
        public static HassiumObject NetworkStream(HassiumObject[] args)
        {
            return new HassiumNetworkStream(new System.Net.Sockets.NetworkStream(((HassiumSocket)args[0]).Value));
        }

        [IntFunc("FileStream", true)]
        public static HassiumObject FileStream(HassiumObject[] args)
        {
            return new HassiumFileStream(new FileStream(args[0].HString().Value, FileMode.OpenOrCreate));
        }

        [IntFunc("StringBuilder", true)]
        public static HassiumObject StringBuilder(HassiumObject[] args)
        {
            return new HassiumStringBuilder(new StringBuilder());
        }

        [IntFunc("HttpListener", true)]
        public static HassiumObject HttpListener(HassiumObject[] args)
        {
            return new HassiumHttpListener(new System.Net.HttpListener());
        }

        [IntFunc("BinaryWriter", true)]
        public static HassiumObject BinaryWriter(HassiumObject[] args)
        {
            return new HassiumBinaryWriter(new BinaryWriter(((HassiumStream)args[0]).Value));
        }

        [IntFunc("BinaryReader", true)]
        public static HassiumObject BinaryReader(HassiumObject[] args)
        {
            return new HassiumBinaryReader(new System.IO.BinaryReader(((HassiumStream)args[0]).Value));
        }

        [IntFunc("Random", true)]
        public static HassiumObject Random(HassiumObject[] args)
        {
            if (args.Length > 0)
                return new HassiumRandom(new System.Random(((HassiumNumber)args[0]).ValueInt));
            else
                return new HassiumRandom(new System.Random());
        }

        [IntFunc("List", true)]
        public static HassiumObject List(HassiumObject[] args)
        {
            return new HassiumList(new System.Collections.Generic.List<HassiumObject>());
        }

        [IntFunc("Stack", true)]
        public static HassiumObject Stack(HassiumObject[] args)
        {
            return new HassiumStack(new System.Collections.Stack(((HassiumNumber)args[0]).ValueInt));
        }
    }
}

