// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Ai.LUIS;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;
using Microsoft.Cognitive.LUIS;
using Microsoft.Extensions.Configuration;

namespace AspNetCore_EchoBot_With_State
{
    public class EchoBot : IBot
    {
        /// <summary>
        /// Singleton reference to the LUIS app and model.
        /// </summary>
        private LuisRecognizer Recognizer { get; } = null;

        /// <summary>
        /// A bot constructor that takes a configuration object.
        /// </summary>
        /// <param name="configuration">A configuration object containing information from our appsettings.json file.</param>
        public EchoBot(IConfiguration configuration)
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
            // This bot is only handling Messages
            if (context.Activity.Type == ActivityTypes.Message)
            {
                // Use LUIS to extract intent and entities from the user's input text.
                var luisResult = await Recognizer.Recognize<BankoLuisModel>(context.Activity.Text, new CancellationToken());

                await context.SendActivity($"Top intent {luisResult.TopIntent().intent} with score {luisResult.TopIntent().score}. ");

                //extract entities if they exist
                var accountLabel = luisResult.Entities.AccountLabel?[0].ToString();
                var money = luisResult.Entities.money?[0].Number ?? -1;
                var payee = luisResult.Entities.Payee?[0].ToString();

                switch (luisResult.TopIntent().intent.ToString().ToLower())
                {
                    case "balance":
                        var randomBalance = new Random().Next(00, 5000);
                        await context.SendActivity($"Your balance is {randomBalance}");
                        break;
                    case "transfer":
                        //hard coding some entity values if they are null. TO DO use dialog to fill the gapes here
                        var accountLabelResolved = accountLabel ?? "Current";
                        var moneyResolved = (money != -1) ? money : new Random().Next(10, 500);
                        await context.SendActivity($"I'll transfer {money} from {accountLabelResolved}");
                        break;
                    case "none":
                        break;
                    default:
                        break;
                }
            }
        }
    }    
}
