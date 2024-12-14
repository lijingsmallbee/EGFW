// Add by Jiacan Li
// Kings Group

using System;
using System.Collections.Generic;
using System.Net;

public static class DnsCache
{
    private static readonly Dictionary<string, IPAddress[]> AddressCache = new Dictionary<string, IPAddress[]>();

    public static IPAddress[] GetHostAddresses(string hostname, TimeSpan connectTimeout)
    {
        IPAddress[] addresses;
        if (!AddressCache.TryGetValue(hostname, out addresses))
        {
            var mre = new System.Threading.ManualResetEvent(false);
            var result = Dns.BeginGetHostAddresses(hostname, res => mre.Set(), null);
            var success = mre.WaitOne(connectTimeout);
            if (success)
            {
                addresses = Dns.EndGetHostAddresses(result);
                AddressCache[hostname] = addresses;
            }
            else
            {
                throw new TimeoutException("DNS resolve timed out!");
            }
        }

        return addresses;
    }

    public static IPAddress[] GetHostAddresses(string hostname)
    {
        IPAddress[] addresses;
        if (!AddressCache.TryGetValue(hostname, out addresses))
        {
            addresses = Dns.GetHostAddresses(hostname);
            AddressCache[hostname] = addresses;
        }

        return addresses;
    }

    public static void Clear()
    {
        AddressCache.Clear();
    }
}