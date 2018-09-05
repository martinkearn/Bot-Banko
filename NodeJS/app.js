// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { BotStateSet, BotFrameworkAdapter, MemoryStorage, ConversationState, UserState } = require('botbuilder');
const restify = require('restify');
const { LuisRecognizer } = require('botbuilder-ai');
require('dotenv').config();

const luisRecognizer = new LuisRecognizer({
    appId: process.env("LuisModel"),
    subscriptionKey: process.env("LuisSubscriptionKey"),
    serviceEndpoint: 'https://westeurope.api.cognitive.microsoft.com'
});

// Create server
let server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 3978, function () {
    console.log(`${server.name} listening to ${server.url}`);
});

// Create adapter
const adapter = new BotFrameworkAdapter({
    appId: process.env.MicrosoftAppId,
    appPassword: process.env.MicrosoftAppPassword
});

// Add state middleware
const storage = new MemoryStorage();
const convoState = new ConversationState(storage);
const userState = new UserState(storage);
adapter.use(new BotStateSet(convoState, userState));

// Add the recognizer to your bot
adapter.use(luisRecognizer);

// Listen for incoming requests 
server.post('/api/messages', (req, res) => {
    adapter.processActivity(req, res, async(context) => {
        if (context.activity.type === 'message') {
            const state = convoState.get(context);

            const luisResults = luisRecognizer.get(context);

            // Extract the top intent from LUIS and use it to select which dialog to start
            const topIntent = LuisRecognizer.topIntent(luisResults, "NotFound");
            switch (topIntent) {
                case 'Balance':                    
                    await context.sendActivity(`You reached the Balance intent.`);
                    break;
                case 'Transfer':                    
                    await context.sendActivity(`You reached the Transfer intent.`);
                    break;
                case 'None':
                    await context.sendActivity(`You reached the None intent.`);
                    break;
                default:
                    break;
            }

        } else {
            await context.sendActivity(`[${context.activity.type} event detected]`);
        }
    });
});