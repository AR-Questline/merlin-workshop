using System;
using System.Net.Sockets;
using System.Text;

namespace Awaken.TG.Editor.Utility.Audio {
    public class TelnetConnection {
        public readonly bool connected;
        readonly TcpClient _tcpSocket;
        
        public TelnetConnection(string hostname, int port) {
            try {
                _tcpSocket = new TcpClient(hostname, port);
            } catch (SocketException) {
                connected = false;
                return;
            } catch (ArgumentOutOfRangeException) {
                connected = false;
                return;
            }

            connected = true;
        }

        public void WriteLine(string cmd) {
            cmd += "\n";
            if (!_tcpSocket.Connected) {
                return;
            }
            byte[] buf = Encoding.ASCII.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
            _tcpSocket.GetStream().Write(buf, 0, buf.Length);
        }
    }
}