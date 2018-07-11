// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Banko.Dialogs;
using Banko.Models;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Ai.LUIS;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Cognitive.LUIS;
using Microsoft.Extensions.Configuration;

namespace Banko.Bots
{
    public class MainBot : IBot
    {
        /// <summary>
        /// Singleton reference to the LUIS app and model.
        /// </summary>
        private LuisRecognizer LuisRecognizer { get; } = null;

        private DialogSet _dialogs { get; } = ComposeMainDialog();

        private const string MainMenuId = "mainMenu";

        private const string LuisResultKey = "luisResult";

        /// <summary>
        /// A bot constructor that takes a configuration object.
        /// </summary>
        /// <param name="configuration">A configuration object containing information from our appsettings.json file.</param>
        public MainBot(IConfiguration configuration)
        {
            // Create the LUIS recognizer for our model.
            var luisRecognizerOptions = new LuisRecognizerOptions { Verbose = true };
            var luisModel = new LuisModel(
                configuration["LuisModel"],
                configuration["LuisSubscriptionKey"],
                new Uri(configuration["LuisUriBase"]),
                LuisApiVersion.V2);
            LuisRecognizer = new LuisRecognizer(luisModel, luisRecognizerOptions, null);
        }
      
        public async Task OnTurn(ITurnContext context)
        {
            // This bot handles only messages for simplicity. Ideally should handle new conversation joins and other non message based activities things. See the CafeBot examples
            if (context.Activity.Type == ActivityTypes.Message)
            {
                // Capture any input text.
                var text = context.Activity.AsMessageActivity()?.Text?.Trim().ToLowerInvariant();

                // Get the user and conversation state from the turn context.
                var conversationData = ConversationState<ConversationData>.Get(context);

                // Establish dialog state from the conversation state.
                var dc = _dialogs.CreateContext(context, conversationData);

                // Continue any current dialog.
                await dc.Continue();

                if (!context.Responded)
                {

                    // Use LUIS to extract intent and entities from the user's input text.
                    var luisResult = await LuisRecognizer.Recognize<BankoLuisModel>(text, new CancellationToken());
                    if (luisResult != null)
                    {
                        conversationData[LuisResultKey] = luisResult;
                    }

                    // Start main dialog.
                    await dc.Begin(MainMenuId);
                }


            }

        }



        /// <summary>
        /// Composes a main dialog for our bot.
        /// </summary>
        /// <returns>A new main dialog.</returns>
        private static DialogSet ComposeMainDialog()
        {
            var dialogs = new DialogSet();

            dialogs.Add(MainMenuId, new WaterfallStep[]
            {
                //async (dc, args, next) =>
                //{
                //    await dc.Context.SendActivity($"I dont know what you want to do. Type `make a transfer` or `get a balance` to get started.");
                //    await next();
                //},
                async (dc, args, next) =>
                {
                    //get luis result from conversation data
                    var luisResult = (BankoLuisModel)ConversationState<ConversationData>.Get(dc.Context)[LuisResultKey];

                    // Decide which dialog to start.
                    switch (luisResult.TopIntent().intent)
                    {
                        case BankoLuisModel.Intent.Balance:
                            await dc.Begin(nameof(BalanceDialog));
                            break;
                        case BankoLuisModel.Intent.Transfer:
                            //var dialogArgs = new Dictionary<string, object>
                            //{
                            //    { TransferDialog.Keys.LuisArgs, luisResult.Entities }
                            //};
                            //await dc.Begin(nameof(TransferDialog), dialogArgs);
                            await dc.Context.SendActivity($"Transfer");
                            break;
                        case BankoLuisModel.Intent.None:
                        default:
                            await dc.Context.SendActivity($"I dont know what you want to do. Type `make a transfer` or `get a balance` to get started.");
                            await next();
                            break;
                    }
                    //var result = (args["Activity"] as Activity)?.Text?.Trim().ToLowerInvariant();
                    //switch (result)
                    //{
                    //    case "reserve table":
                    //        await dc.Begin(ReserveTable.Id);
                    //        break;
                    //    case "wake up":
                    //        await dc.Begin(WakeUp.Id);
                    //        break;
                    //    default:
                    //        await dc.Context.SendActivity("Sorry, I don't understand that command. Please choose an option from the list below.");
                    //        await next();
                    //        break;
                    //}
                },
                async (dc, args, next) =>
                {


                    // Show the main menu again.
                    await dc.Replace(MainMenuId);
                }
            });

            // Add our child dialogs.
            dialogs.Add(nameof(BalanceDialog), BalanceDialog.Instance);

            return dialogs;
        }

    }
}
