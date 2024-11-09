using System;
using PFRelay.Util; // Import LoggerHelper

namespace PFRelay.Util
{
    public static class CharacterUtil
    {
        public static bool IsClientAfk()
        {
            try
            {
                if (Plugin.Configuration.IgnoreAfkStatus)
                {
                    LoggerHelper.LogDebug("AFK status ignored due to configuration setting.");
                    return true;
                }

                if (!Service.ClientState.IsLoggedIn || Service.ClientState.LocalPlayer == null)
                {
                    LoggerHelper.LogDebug("Client is not logged in or local player is null. Returning not AFK.");
                    return false;
                }

                // 17 = AFK, 18 = Camera Mode (should catch idle camera. also has the effect of catching gpose!)
                bool isAfk = Service.ClientState.LocalPlayer.OnlineStatus.Id is 17 or 18;
                LoggerHelper.LogDebug($"AFK check result: {isAfk} (Status ID: {Service.ClientState.LocalPlayer.OnlineStatus.Id})");
                return isAfk;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error checking AFK status", ex);
                return false; // Safe default in case of an error
            }
        }
    }
}
