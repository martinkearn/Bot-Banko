// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { BotStateSet, BotFrameworkAdapter, MemoryStorage, ConversationState, UserState } = require('botbuilder');
const restify = require('restify');
const { LuisRecognizer } = require('botbuilder-ai');
const { DialogSet } = require('botbuilder-dialogs');
require('dotenv').config();

const luisRecognizer = new LuisRecognizer({
    appId: process.env.LuisModel,
    subscriptionKey: process.env.LuisSubscriptionKey,
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
const conversationState = new ConversationState(new MemoryStorage());
adapter.use(conversationState);

// Add the recognizer to your bot
adapter.use(luisRecognizer);

// register some dialogs for usage with the intents detected by the LUIS app
const dialogs = new DialogSet();

dialogs.add('BalanceDialog', [
    async (dialogContext) => {
        await dialogContext.context.sendActivity(`Your balance is £20.`);
        await dialogContext.end();
    }
]);

dialogs.add('TransferDialog', [
    async (dialogContext) => {
        await dialogContext.context.sendActivity(`Transfer.`);
        await dialogContext.end();
    }
]);

// Listen for incoming requests 
server.post('/api/messages', (req, res) => {
    adapter.processActivity(req, res, async(context) => {
        if (context.activity.type === 'message') {
            const state = conversationState.get(context);
            const dc = dialogs.createContext(context, state);

            const luisResults = luisRecognizer.get(context);

            // Extract the top intent from LUIS and use it to select which dialog to start
            const topIntent = LuisRecognizer.topIntent(luisResults, "NotFound");
            switch (topIntent) {
                case 'Balance':                    
                    await dc.begin("BalanceDialog", luisResults);
                    break;
                case 'Transfer':                    
                    await dc.begin("TransferDialog", luisResults);
                    break;
                case 'None':
                    await context.sendActivity(`I dont know what you want to do. Type "make a transfer" or "get a balance".`);
                    break;
                default:
                    break;
            }

        }
    });
});