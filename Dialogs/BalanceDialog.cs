using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Banko.Dialogs
{
    public class BalanceDialog : DialogSet
    {
        /// <summary>
        /// Defines a singleton instance of the dialog.
        /// </summary>
        public static BalanceDialog Instance { get; } = new Lazy<BalanceDialog>(new BalanceDialog()).Value;

        /// <summary>
        /// Creates a new dialog instance.
        /// </summary>
        private BalanceDialog()
        {
            // Define and add the waterfall steps for our dialog.
            Add(nameof(BalanceDialog), new WaterfallStep[]
            {
                // Begin a check balance.
                async (dc, args, next) =>
                {
                    var randomBalance = new Random().Next(00, 5000);
                    await dc.Context.SendActivity($"Your balance is {randomBalance}");

                    await next();
                },
                async (dc, args, next) =>
                {
                    // Prompt the user to do something else
                    await dc.Context.SendActivity("OK, we're done here. What is next?");

                    // No await Next(); because this is the end of the dialog so we don't want to wait for anything
                }
            });
        }
    }
}
