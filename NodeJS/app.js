const { BotStateSet, BotFrameworkAdapter, MemoryStorage, TurnContext, ConversationState, UserState } = require('botbuilder');
const {
    DialogSet,
    TextPrompt,
    ChoicePrompt,
    ConfirmPrompt,
    DatetimePrompt,
    FoundChoice,
    FoundDatetime,
    ListStyle
} = require('botbuilder-dialogs');
const restify = require('restify');
const { LuisRecognizer } = require('botbuilder-ai');
require('dotenv').config();

const model = new LuisRecognizer({
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
const storage = new MemoryStorage();
const convoState = new ConversationState(storage);
const userState = new UserState(storage);
adapter.use(new BotStateSet(convoState, userState));

// Listen for incoming requests
server.post('/api/messages', (req, res) => {
    // Route received request to adapter for processing
    adapter.processActivity(req, res, async context => {
        if (context.activity.type === 'message') {
            const state = convoState.get(context);
            const utterance = (context.activity.text || '').trim().toLowerCase();

            // Create dialog context
            const dc = dialogs.createContext(context, state);

            // Call LUIS model
            await model
                .recognize(context)
                .then(async res => {
                    // Resolve intents returned from LUIS
                    let topIntent = LuisRecognizer.topIntent(res);
                    state.Intent = topIntent;

                    // Start Transfer dialog
                    if (topIntent === 'Transfer') {
                        // Resolve entities returned from LUIS, and save to state
                        let accountLabel = res.entities['AccountLabel'];
                        state.AccountLabel = (accountLabel) ? accountLabel : null;

                        return dc.begin('TransferDialog');

                        // Start Balance
                    } else if (topIntent === 'Balance') {
                        return dc.begin('BalanceDialog');

                        // Continue current dialog
                    } else {
                        return dc.continue().then(async res => {
                            // Return default message if nothing replied.
                            if (!context.responded) {
                                await context.sendActivity(`I dont know what you want to do. Type 'make a transfer' or 'get a balance'.`);
                            }
                        });
                    }
                })
                .catch(err => {
                    console.log(err);
                });
        }
    });
});

// // Helper function for finding a specified entity. entityResults are the results from LuisRecognizer.get(context)
// function findEntities(entityName, entityResults) {
//     let entities = []
//     if (entityName in entityResults) {
//         entityResults[entityName].forEach(entity => {
//             entities.push(entity);
//         });
//     }
//     return entities.length > 0 ? entities : undefined;
// }

// register some dialogs for usage with the intents detected by the LUIS app
const dialogs = new DialogSet();

dialogs.add('textPrompt', new TextPrompt());

dialogs.add('BalanceDialog', [
    async function(dc){
        let balance = Math.floor(Math.random() * Math.floor(100));
        await dc.context.sendActivity(`Your balance is £${balance}.`);
        await dc.continue();
    },
    async function(dc){
        await dc.context.sendActivity(`OK, we're done here. What is next?`);
        await dc.continue();
    },
    async function(dc){
        await dc.end();
    }
]);

dialogs.add('TransferDialog', [
    async function(dc) {
        const state = convoState.get(dc.context);
        if (state.AccountLabel) {
            await dc.continue();
        } else {
            await dc.prompt('textPrompt', `Which account do you want to transfer from? For example Joint, Current, Savings etc`);
        }
    },
    async function(dc, accountLabel) {
        const state = convoState.get(dc.context);
        // Save accountLabel
        if (!state.AccountLabel) {
            state.AccountLabel = accountLabel;
        }
        
        //continue
        await dc.continue();
    },
    async function(dc) {
        const state = convoState.get(dc.context);
        await dc.context.sendActivity(`AccountLabel: ${state.AccountLabel}`);

        //continue
        await dc.continue();
    },    
    async function(dc){
        await dc.context.sendActivity(`OK, we're done here. What is next?`);
        await dc.continue();
    },
    async function(dc){
        await dc.end();
    }
]);