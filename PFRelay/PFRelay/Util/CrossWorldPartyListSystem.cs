using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using PFRelay.Util; 
namespace PFRelay.Util
{
    public static class CrossWorldPartyListSystem
    {
        public delegate void CrossWorldJoinDelegate(CrossWorldMember m);
        public delegate void CrossWorldLeaveDelegate(CrossWorldMember m);

        private static readonly List<CrossWorldMember> members = new();
        private static List<CrossWorldMember> oldMembers = new();

        public static event CrossWorldJoinDelegate? OnJoin;
        public static event CrossWorldLeaveDelegate? OnLeave;

        private static DateTime nextStatusLogTime = DateTime.MinValue; 
        private const int StatusLogCooldownSeconds = 30;

        public static void Start()
        {
            try
            {
                LoggerHelper.LogDebug("Starting CrossWorldPartyListSystem...");
                Service.Framework.Update += Update;
                LoggerHelper.LogDebug("CrossWorldPartyListSystem started successfully.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error starting CrossWorldPartyListSystem", ex);
            }
        }

        public static void Stop()
        {
            try
            {
                LoggerHelper.LogDebug("Stopping CrossWorldPartyListSystem...");
                Service.Framework.Update -= Update;
                LoggerHelper.LogDebug("CrossWorldPartyListSystem stopped successfully.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error stopping CrossWorldPartyListSystem", ex);
            }
        }

        private static bool ListContainsMember(List<CrossWorldMember> list, CrossWorldMember member)
            => list.Any(a => a.Name == member.Name);

        private static unsafe void Update(IFramework framework)
        {
            try
            {
                if (!Service.ClientState.IsLoggedIn)
                {
                    LoggerHelper.LogDebug("Client is not logged in; skipping CrossWorldPartyListSystem update.");
                    return;
                }

                if (!InfoProxyCrossRealm.IsCrossRealmParty())
                {
                    // Log "not in cross-realm party" message only if the cooldown period has passed
                    if (DateTime.UtcNow >= nextStatusLogTime)
                    {
                        LoggerHelper.LogDebug("Not in a cross-realm party; skipping update.");
                        nextStatusLogTime = DateTime.UtcNow.AddSeconds(StatusLogCooldownSeconds);
                    }
                    return;
                }

                members.Clear();
                var partyCount = InfoProxyCrossRealm.GetPartyMemberCount();

                for (var i = 0u; i < partyCount; i++)
                {
                    var addr = InfoProxyCrossRealm.GetGroupMember(i);
                    var name = addr->NameString;
                    var member = new CrossWorldMember
                    {
                        Name = name,
                        PartyCount = (int)partyCount,
                        Level = addr->Level,
                        JobId = addr->ClassJobId
                    };
                    members.Add(member);
                }

                if (members.Count != oldMembers.Count)
                {
                    try
                    {
                        foreach (var newMember in members)
                        {
                            if (!ListContainsMember(oldMembers, newMember))
                            {
                                OnJoin?.Invoke(newMember);
                                LoggerHelper.LogDebug($"Member joined: {newMember.Name}");
                            }
                        }

                        foreach (var oldMember in oldMembers)
                        {
                            if (!ListContainsMember(members, oldMember))
                            {
                                OnLeave?.Invoke(oldMember);
                                LoggerHelper.LogDebug($"Member left: {oldMember.Name}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError("Error processing join/leave events", ex);
                    }
                }

                oldMembers = members.ToList();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error updating CrossWorldPartyListSystem", ex);
            }
        }

        public struct CrossWorldMember
        {
            public string Name;
            public int PartyCount;
            public uint Level;
            public uint JobId;
        }
    }
}
