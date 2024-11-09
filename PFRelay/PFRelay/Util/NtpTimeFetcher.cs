using System;
using System.Net;
using System.Net.Sockets;

namespace PFRelay.Util
{
    public static class NtpTimeFetcher
    {
        public static string GetNtpTime()
        {
            const string ntpServer = "0.pool.ntp.org";
            const int ntpPort = 123;
            const int ntpPacketSize = 48;
            byte[] ntpData = new byte[ntpPacketSize];

            // Set up NTP request packet
            ntpData[0] = 0x1B; // LI, Version, Mode settings for NTP request

            try
            {
                // Create a UDP client to communicate with the NTP server
                using (var udpClient = new UdpClient())
                {
                    udpClient.Connect(ntpServer, ntpPort);
                    udpClient.Send(ntpData, ntpData.Length);

                    // Wait for response
                    var ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    var response = udpClient.Receive(ref ipEndPoint);

                    if (response.Length < ntpPacketSize)
                    {
                        throw new Exception("Invalid response from NTP server.");
                    }

                    // Extract timestamp from response (NTP timestamp starts at byte 40 in response)
                    ulong intPart = BitConverter.ToUInt32(response, 40);
                    ulong fractPart = BitConverter.ToUInt32(response, 44);

                    // Convert to big-endian if system is little-endian
                    intPart = SwapEndianness(intPart);
                    fractPart = SwapEndianness(fractPart);

                    // Calculate time in seconds since Jan 1, 1900 (NTP epoch)
                    ulong millisecondsSince1900 = (intPart * 1000) + ((fractPart * 1000) / 0x100000000UL);

                    // NTP epoch starts in 1900, Unix epoch starts in 1970
                    const ulong unixEpochOffset = 2208988800000UL;
                    ulong millisecondsSince1970 = millisecondsSince1900 - unixEpochOffset;

                    // Return as Unix timestamp (in UTC)
                    var unixTime = millisecondsSince1970 / 1000;
                    return unixTime.ToString();
                }
            }
            catch (SocketException ex)
            {
                LoggerHelper.LogError("Socket error occurred while querying NTP server", ex);
                throw;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error fetching time from NTP server", ex);
                throw;
            }
        }

        // Utility to convert to big-endian if necessary
        private static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000FF) << 24) + ((x & 0x0000FF00) << 8) +
                          ((x & 0x00FF0000) >> 8) + ((x & 0xFF000000) >> 24));
        }
    }
}
