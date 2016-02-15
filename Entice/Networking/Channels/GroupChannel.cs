﻿using Entice.Base;
using Entice.Entities;
using GuildWarsInterface;
using GuildWarsInterface.Datastructures;
using GuildWarsInterface.Datastructures.Agents;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Entice.Channels
{
    internal class GroupChannel : Channel
    {
        public GroupChannel()
                : base("group")
        {
        }

        public void Merge(PlayerCharacter player)
        {
            Send("merge", o => { o.target = Entity.Players.First(p => p.Character == player).Id; });
        }

        public void Kick(PlayerCharacter player)
        {
            Send("kick", o => { o.target = Entity.Players.First(p => p.Character == player).Id; });
        }

        public override void HandleMessage(Message message)
        {
            PlayerCharacter groupLeader;
            Party party;
            switch (message.Event)
            {
                case "update":
                {
                    bool newParty = false;
                    groupLeader =  Entity.GetEntity<Player>(Guid.Parse(message.Payload.leader.ToString())).Character;
                    party = Game.Zone.Parties.FirstOrDefault(x => x.Leader == groupLeader);
                    if (party == null)
                    {
                        party = new Party(groupLeader);
                        newParty = true;
                    }

                    AddMembersToParty(party, message.Payload.members);
                    AddOrRemoveInvites(party, message.Payload.invites);
                    GenerateJoinRequests(groupLeader, message.Payload.invites);

                    if (newParty)
                        Game.Zone.AddParty(party);
                }
                    break;
                case "remove":
                {
                    //party.RemoveMember();
                }
                    break;
                case "map:change":
                    break;
            }
        }

        private void AddMembersToParty(Party party, JArray members)
        {
            string[] items = members.Select(jv => (string)jv).ToArray();
            foreach (string entityId in items)
            {
                PlayerCharacter groupmember = Entity.GetEntity<Player>(Guid.Parse(entityId)).Character;
                party.AddMember(groupmember);
            }
        }

        private void AddOrRemoveInvites(Party party, JArray invites)
        {
            string[] items = invites.Select(jv => (string)jv).ToArray();
            foreach (string entityId in items)
            {
                PlayerCharacter invite = Entity.GetEntity<Player>(Guid.Parse(entityId)).Character;
                Party partyOfInvitedMember =
                    Game.Zone.Parties.FirstOrDefault(x => x.Members.Contains(invite) || x.Leader == invite);
                party.AddInvite(partyOfInvitedMember);
            }
        }

        private void GenerateJoinRequests(PlayerCharacter groupLeader, JArray invites)
        {
            foreach (string leaderid in invites.Select(y=> (string)y).ToArray())
            { 
                PlayerCharacter requestedGroupJoinLeader = Entity.GetEntity<Player>(Guid.Parse(leaderid)).Character;
                Party partyOfRequestedGroupJoinLeader = Game.Zone.Parties.FirstOrDefault(x => x.Leader == requestedGroupJoinLeader);
                Party ownParty = Game.Zone.Parties.FirstOrDefault(x => x.Leader == groupLeader);
                partyOfRequestedGroupJoinLeader?.AddJoinRequest(ownParty);
            }
        }
    }
}