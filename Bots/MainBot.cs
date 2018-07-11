// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Banko.Constants;
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

        private DialogSet _dialogs { get; } = null;

        /// <summary>
        /// A bot constructor that takes a configuration object.
        /// </summary>
        /// <param name="configuration">A configuration object containing information from our appsettings.json file.</param>
        public MainBot(IConfiguration configuration)
        {            
            // Create DialogSet
            _dialogs = ComposeRootDialog();

            // Create the LUIS recognizer for our model.
            var luisRecognizerOptions = new LuisRecognizerOptions { Verbose = true };
            var luisModel = new LuisModel(
                configuration[Keys.LuisModel],
                configuration[Keys.LuisSubscriptionKey],
                new Uri(configuration[Keys.LuisUriBase]),
                LuisApiVersion.V2);
            LuisRecognizer = new LuisRecognizer(luisModel, luisRecognizerOptions, null);
        }
      
        public async Task OnTurn(ITurnContext context)
        {
            // Get state
            var conversationInfo = ConversationState<ConversationInfo>.Get(context);

            // Establish dialog state from the conversation state.
            var dc = _dialogs.CreateContext(context, conversationInfo);

            // This bot handles only messages for simplicity. Ideally it should handle new conversation joins and other non message based activities. See the CafeBot examples
            if (context.Activity.Type == ActivityTypes.Message)
            {
                // Continue any current dialog.
                await dc.Continue();

                // If this is not a repsonse, start the main root dialog
                if (!context.Responded)
                {
                    await dc.Begin(nameof(MainBot));
                }
            }
            
        }


        private DialogSet ComposeRootDialog()
        {
            var dialogs = new DialogSet();

            dialogs.Add(nameof(MainBot), new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    var utterance = dc.Context.Activity.Text?.Trim().ToLowerInvariant();

                    if (!string.IsNullOrEmpty(utterance))
                    {
                        // Decide which dialog to start based on top scoring Luis intent
                        var luisResult = await LuisRecognizer.Recognize<BankoLuisModel>(utterance, new CancellationToken());

                        // Decide which dialog to start.
                        switch (luisResult.TopIntent().intent)
                        {
                            case BankoLuisModel.Intent.Balance:
                                await dc.Begin(nameof(BalanceDialog));
                                break;
                            case BankoLuisModel.Intent.Transfer:
                                var dialogArgs = new Dictionary<string, object>
                                {
                                    { Keys.LuisArgs, luisResult.Entities }
                                };
                                //await dc.Begin(nameof(TransferDialog), dialogArgs);
                                await dc.Context.SendActivity($"Transfer");
                                await next();
                                break;
                            case BankoLuisModel.Intent.None:
                            default:
                                await dc.Context.SendActivity($"I dont know what you want to do. Type `make a transfer` or `get a balance`.");
                                await next();
                                break;
                        }
                    }
                    else
                    {
                        await dc.End();
                    }
                }
            });

            // Add our child dialogs.
            dialogs.Add(nameof(BalanceDialog), BalanceDialog.Instance);

            return dialogs;
        }

    }
}
