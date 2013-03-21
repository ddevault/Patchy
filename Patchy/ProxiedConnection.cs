using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MonoTorrent.Client.Connections;
using Starksoft.Net.Proxy;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows;

namespace Patchy
{
    public class ProxiedConnection : IConnection
    {
        private static string ProxyHostname { get; set; }
        private static ushort ProxyPort { get; set; }
        private static string Username { get; set; }
        private static string Password { get; set; }
        private static bool InformedUserOfFailure { get; set; }
        private static bool ConnectAnyway { get; set; }
        private static bool WaitingForUserInput { get; set; }

        public static void SetProxyDetails(string proxyHostname, ushort proxyPort)
        {
            ProxyHostname = proxyHostname;
            ProxyPort = proxyPort;
            Username = Password = null;
            WaitingForUserInput = ConnectAnyway = InformedUserOfFailure = false;
        }

        public static void SetProxyDetails(string proxyHostname, ushort proxyPort, string username, string password)
        {
            ProxyHostname = proxyHostname;
            ProxyPort = proxyPort;
            Username = username;
            Password = password;
            WaitingForUserInput = ConnectAnyway = InformedUserOfFailure = false;
        }

        private bool isIncoming;
        private IPEndPoint endPoint;
        private Socket socket;
        private Uri uri;
        private Socks5ProxyClient proxyClient;
        private ConcurrentQueue<ProxyConnectionResult> pendingOperations;

        public ProxiedConnection(Uri uri) : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), 
                   new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port), false)
        {
            this.uri = uri;
        }

        public ProxiedConnection(IPEndPoint endPoint) : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
            endPoint, false)
        {

        }

        public ProxiedConnection(Socket socket, bool isIncoming) : this(socket, (IPEndPoint)socket.RemoteEndPoint, isIncoming)
        {

        }


        private ProxiedConnection(Socket socket, IPEndPoint endPoint, bool isIncoming)
        {
            this.socket = socket;
            this.endPoint = endPoint;
            this.isIncoming = isIncoming;
            if (Username == null)
                this.proxyClient = new Socks5ProxyClient(ProxyHostname, ProxyPort);
            else
                this.proxyClient = new Socks5ProxyClient(ProxyHostname, ProxyPort, Username, Password);
            proxyClient.CreateConnectionAsyncCompleted += CreateConnectionAsyncCompleted;
            pendingOperations = new ConcurrentQueue<ProxyConnectionResult>();
        }

        public bool CanReconnect
        {
            get { return !isIncoming; }
        }

        public bool Connected
        {
            get { return socket.Connected; }
        }

        EndPoint IConnection.EndPoint
        {
            get { return endPoint; }
        }

        public IPEndPoint EndPoint
        {
            get { return endPoint; }
        }

        public bool IsIncoming
        {
            get { return isIncoming; }
        }

        public Uri Uri
        {
            get { return uri; }
        }

        public byte[] AddressBytes
        {
            get { return EndPoint.Address.GetAddressBytes(); }
        }

        public IAsyncResult BeginConnect(AsyncCallback callback, object state)
        {
            proxyClient.CreateConnectionAsync(EndPoint.Address.ToString(), EndPoint.Port);
            var result = new ProxyConnectionResult(state, callback);
            pendingOperations.Enqueue(result);
            return result;
        }

        public void EndConnect(IAsyncResult result)
        {
            // This space intentionally left blank
        }

        private void CreateConnectionAsyncCompleted(object sender, CreateConnectionAsyncCompletedEventArgs e)
        {
            ProxyConnectionResult result;
            while (!pendingOperations.TryDequeue(out result)) { }
            if (e.Error == null)
            {
                socket = e.ProxyConnection.Client;
                result.Callback(result);
            }
            else
            {
                // Inform user, ask if they want to forgo the proxy
                while (WaitingForUserInput) { }
                if (!InformedUserOfFailure)
                {
                    InformedUserOfFailure = true;
                    WaitingForUserInput = true;
                    ConnectAnyway = MessageBox.Show("Unable to connect to proxy. Connect without proxy?", "Error", MessageBoxButton.YesNo) ==
                                    MessageBoxResult.Yes;
                    WaitingForUserInput = false;
                }
                if (ConnectAnyway)
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        socket.Connect(EndPoint);
                        result.Callback(result);
                    }
                    catch (Exception ex)
                    {
                        result.Callback(result);
                    }
                }
                else
                    result.Callback(result);
            }
        }

        public IAsyncResult BeginReceive(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object state)
        {
            return socket.BeginReceive(buffer, offset, count, SocketFlags.None, asyncCallback, state);
        }

        public IAsyncResult BeginSend(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object state)
        {
            return socket.BeginSend(buffer, offset, count, SocketFlags.None, asyncCallback, state);
        }

        public void Dispose()
        {
            ((IDisposable)socket).Dispose();
        }

        public int EndSend(IAsyncResult result)
        {
            return socket.EndSend(result);
        }

        public int EndReceive(IAsyncResult result)
        {

            return socket.EndReceive(result);
        }
    }

    public class ProxyConnectionResult : IAsyncResult
    {
        internal ProxyConnectionResult(object asyncState, AsyncCallback callback)
        {
            AsyncState = asyncState;
            Callback = callback;
            CompletedSynchronously = IsCompleted = false;
        }

        public object AsyncState { get; private set; }

        public WaitHandle AsyncWaitHandle { get; private set; }

        public bool CompletedSynchronously { get; private set; }

        public bool IsCompleted { get; internal set; }

        internal AsyncCallback Callback { get; set; }
    }
}
