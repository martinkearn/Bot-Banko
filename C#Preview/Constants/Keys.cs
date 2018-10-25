using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Banko.Constants
{
    /// <summary>
    /// The names of the inputs and prompts in this dialog.
    /// </summary>
    /// <remarks>We'll store the information gathered using these same names.</remarks>
    public struct Keys
    {
        /// <summary>
        ///  Key to use for LUIS entities as input.
        /// </summary>
        public const string LuisModel = "LuisModel";
        public const string LuisSubscriptionKey = "LuisSubscriptionKey";
        public const string LuisUriBase = "LuisUriBase";
        public const string LuisArgs = "LuisEntities";
        public const string LuisResult = "LuisResult";
        public const string AccountLabel = "AccountLabel";
        public const string Money = "money";
        public const string Payee = "Payee";
        public const string Date = "datetimeV2";
        public const string Confirm = "confirmation";
    }
}
