using System.Collections.Generic;

namespace Banko.Models
{
    /// <summary>
    /// Conversation state information.
    /// We are also using this directly for dialog state, which needs an <see cref="IDictionary{string, object}"/> object.
    /// </summary>
    public class ConversationInfo : Dictionary<string, object> { }
}
