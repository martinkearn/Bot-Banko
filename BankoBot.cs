// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Banko.Dialogs;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Ai.LUIS;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;
using Microsoft.Cognitive.LUIS;
using Microsoft.Extensions.Configuration;

namespace Banko
{
    public class BankoBot : IBot
    {


        /// <summary>
        /// Singleton reference to the LUIS app and model.
        /// </summary>
        private LuisRecognizer Recognizer { get; } = null;

        /// <summary>
        /// A bot constructor that takes a configuration object.
        /// </summary>
        /// <param name="configuration">A configuration object containing information from our appsettings.json file.</param>
        public BankoBot(IConfiguration configuration)
        {
            // Create the LUIS recognizer for our model.
            var luisRecognizerOptions = new LuisRecognizerOptions { Verbose = true };
            var luisModel = new LuisModel(
                configuration["LuisModel"],
                configuration["LuisSubscriptionKey"],
                new Uri(configuration["LuisUriBase"]),
                LuisApiVersion.V2);
            Recognizer = new LuisRecognizer(luisModel, luisRecognizerOptions, null);
        }

        /// <summary>
        /// Every Conversation turn for our EchoBot will call this method. In here
        /// the bot checks the Activty type to verify it's a message, bumps the 
        /// turn conversation 'Turn' count, and then echoes the users typing
        /// back to them. 
        /// </summary>
        /// <param name="context">Turn scoped context containing all the data needed
        /// for processing this conversation turn. </param>        
        public async Task OnTurn(ITurnContext context)
        {
            // Handle message and non-message activities differently.
            if (context.Activity.Type != ActivityTypes.Message)
            {
                // Handle any non-message activity.
                await HandleSystemActivity(context);
            }
            else
            {
                // Get conversation state and establish a dialog context.
                var state = ConversationState<ConversationData>.Get(context);
                var dc = TransferDialog.Instance.CreateContext(context, state.DialogState);

                // Capture any input text.
                var text = context.Activity.AsMessageActivity()?.Text?.Trim().ToLowerInvariant();


                if (!context.Responded)
                {
                    // Continue any active dialog. If there's no active dialog, this is a no-op.
                    await dc.Continue();
                }

                if (!context.Responded)
                {
                    // Use LUIS to extract intent and entities from the user's input text.
                    var luisResult = await Recognizer.Recognize<BankoLuisModel>(text, new CancellationToken());
                    Dictionary<string, object> lD = new Dictionary<string, object>();
                    if (luisResult != null)
                    {
                        lD.Add("luisResult", luisResult);
                    }
                    await context.SendActivity($"Top intent {luisResult.TopIntent().intent} with score {luisResult.TopIntent().score}. ");

                    //extract entities if they exist
                    var accountLabel = luisResult.Entities.AccountLabel?[0].ToString();
                    var money = luisResult.Entities.money?[0].Number;
                    var payee = luisResult.Entities.Payee?[0].ToString();

                    //top level dispatch
                    switch (luisResult.TopIntent().intent)
                    {
                        case BankoLuisModel.Intent.Balance:
                            var randomBalance = new Random().Next(00, 5000);
                            await context.SendActivity($"Your balance is {randomBalance}");
                            break;
                        case BankoLuisModel.Intent.Transfer:
                            //hard coding some entity values if they are null. TO DO use dialog to fill the gapes here
                            //var accountLabelResolved = accountLabel ?? "Current";
                            //var moneyResolved = (money != -1) ? money : new Random().Next(10, 500);
                            //await context.SendActivity($"I'll transfer {money} from {accountLabelResolved}");
                            //break;
                            // Start the "transfer" dialog. Pass in any entities that our LUIS model captured.
                            var dialogArgs = new Dictionary<string, object>();
                            dialogArgs.Add(TransferDialog.Keys.LuisArgs, luisResult.Entities);
                            await dc.Begin(nameof(TransferDialog), dialogArgs);
                            break;
                        case BankoLuisModel.Intent.None:
                        default:
                            await context.SendActivity($"I dont know what you want to do.");
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Handle any non-message activities from the channel.
        /// </summary>
        /// <param name="context">The context object for this turn.</param>
        private async Task HandleSystemActivity(ITurnContext context)
        {
            switch (context.Activity.Type)
            {
                // Not all channels send a ConversationUpdate activity.
                // However, both the Emulator and WebChat do.
                case ActivityTypes.ConversationUpdate:

                    // If a user is being added to the conversation, send them an initial greeting.
                    var update = context.Activity.AsConversationUpdateActivity();
                    if (update.MembersAdded.Any(member => member.Id != update.Recipient.Id))
                    {
                        await context.SendActivities(
                            new IActivity[]
                            {
                                MessageFactory.Text("Hello, I'm the Banko bot."),
                                MessageFactory.Text("How can I help you? (Type `make a transfer` or `get a balance`.)")
                            });
                    }
                    break;
            }
        }
    }    
}
