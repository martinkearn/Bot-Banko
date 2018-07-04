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
                // Get the conversation state from the turn context
                var state = context.GetConversationState<EchoState>();

                // Bump the turn count. 
                state.TurnCount++;

                // Echo back to the user whatever they typed.
                await context.SendActivity($"Turn {state.TurnCount}: You sent '{context.Activity.Text}'");

                // Use LUIS to extract intent and entities from the user's input text.
                var luisResult = await Recognizer.Recognize<HowHappyLUISModel>(context.Activity.Text, new CancellationToken());

                var entities = string.Empty;
                if (luisResult.Entities.happiness != null) entities += luisResult.Entities.happiness[0];
                if (luisResult.Entities.neutral != null) entities += luisResult.Entities.neutral[0];
                if (luisResult.Entities.sadness != null) entities += luisResult.Entities.sadness[0];
                if (luisResult.Entities.surprise != null) entities += luisResult.Entities.surprise[0];
                if (luisResult.Entities.anger != null) entities += luisResult.Entities.anger[0];
                if (luisResult.Entities.contempt != null) entities += luisResult.Entities.contempt[0];
                if (luisResult.Entities.disgust != null) entities += luisResult.Entities.disgust[0];
                if (luisResult.Entities.emotion != null) entities += luisResult.Entities.emotion.ToString();

                await context.SendActivity($"Top intent {luisResult.TopIntent().intent} with score {luisResult.TopIntent().score}. Entities: {entities}");
            }
        }
    }    
}
