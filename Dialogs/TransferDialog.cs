using Banko.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Prompts;
using Microsoft.Bot.Builder.Prompts.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Banko.Dialogs
{
    /// <summary>
    /// Defines the dialog for doing a transfer
    /// </summary>
    public class TransferDialog : DialogSet
    {
        /// <summary>
        /// Defines a singleton instance of the dialog.
        /// </summary>
        public static TransferDialog Instance { get; } = new Lazy<TransferDialog>(new TransferDialog()).Value;

        /// <summary>
        /// The names of the inputs and prompts in this dialog.
        /// </summary>
        /// <remarks>We'll store the information gathered using these same names.</remarks>
        public struct Keys
        {
            /// <summary>
            ///  Key to use for LUIS entities as input.
            /// </summary>
            public const string LuisArgs = "LuisEntities";

            public const string AccountLabel = "AccountLabel";
            public const string Money = "money";
            //public const string Payee = "Payee";
            //public const string Date = "datetimeV2";
            public const string Confirm = "confirmation";
        }

        private static Task MoneyValidator(ITurnContext context, NumberResult<int> toValidate)
        {
            if (toValidate.Value < 0)
            {
                toValidate.Status = PromptStatus.TooSmall;
            }
            else
            {
                toValidate.Status = PromptStatus.Recognized;
            }

            return Task.CompletedTask;
        }


        /// <summary>
        /// Creates a new dialog instance.
        /// </summary>
        private TransferDialog()
        {
            // Add the prompts we'll be using in our dialog.
            Add(Keys.AccountLabel, new Microsoft.Bot.Builder.Dialogs.TextPrompt());
            Add(Keys.Money, new Microsoft.Bot.Builder.Dialogs.NumberPrompt<int>(Culture.English, MoneyValidator));
            //Add(Keys.Payee, new Microsoft.Bot.Builder.Dialogs.TextPrompt());
            //Add(Keys.Date, new Microsoft.Bot.Builder.Dialogs.DateTimePrompt(Culture.English, null));
            Add(Keys.Confirm, new Microsoft.Bot.Builder.Dialogs.ConfirmPrompt(Culture.English));

            // Define and add the waterfall steps for our dialog.
            Add(nameof(TransferDialog), new WaterfallStep[]
            {
                // Begin a transfer.
                async (dc, args, next) =>
                {
                    // Initialize state.
                    if(args!=null && args.ContainsKey(Keys.LuisArgs))
                    {
                        // Add any LUIS entities to the active dialog state. Remove any values that don't validate, and convert the remainder to a dictionary.
                        var entities = (BankoLuisModel._Entities)args[Keys.LuisArgs];
                        dc.ActiveDialog.State = ValidateLuisArgs(entities);
                    }
                    else
                    {
                        // Begin without any information collected.
                        dc.ActiveDialog.State = new Dictionary<string,object>();
                    }

                    // Display user's choice
                    await dc.Context.SendActivity("OK, we're going to make a transfer.");

                    await next();
                },
                async (dc, args, next) =>
                {
                    // Verify or ask for Money
                    if (dc.ActiveDialog.State.ContainsKey(Keys.Money))
                    {
                        await next();
                    }
                    else
                    {
                        var promptOptions = new PromptOptions(){RetryPromptString = "How much do you want to transfer? You can say a number, for example 23, 100, 10 or ten (BUG support Int only, not a Double or Decimal. Dont use currency symbols)"};
                        await dc.Prompt(Keys.Money,"How much?", promptOptions);
                    }
                },
                async (dc, args, next) =>
                {
                    // Capture Money to state
                    if (!dc.ActiveDialog.State.ContainsKey(Keys.Money))
                    {
                        //BUG: will not recognise double or decimal ... only int, need to research
                        var answer = (int)args["Value"];
                        dc.ActiveDialog.State[Keys.Money] = answer;
                    }

                    await next();
                },
                async (dc, args, next) =>
                {
                    // Verify or ask for AccountLabel
                    if (dc.ActiveDialog.State.ContainsKey(Keys.AccountLabel))
                    {
                        await next();
                    }
                    else
                    {
                        var promptOptions = new PromptOptions(){RetryPromptString = "Which account do you want to transfer from? For exmaple Joint, Current, Savings etc"};
                        await dc.Prompt(Keys.AccountLabel,"Which account?", promptOptions);
                    }
                },
                async (dc, args, next) =>
                {
                    // Capture AccountLabel to state
                    if (!dc.ActiveDialog.State.ContainsKey(Keys.AccountLabel))
                    {
                        var answer = (string)args["Value"];
                        dc.ActiveDialog.State[Keys.AccountLabel] = answer;
                    }

                    await next();
                },
                async (dc, args, next) =>
                {
                    // Confirm the transfer.
                    var promptOptions = new PromptOptions(){RetryPromptString = "Should I make the transfer for you? Please enter `yes` or `no`."};
                    var promptBody = $"Ok, I'll transfer `{dc.ActiveDialog.State[Keys.Money]}` from `{dc.ActiveDialog.State[Keys.AccountLabel]}`, is this correct?";
                    await dc.Prompt(Keys.Confirm, promptBody, promptOptions);
                },
                async (dc, args, next) =>
                {
                    // Make the transfer or cancel the operation.
                    var confirmed = (bool)args["Confirmation"];
                    if (confirmed)
                    {
                        // Send a confirmation message: the typing activity indicates to the user that the bot is working on something, the delay simulates a process that takes some time, and the message simulates a confirmation message generated by the process.
                        var typing = Activity.CreateTypingActivity();
                        var delay = new Activity { Type = "delay", Value = 3000 };
                        await dc.Context.SendActivities(
                            new IActivity[]
                            {
                                typing, delay,
                                MessageFactory.Text("Your transfer is scheduled. Reference number: #K89HG38SZ")
                            });
                    }
                    else
                    {
                        // Cancel the reservation.
                        await dc.Context.SendActivity("OK, we have canceled the transfer.");
                    }

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

        /// <summary>
        /// Check whether each entity is valid and return valid ones in a dictionary.
        /// </summary>
        /// <param name="entities">The LUIS entities from the input arguments.</param>
        /// <returns>A dictionary of the valid entities.</returns>
        private Dictionary<string, object> ValidateLuisArgs(BankoLuisModel._Entities entities)
        {
            var result = new Dictionary<string, object>();

            // Check AccountLabel
            if (entities?.AccountLabel?.Any() is true)
            {
                var accountLabel = entities.AccountLabel.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
                if (accountLabel != null)
                {
                    result[Keys.AccountLabel] = accountLabel;
                }
            }

            // Check Money
            if (entities?.money?.Any() is true)
            {
                var number = entities.money.FirstOrDefault().Number;
                if (number != 0.0)
                {
                    // LUIS recognizes numbers as doubles. Convert to decimal.
                    result[Keys.Money] = Convert.ToDecimal(number);
                }
            }

            return result;
        }
    }
}
