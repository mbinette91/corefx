// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Threading;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class AcceptAsync
    {
        private readonly ITestOutputHelper _log;

        public AcceptAsync(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
        }

        public void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            _log.WriteLine("OnAcceptCompleted event handler");
            EventWaitHandle handle = (EventWaitHandle)args.UserToken;
            handle.Set();
        }
        public void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            _log.WriteLine("OnConnectCompleted event handler");
            EventWaitHandle handle = (EventWaitHandle)args.UserToken;
            handle.Set();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [Trait("IPv4", "true")]
        public void AcceptAsync_IpV4_Success()
        {
            Assert.True(Capability.IPv4Support());

            AutoResetEvent completed = new AutoResetEvent(false);
            AutoResetEvent completedClient = new AutoResetEvent(false);

            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = sock.BindToAnonymousPort(IPAddress.Loopback);
                sock.Listen(1);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnAcceptCompleted;
                args.UserToken = completed;

                Assert.True(sock.AcceptAsync(args));
                _log.WriteLine("IPv4 Server: Waiting for clients.");

                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    SocketAsyncEventArgs argsClient = new SocketAsyncEventArgs();
                    argsClient.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                    argsClient.Completed += OnConnectCompleted;
                    argsClient.UserToken = completedClient;
                    client.ConnectAsync(argsClient);

                    _log.WriteLine("IPv4 Client: Connecting.");
                    Assert.True(completed.WaitOne(5000), "IPv4: Timed out while waiting for connection");

                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);
                    Assert.NotNull(args.AcceptSocket);
                    Assert.True(args.AcceptSocket.Connected, "IPv4 Accept Socket was not connected");
                    Assert.NotNull(args.AcceptSocket.RemoteEndPoint);
                    Assert.Equal(client.LocalEndPoint, args.AcceptSocket.RemoteEndPoint);
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [Trait("IPv6", "true")]
        public void AcceptAsync_IPv6_Success()
        {
            Assert.True(Capability.IPv6Support());

            AutoResetEvent completed = new AutoResetEvent(false);
            AutoResetEvent completedClient = new AutoResetEvent(false);

            using (Socket sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = sock.BindToAnonymousPort(IPAddress.IPv6Loopback);
                sock.Listen(1);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnAcceptCompleted;
                args.UserToken = completed;

                Assert.True(sock.AcceptAsync(args));
                _log.WriteLine("IPv6 Server: Waiting for clients.");

                using (Socket client = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    SocketAsyncEventArgs argsClient = new SocketAsyncEventArgs();
                    argsClient.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, port);
                    argsClient.Completed += OnConnectCompleted;
                    argsClient.UserToken = completedClient;
                    client.ConnectAsync(argsClient);

                    _log.WriteLine("IPv6 Client: Connecting.");
                    Assert.True(completed.WaitOne(5000), "IPv6: Timed out while waiting for connection");

                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);
                    Assert.NotNull(args.AcceptSocket);
                    Assert.True(args.AcceptSocket.Connected, "IPv6 Accept Socket was not connected");
                    Assert.NotNull(args.AcceptSocket.RemoteEndPoint);
                    Assert.Equal(client.LocalEndPoint, args.AcceptSocket.RemoteEndPoint);
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void AcceptAsync_WithReceiveBuffer_Success()
        {
            Assert.True(Capability.IPv4Support());

            AutoResetEvent accepted = new AutoResetEvent(false);

            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = server.BindToAnonymousPort(IPAddress.Loopback);
                server.Listen(1);

                const int acceptBufferOverheadSize = 288; // see https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.acceptasync(v=vs.110).aspx
                const int acceptBufferDataSize = 256;
                const int acceptBufferSize = acceptBufferOverheadSize + acceptBufferDataSize;

                byte[] sendBuffer = new byte[acceptBufferDataSize];
                new Random().NextBytes(sendBuffer);

                SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += OnAcceptCompleted;
                acceptArgs.UserToken = accepted;
                acceptArgs.SetBuffer(new byte[acceptBufferSize], 0, acceptBufferSize);

                Assert.True(server.AcceptAsync(acceptArgs));
                _log.WriteLine("IPv4 Server: Waiting for clients.");

                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    client.Connect(IPAddress.Loopback, port);
                    client.Send(sendBuffer);
                    client.Shutdown(SocketShutdown.Both);
                }

                Assert.True(
                    accepted.WaitOne(TestSettings.PassingTestTimeout), "Test completed in alotted time");

                Assert.Equal(
                    SocketError.Success, acceptArgs.SocketError);

                Assert.Equal(
                    acceptBufferDataSize, acceptArgs.BytesTransferred);

                Assert.Equal(
                    new ArraySegment<byte>(sendBuffer), 
                    new ArraySegment<byte>(acceptArgs.Buffer, 0, acceptArgs.BytesTransferred));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void AcceptAsync_WithReceiveBuffer_Failure()
        {
            //
            // Unix platforms don't yet support receiving data with AcceptAsync.
            //

            Assert.True(Capability.IPv4Support());

            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = server.BindToAnonymousPort(IPAddress.Loopback);
                server.Listen(1);

                SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += OnAcceptCompleted;
                acceptArgs.UserToken = new ManualResetEvent(false);

                byte[] buffer = new byte[1024];
                acceptArgs.SetBuffer(buffer, 0, buffer.Length);

                Assert.Throws<PlatformNotSupportedException>(() => server.AcceptAsync(acceptArgs));
            }
        }

        #region GC Finalizer test
        // This test assumes sequential execution of tests and that it is going to be executed after other tests
        // that used Sockets. 
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void TestFinalizers()
        {
            // Making several passes through the FReachable list.
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        #endregion 
    }
}
