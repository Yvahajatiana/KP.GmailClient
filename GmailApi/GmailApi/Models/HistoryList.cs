﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace GmailApi.Models
{
    public class HistoryList
    {
        public HistoryList()
        {
            Messages = new List<Message>(0);
            NextPageToken = string.Empty;
        }

        /// <summary>
        /// List with messages, with only the ID and/or ThreadID set.
        /// </summary>
        [JsonProperty("messages")]
        public List<Message> Messages { get; set; }

        [JsonProperty("messagesAdded[message]")]
        public List<Message> MessagesAdded { get; set; }

        [JsonProperty("messagesDeleted[message]")]
        public List<Message> MessagesDeleted { get; set; }

        //TODO:
        //labelsAdded
        //labelsRemoved

        /// <summary>
        /// The mailbox sequence ID.
        /// </summary>
        [JsonProperty("historyId")]
        public ulong HistoryId { get; set; }

        /// <summary>
        /// Token to retrieve the next page of results in the list.
        /// </summary>
        [JsonProperty("nextPageToken")]
        public string NextPageToken { get; set; }

        public override string ToString()
        {
            return string.Concat("# Messages: ", Messages.Count, ", NextPageToken: ", NextPageToken, ", History ID: ", HistoryId);
        }
    }
}
