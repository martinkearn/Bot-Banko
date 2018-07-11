using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Banko.Dialogs
{
    public class BalanceDialog : DialogContainer
    {
        public static BalanceDialog Instance { get; } = new BalanceDialog();

        private BalanceDialog() : base(nameof(BalanceDialog))
        {
            // Define and add the waterfall steps for our dialog.
            this.Dialogs.Add(nameof(BalanceDialog), new WaterfallStep[]
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
                    await dc.End();
                }
            });
        }
    }
}
