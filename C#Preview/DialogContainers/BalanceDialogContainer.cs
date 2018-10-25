using Microsoft.Bot.Builder.Dialogs;
using System;

namespace Banko.DialogContainers
{
    public class BalanceDialogContainer : DialogContainer
    {
        public static BalanceDialogContainer Instance { get; } = new BalanceDialogContainer();

        private BalanceDialogContainer() : base(nameof(BalanceDialogContainer))
        {
            // Define and add the waterfall steps for our dialog.
            this.Dialogs.Add(nameof(BalanceDialogContainer), new WaterfallStep[]
            {
                // Begin a check balance.
                async (dc, args, next) =>
                {
                    var randomBalance = new Random().Next(00, 5000);
                    await dc.Context.SendActivity($"Your balance is £{Convert.ToDecimal(randomBalance)}");

                    await next();
                },
                async (dc, args, next) =>
                {
                    // Prompt the user to do something else
                    await dc.Context.SendActivity("OK, we're done here. What is next?");
                },
                async (dc, args, next) =>
                {
                    await dc.End();
                }
            });
        }
    }
}
