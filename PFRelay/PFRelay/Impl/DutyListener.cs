using System;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using PFRelay.Delivery;
using PFRelay.Util;

namespace PFRelay.Impl
{
    public class DutyListener
    {
        public static void On()
        {
            try
            {
                LoggerHelper.LogDebug("DutyListener On");
                Service.ClientState.CfPop += OnDutyPop;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error enabling DutyListener", ex);
            }
        }

        public static void Off()
        {
            try
            {
                LoggerHelper.LogDebug("DutyListener Off");
                Service.ClientState.CfPop -= OnDutyPop;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error disabling DutyListener", ex);
            }
        }

        private static void OnDutyPop(ContentFinderCondition e)
        {
            try
            {
                if (!Plugin.Configuration.EnableForDutyPops)
                {
                    LoggerHelper.LogDebug("Duty pop notification is disabled in configuration.");
                    return;
                }

                if (!CharacterUtil.IsClientAfk())
                {
                    LoggerHelper.LogDebug("Client is not AFK; no duty pop notification sent.");
                    return;
                }

                var dutyName = e.RowId == 0 ? "Duty Roulette" : e.Name.ToDalamudString().TextValue;
                LoggerHelper.LogDebug($"Duty pop detected: {dutyName}");

                try
                {
                    MasterDelivery.Deliver("Duty pop", $"Duty registered: '{dutyName}'.");
                    LoggerHelper.LogDebug("Duty pop notification sent successfully.");
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("Error delivering duty pop notification", ex);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error handling duty pop event", ex);
            }
        }
    }
}
