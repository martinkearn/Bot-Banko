using Banko.Constants;
using Banko.Helpers;
using Banko.Models;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Prompts;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using System.Collections.Generic;

namespace Banko.DialogContainers
{
    public class TransferDialogContainer : DialogContainer
    {

        public static TransferDialogContainer Instance { get; } = new TransferDialogContainer();

        private TransferDialogContainer() : base(nameof(TransferDialogContainer))
        {
            // Define and add the waterfall steps for our dialog.
            this.Dialogs.Add(nameof(TransferDialogContainer), new WaterfallStep[]
            {
                // Begin a transfer.
                async (dc, args, next) =>
                {
                    // Initialize state.
                    if(args!=null && args.ContainsKey(Keys.LuisArgs))
                    {
                        // Add any LUIS entities to the active dialog state. Remove any values that don't validate, and convert the remainder to a dictionary.
                        var entities = (BankoLuisModel._Entities)args[Keys.LuisArgs];
                        dc.ActiveDialog.State = Validators.LuisValidator(entities);
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
                        //BUG: will not recognise double or decimal as a valid number response ... only int, need to research why this is
                        var answer = (int)args["Value"];
                        dc.ActiveDialog.State[Keys.Money] = answer;
                    }

                    await next();
                },
                async (dc, args, next) =>
                {
                    // Verify or ask for Date
                    if (dc.ActiveDialog.State.ContainsKey(Keys.Date))
                    {
                        // If we already have the transfer date, continue on to the next waterfall step.
                        await next();
                    }
                    else
                    {
                        // Otherwise, query for the transfer date and time.
                        await dc.Prompt(Keys.Date,
                            "When do you want the transfer to happen?", new PromptOptions
                            {
                                RetryPromptString = "Please enter a date and time for the transfer."
                            });
                    }

                },
                async (dc, args, next) =>
                {
                    // Capture Date to state
                    if (!dc.ActiveDialog.State.ContainsKey(Keys.Date))
                    {
                        // Update state with the prompt result.
                        // The prompt can return multiple interpretations of the date entered. For now, just use the first one.
                        var answer = args["Resolution"] as List<DateTimeResult.DateTimeResolution>;
                        var date = Converters.TimexToDateConverter(answer[0].Timex);
                        dc.ActiveDialog.State[Keys.Date] = date.ToLongDateString();
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
                    // Verify or ask for Payee
                    if (dc.ActiveDialog.State.ContainsKey(Keys.Payee))
                    {
                        await next();
                    }
                    else
                    {
                        var promptOptions = new PromptOptions(){RetryPromptString = "Who is the payee. This needs to be the name of a person or company you have already setup. i.e. Martin Kearn or BT"};
                        await dc.Prompt(Keys.AccountLabel,"Who is the payee?", promptOptions);
                    }
                },
                async (dc, args, next) =>
                {
                    // Capture Payee to state
                    if (!dc.ActiveDialog.State.ContainsKey(Keys.Payee))
                    {
                        var answer = (string)args["Value"];
                        dc.ActiveDialog.State[Keys.Payee] = answer;
                    }

                    await next();
                },
                async (dc, args, next) =>
                {
                    // Confirm the transfer.
                    var promptOptions = new PromptOptions(){RetryPromptString = "Should I make the transfer for you? Please enter `yes` or `no`."};
                    var promptBody = $"Ok, I'll transfer `{dc.ActiveDialog.State[Keys.Money]}` from `{dc.ActiveDialog.State[Keys.AccountLabel]}` on `{dc.ActiveDialog.State[Keys.Date]}` to `{dc.ActiveDialog.State[Keys.Payee]}`, is this correct?";
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
                },
                async (dc, args, next) =>
                {
                    await dc.End();
                }
            });

            // Add the prompts and child dialogs
            this.Dialogs.Add(Keys.AccountLabel, new Microsoft.Bot.Builder.Dialogs.TextPrompt());
            this.Dialogs.Add(Keys.Money, new Microsoft.Bot.Builder.Dialogs.NumberPrompt<int>(Culture.English, Validators.MoneyValidator));
            this.Dialogs.Add(Keys.Date, new Microsoft.Bot.Builder.Dialogs.DateTimePrompt(Culture.English, Validators.DateTimeValidator));
            this.Dialogs.Add(Keys.Payee, new Microsoft.Bot.Builder.Dialogs.TextPrompt());
            this.Dialogs.Add(Keys.Confirm, new Microsoft.Bot.Builder.Dialogs.ConfirmPrompt(Culture.English));
        }


    }
}
