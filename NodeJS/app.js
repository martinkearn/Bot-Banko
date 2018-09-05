// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { BotStateSet, BotFrameworkAdapter, MemoryStorage, ConversationState, UserState } = require('botbuilder');
const restify = require('restify');
const { LuisRecognizer } = require('botbuilder-ai');
//require('dotenv').config();

const model = new LuisRecognizer({
    // This appID is for a public app that's made available for demo purposes
    // You can use it by providing your LUIS subscription key
     appId: process.env.LuisModel,
    // replace subscriptionKey with your Authoring Key
    // your key is at https://www.luis.ai under User settings > Authoring Key 
    subscriptionKey: process.env.LuisSubscriptionKey,
    // The serviceEndpoint URL begins with "https://<region>.api.cognitive.microsoft.com", where region is the region associated with the key you are using. Some examples of regions are `westus`, `westcentralus`, `eastus2`, and `southeastasia`.
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

// Listen for incoming requests 
server.post('/api/messages', (req, res) => {
    // Route received request to adapter for processing
    adapter.processActivity(req, res, (context) => {
        if (context.activity.type === 'message') {
            const state = convoState.get(context);
            const count = state.count === undefined ? state.count = 0 : ++state.count;
            //let testSecret = process.env("TESTSECRET");
            return context.sendActivity(`${count}: You said "${context.activity.text}"`);
        } else {
            return context.sendActivity(`[${context.activity.type} event detected]`);
        }
    });
});