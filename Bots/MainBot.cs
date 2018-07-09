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
                    var luisResult = await LuisRecognizer.Recognize<BankoLuisModel>(text, new CancellationToken());
                    Dictionary<string, object> lD = new Dictionary<string, object>();
                    if (luisResult != null)
                    {
                        lD.Add("luisResult", luisResult);
                    }

                    //top level dispatch
                    switch (luisResult.TopIntent().intent)
                    {
                        case BankoLuisModel.Intent.Balance:
                            //Cant work out how to have multiple dialogs yet 
                            //await dc.Begin(nameof(BalanceDialog), null);
                            var randomBalance = new Random().Next(00, 5000);
                            await context.SendActivity($"Your balance is {randomBalance}");
                            break;
                        case BankoLuisModel.Intent.Transfer:
                            var dialogArgs = new Dictionary<string, object>
                            {
                                { TransferDialog.Keys.LuisArgs, luisResult.Entities }
                            };
                            await dc.Begin(nameof(TransferDialog), dialogArgs);
                            break;
                        case BankoLuisModel.Intent.None:
                        default:
                            await context.SendActivity($"I dont know what you want to do. Type `make a transfer` or `get a balance` to get started.");
                            break;
                    }
                    
                }


            }

        }

    }
}
