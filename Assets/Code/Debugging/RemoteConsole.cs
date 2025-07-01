#if REMOTE_CONSOLE
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Threads;
using QFSW.QC;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Awaken.TG.Assets.Code.Debugging {
    internal class RemoteConsole {
        const int Port = 5000;
        UdpClient udpServer;
        Thread receiveThread;
        bool listening;

        public RemoteConsole() {
            Application.quitting += OnApplicationQuit;
            udpServer = new UdpClient(Port);
            receiveThread = new Thread(new ThreadStart(ReceiveData)) {
                Name = "ConsoleServiceThread",
                IsBackground = true
            };
            receiveThread.Start();
            Debug.Log($"Server started, listening on port {Port}...");
        }

        void ReceiveData() {
            listening = true;
            var remoteEndPoint = new IPEndPoint(IPAddress.Any, Port);
            while (listening) {
                try {
                    byte[] data = udpServer.Receive(ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(data);
                    Debug.Log($"ConsoleService: Message received: {message}");
                    switch (message) {
                        case "cheatme":
                            World.Any<CheatController>().TryPassword("cheatme");
                            break;
                        default:
                            MainThreadDispatcher.InvokeAsync(() => QuantumConsoleProcessor.InvokeCommand(message));
                            break;
                    }
                } catch (System.Exception e) {
                    Debug.LogError($"ConsoleService: Exception: {e}");
                }
            }
        }

        void OnApplicationQuit() {
            listening = false;
            udpServer.Close();
            if (receiveThread != null) {
                receiveThread.Abort();
            }
        }
    }
}
#endif