using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace YA.ServiceTemplate.Extensions
{
    public static class IpAddressExtensions
    {
        public static async Task<bool> CheckPingAsync(this IPAddress ip, int timeoutMs = 2000)
        {
            bool result = false;

            PingReply reply;

            using (Ping ping = new Ping())
            {
                reply = await ping.SendPingAsync(ip, timeoutMs).ConfigureAwait(false);
            }

            if (reply.Status == IPStatus.Success)
            {
                result = true;
            }

            return result;
        }
    }
}
