using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PFRelay.Delivery;
using PFRelay.Util;

namespace PFRelay.Impl
{
    public static class PartyListener
    {
        private static List<CrossWorldPartyListSystem.CrossWorldMember> recentJoins = new();
        private static DateTime lastBatchTime = DateTime.Now;
        private static readonly TimeSpan batchInterval = TimeSpan.FromSeconds(5);
        private static bool joinBatchScheduled = false;
        private static bool isDeliveringBatch = false;

        public static void On()
        {
            try
            {
                LoggerHelper.LogDebug("PartyListener On");
                CrossWorldPartyListSystem.OnJoin += HandleOnJoin;
                CrossWorldPartyListSystem.OnLeave += HandleOnLeave;
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
                CrossWorldPartyListSystem.OnJoin -= HandleOnJoin;
                CrossWorldPartyListSystem.OnLeave -= HandleOnLeave;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error disabling PartyListener", ex);
            }
        }

        private static void HandleOnJoin(CrossWorldPartyListSystem.CrossWorldMember member)
        {
            if (isDeliveringBatch)
            {
                recentJoins.Add(member);
                RescheduleBatchDelivery();
                return;
            }

            recentJoins.Add(member);

            if (!joinBatchScheduled)
            {
                joinBatchScheduled = true;
                Task.Delay(batchInterval).ContinueWith(_ => DeliverJoinBatch());
            }
        }

        private static void HandleOnLeave(CrossWorldPartyListSystem.CrossWorldMember member)
        {
            DeliverSingleEvent($"{member.PartyCount - 1}/8: Party leave",
                               $"{member.Name} (Lv{member.Level} {LuminaDataUtil.GetJobAbbreviation(member.JobId)}) has left the party.");
        }

        private static void DeliverJoinBatch()
        {
            if (isDeliveringBatch || recentJoins.Count == 0) return;

            isDeliveringBatch = true; 

            if (!CharacterUtil.IsClientAfk())
            {
                LoggerHelper.LogDebug("Client is not AFK; no join notification sent.");
                recentJoins.Clear();
                isDeliveringBatch = false;
                joinBatchScheduled = false; 
                return;
            }

            var message = FormatJoinMessage(recentJoins);

            MasterDelivery.Deliver("Party Join Update", message);
            LoggerHelper.LogDebug("Party join batch notification sent.");

            recentJoins.Clear();
            lastBatchTime = DateTime.Now;

            isDeliveringBatch = false; 
            joinBatchScheduled = false; 

            if (recentJoins.Count > 0)
            {
                RescheduleBatchDelivery();
            }
        }

        private static void RescheduleBatchDelivery()
        {
            if (!joinBatchScheduled)
            {
                joinBatchScheduled = true;
                Task.Delay(batchInterval).ContinueWith(_ => DeliverJoinBatch());
            }
        }

        private static string FormatJoinMessage(List<CrossWorldPartyListSystem.CrossWorldMember> members)
        {
            var formattedMembers = members
                .Select(m => $"• **{m.Name}** (Lv{m.Level} **{LuminaDataUtil.GetJobAbbreviation(m.JobId)}**)")
                .ToList();

            return "The following members have joined the party:\n" + string.Join("\n", formattedMembers);
        }

        private static void DeliverSingleEvent(string title, string message)
        {
            if (CharacterUtil.IsClientAfk())
            {
                MasterDelivery.Deliver(title, message);
                LoggerHelper.LogDebug($"{title} notification sent successfully.");
            }
            else
            {
                LoggerHelper.LogDebug("Client is not AFK; no leave or duty pop notification sent.");
            }
        }
    }
}
