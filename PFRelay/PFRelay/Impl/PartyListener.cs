using System;
using PFRelay.Delivery;
using PFRelay.Util;

namespace PFRelay.Impl
{
    public static class PartyListener
    {
        public static void On()
        {
            try
            {
                LoggerHelper.LogDebug("PartyListener On");
                CrossWorldPartyListSystem.OnJoin += OnJoin;
                CrossWorldPartyListSystem.OnLeave += OnLeave;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error enabling PartyListener", ex);
            }
        }

        public static void Off()
        {
            try
            {
                LoggerHelper.LogDebug("PartyListener Off");
                CrossWorldPartyListSystem.OnJoin -= OnJoin;
                CrossWorldPartyListSystem.OnLeave -= OnLeave;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error disabling PartyListener", ex);
            }
        }

        private static void OnJoin(CrossWorldPartyListSystem.CrossWorldMember m)
        {
            try
            {
                if (!CharacterUtil.IsClientAfk())
                {
                    LoggerHelper.LogDebug("Client is not AFK; no party join notification sent.");
                    return;
                }

                var jobAbbr = LuminaDataUtil.GetJobAbbreviation(m.JobId);

                if (m.PartyCount == 8)
                {
                    try
                    {
                        MasterDelivery.Deliver("Party full",
                                               $"{m.Name} (Lv{m.Level} {jobAbbr}) joins the party.\nParty recruitment ended. All spots have been filled.");
                        LoggerHelper.LogDebug("Party full notification sent successfully.");
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError("Error delivering party full notification", ex);
                    }
                }
                else
                {
                    try
                    {
                        MasterDelivery.Deliver($"{m.PartyCount}/8: Party join",
                                               $"{m.Name} (Lv{m.Level} {jobAbbr}) joins the party.");
                        LoggerHelper.LogDebug("Party join notification sent successfully.");
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError("Error delivering party join notification", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error handling OnJoin event", ex);
            }
        }

        private static void OnLeave(CrossWorldPartyListSystem.CrossWorldMember m)
        {
            try
            {
                if (!CharacterUtil.IsClientAfk())
                {
                    LoggerHelper.LogDebug("Client is not AFK; no party leave notification sent.");
                    return;
                }

                var jobAbbr = LuminaDataUtil.GetJobAbbreviation(m.JobId);

                try
                {
                    MasterDelivery.Deliver($"{m.PartyCount - 1}/8: Party leave",
                                           $"{m.Name} (Lv{m.Level} {jobAbbr}) has left the party.");
                    LoggerHelper.LogDebug("Party leave notification sent successfully.");
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("Error delivering party leave notification", ex);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error handling OnLeave event", ex);
            }
        }
    }
}
