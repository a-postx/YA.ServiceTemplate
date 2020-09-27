using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace YA.ServiceTemplate.Utils
{
    public static class TcpConnection
    {
        public static async Task<bool> CheckAsync(string host, int port, int sendTimeout = 0, int receiveTimeout = 0)
        {
            bool result = false;

            using (TcpClient tcpClient = new TcpClient { ReceiveTimeout = receiveTimeout, SendTimeout = sendTimeout })
            {
                try
                {
                    await tcpClient.ConnectAsync(host, port);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error checking TCP connection to " + host + ":" + port + ".", ex);
                }
                finally
                {
                    if (tcpClient.Connected)
                    {
                        tcpClient.Close();
                        result = true;
                    }
                }
            }

            return result;
        }
    }
}
