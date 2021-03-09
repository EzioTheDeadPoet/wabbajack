﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wabbajack.Test
{
    // MIT License - Copyright (c) 2016 Can Güney Aksakalli
    // https://aksakalli.github.io/2014/02/24/simple-http-server-with-csparp.html

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Net.Sockets;
    using System.Net;
    using System.IO;
    using System.Threading;
    using System.Diagnostics;


    public abstract class SimpleHTTPServer : IDisposable
    {
        private Thread _serverThread;
        private HttpListener _listener;
        private int _port;

        public int Port
        {
            get { return _port; }
        }
        /// <summary>
        /// Construct server with suitable port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        protected SimpleHTTPServer(string path)
        {
            //get an empty port
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            Initialize(path, port);
        }

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            _listener.Stop();
        }

        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:" + _port.ToString() + "/");
            _listener.Start();
            while (true)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        protected abstract void Process(HttpListenerContext context);

        private void Initialize(string path, int port)
        {
            _port = port;
            _serverThread = new Thread(this.Listen);
            _serverThread.Start();
            _serverThread.IsBackground = true;
        }
        
        public void Dispose()
        {
            //Stop();
        }
    }
}
