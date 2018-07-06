using System.Collections.Generic;

namespace Banko.Models
{
    /// <summary>
    /// Class for storing conversation state.
    /// </summary>
    public class ConversationData
    {
        /// <summary>
        /// Property for storing dialog state for the book a table dialog.
        /// </summary>
        public Dictionary<string, object> DialogState { get; set; } = new Dictionary<string, object>();
    }
}
