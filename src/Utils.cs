using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace YA.ServiceTemplate
{
    public static class Utils
    {
        public static IEnumerable<List<T>> SplitList<T>(this List<T> list, int batchSize)
        {
            if (batchSize > 0)
            {
                for (int i = 0; i < list.Count; i += batchSize)
                {
                    yield return list.GetRange(i, Math.Min(batchSize, list.Count - i));
                }
            }
        }

        public static DateTime UnixTimeStampToDateTime(this double unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static double ToUnixTimestamp(this DateTime dateTime)
        {
            return (TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                    new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public static string ListToString(this IList list)
        {
            StringBuilder result = new StringBuilder(string.Empty);

            if (list.Count > 0)
            {
                result.Append(list[0]);
                CultureInfo ci = new CultureInfo("en-US", true);
                for (int i = 1; i < list.Count; i++)
                    result.AppendFormat(ci, ",{0}", list[i]);
            }
            return result.ToString();
        }

        public static async Task<bool> CheckPingAsync(this IPAddress ip, int timeoutMs = 2000)
        {
            bool result = false;

            try
            {
                PingReply reply;

                using (Ping ping = new Ping())
                {
                    reply = await ping.SendPingAsync(ip, timeoutMs).ConfigureAwait(false);
                }

                if (reply.Status == IPStatus.Success)
                {
                    result = true;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error checking ping to " + ip + ".", e);
            }

            return result;
        }

        public static async Task<bool> CheckTcpConnectionAsync(string host, int port, int sendTimeout= 0, int receiveTimeout = 0)
        {
            bool result = false;

            using (TcpClient tcpClient = new TcpClient { ReceiveTimeout = receiveTimeout, SendTimeout = sendTimeout })
            {
                try
                {
                    await tcpClient.ConnectAsync(host, port).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    throw new Exception("Error checking TCP connection to " + host + ":" + port + ".", e);
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

        public static HttpClient GetHttpClient(string userAgent = "YA/1.0", int requestTimeout = 60)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            client.Timeout = TimeSpan.FromSeconds(requestTimeout);

            return client;
        }

        public static async Task<T> ReadAsJsonAsync<T>(this HttpContent content)
        {
            string json = await content.ReadAsStringAsync();
            T value = JsonConvert.DeserializeObject<T>(json);
            return value;
        }

        public static IDisposable BeginScopeWith(this ILogger logger, params (string key, object value)[] keys)
        {
            return logger.BeginScope(keys.ToDictionary(x => x.key, x => x.value));
        }

        public static IEnumerable<T> If<T>(this IEnumerable<T> enumerable, bool condition, Func<IEnumerable<T>, IEnumerable<T>> action)
        {
            if(enumerable is null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (condition)
            {
                return action(enumerable);
            }

            return enumerable;
        }

        public static async Task<IEnumerable<T>> IfAsync<T>(this IEnumerable<T> enumerable, bool condition, Func<IEnumerable<T>, Task<IEnumerable<T>>> action)
        {
            if(enumerable is null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (condition)
            {
                return await action(enumerable);
            }

            return enumerable;
        }

        public static IQueryable<T> If<T>(this IQueryable<T> enumerable, bool condition, Func<IQueryable<T>, IQueryable<T>> action)
        {
            if(enumerable is null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (condition)
            {
                return action(enumerable);
            }

            return enumerable;
        }

        public static async Task<IQueryable<T>> IfAsync<T>(this IQueryable<T> enumerable, bool condition, Func<IQueryable<T>, Task<IQueryable<T>>> action)
        {
            if(enumerable is null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (condition)
            {
                return await action(enumerable);
            }

            return enumerable;
        }
    }
}
