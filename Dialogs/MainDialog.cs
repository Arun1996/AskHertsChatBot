// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.Models;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Luis.AskHerts;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder.AI.QnA.Dialogs;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    class FollowUpCheckResult
    {
        [JsonProperty("answers")]
        public FollowUpCheckQnAAnswer[] Answers
        {
            get;
            set;
        }
    }

    class FollowUpCheckQnAAnswer
    {
        [JsonProperty("context")]
        public FollowUpCheckContext Context
        {
            get;
            set;
        }
    }

    class FollowUpCheckContext
    {
        [JsonProperty("prompts")]
        public FollowUpCheckPrompt[] Prompts
        {
            get;
            set;
        }
    }

    class FollowUpCheckPrompt
    {
        [JsonProperty("displayText")]
        public string DisplayText
        {
            get;
            set;
        }
    }

    public class MainDialog : ComponentDialog
    {
        private readonly BotService _luisRecognizer;
        protected readonly ILogger Logger;
        private bool QnAFlag=false;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(BotService luisRecognizer, AppointmentDialog appointmentDialog, IBotServices services, IConfiguration configuration, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(appointmentDialog);
            AddDialog(new QnAMakerBaseDialog(luisRecognizer, configuration));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            //var weekLaterDate = DateTime.Now.AddDays(7).ToString("MMMM d, yyyy");
            //var messageText = stepContext.Options?.ToString() ?? $"What can I help you with today?\nSay something like \"Book a flight from Paris to Berlin on {weekLaterDate}\"";
            //var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            if (!QnAFlag)
            {
                // Use the text provided in FinalStepAsync or the default if it is the first time.
                var weekLaterDate = DateTime.Now.AddDays(7).ToString("MMMM d, yyyy");
                var messageText = stepContext.Options?.ToString() ?? $"What can I help you with today?";
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            else
            {
                QnAFlag = false;
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = null }, cancellationToken);
            }

            
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                return await stepContext.BeginDialogAsync(nameof(BookingDialog), new BookingDetails(), cancellationToken);
            }

            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<AskHerts>(stepContext.Context, cancellationToken);
            var qnaResult = await _luisRecognizer.QnA.GetAnswersAsync(stepContext.Context);

            var topIntent = Intent.None;
            var qnaScore = qnaResult.Any() ? qnaResult[0].Score:0;
            if (qnaScore > luisResult.TopIntent().score)
            {
                topIntent = Intent.QnA;
            }
            else
            {
                topIntent = luisResult.TopIntent().intent;
            }

            switch (topIntent)
            {
                case AskHerts.Intent.BookAppointment:

                    var AppointmentDt = new Appointment()
                    {
                        studentId = null,
                        professor = luisResult.Entities?._instance?.professor?.FirstOrDefault().Text.ToString(),
                        purpose = luisResult.Entities?._instance?.purpose?.FirstOrDefault().Text.ToString(),
                        Date = luisResult.Entities.datetime?.FirstOrDefault()?.Expressions.FirstOrDefault()?.Split('T')[0]
                    };
                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(AppointmentDialog), AppointmentDt, cancellationToken);

                case AskHerts.Intent.OfficeHours:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var getWeatherMessageText = "OfficeHours";
                    var getWeatherMessage = MessageFactory.Text(getWeatherMessageText, getWeatherMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
                    break;

                case AskHerts.Intent.StudentLetter:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    getWeatherMessageText = "StudentLetter";
                    getWeatherMessage = MessageFactory.Text(getWeatherMessageText, getWeatherMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
                    break;

                case AskHerts.Intent.QnA:
                    QnAFlag = true;
                    //return await stepContext.BeginDialogAsync(nameof(QnAMakerDialog), null, cancellationToken);
                    await ProcessQnAAsync(stepContext, cancellationToken, qnaResult);
                    break;
                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {topIntent})";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);

                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        // Shows a warning if the requested From or To cities are recognized as entities but they are not in the Airport entity list.
        // In some cases LUIS will recognize the From and To composite entities as a valid cities but the From and To Airport values
        // will be empty if those entity values can't be mapped to a canonical item in the Airport.
        private static async Task ShowWarningForUnsupportedCities(ITurnContext context, FlightBooking luisResult, CancellationToken cancellationToken)
        {
            var unsupportedCities = new List<string>();

            var fromEntities = luisResult.FromEntities;
            if (!string.IsNullOrEmpty(fromEntities.From) && string.IsNullOrEmpty(fromEntities.Airport))
            {
                unsupportedCities.Add(fromEntities.From);
            }

            var toEntities = luisResult.ToEntities;
            if (!string.IsNullOrEmpty(toEntities.To) && string.IsNullOrEmpty(toEntities.Airport))
            {
                unsupportedCities.Add(toEntities.To);
            }

            if (unsupportedCities.Any())
            {
                var messageText = $"Sorry but the following airports are not supported: {string.Join(',', unsupportedCities)}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await context.SendActivityAsync(message, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
            // the Result here will be null.
            if (stepContext.Result is Appointment result)
            {
                // Now we have all the booking details call the booking service.

                // If the call to the booking service was successful tell the user.

                var timeProperty = new TimexProperty(result.Date);
                var ApptDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
                var messageText = $"I have you booked {result.purpose} with {result.professor} on {ApptDateMsg}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);

        }

        private async Task ProcessQnAAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken, QueryResult[] queryResults)
        {
            //QnAFlag = true;

            if (queryResults.Any())
            {
                var followUpCheckHttpClient = new HttpClient();
                followUpCheckHttpClient.DefaultRequestHeaders.Add("Authorization", "6c076096-5a49-4057-b175-4837c307deaf");
                var url = $"{"https://askhertsqna1.azurewebsites.net/qnamaker"}/knowledgebases/{"5c55797f-0872-441c-9686-854d99bcaa2d"}/generateAnswer";

                // post query
                var checkFollowUpJsonResponse = await followUpCheckHttpClient.PostAsync(url, new StringContent("{\"question\":\"" + stepContext.Context.Activity.Text + "\"}", Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

                // parse result
                var followUpCheckResult = JsonConvert.DeserializeObject<FollowUpCheckResult>(checkFollowUpJsonResponse);

                // initialize reply message containing the default answer
                var reply = MessageFactory.Text(queryResults[0].Answer);

                if (followUpCheckResult.Answers.Length > 0 && followUpCheckResult.Answers[0].Context.Prompts.Length > 0)
                {
                    // if follow-up check contains valid answer and at least one prompt, add prompt text to SuggestedActions using CardAction one by one
                    reply.SuggestedActions = new SuggestedActions();
                    reply.SuggestedActions.Actions = new List<CardAction>();
                    for (int i = 0; i < followUpCheckResult.Answers[0].Context.Prompts.Length; i++)
                    {
                        var promptText = followUpCheckResult.Answers[0].Context.Prompts[i].DisplayText;
                        reply.SuggestedActions.Actions.Add(new CardAction() { Title = promptText, Type = ActionTypes.ImBack, Value = promptText });
                    }
                }

                await stepContext.Context.SendActivityAsync(reply, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry, could not find an answer to your question"), cancellationToken);
            }

        }
    }
}
