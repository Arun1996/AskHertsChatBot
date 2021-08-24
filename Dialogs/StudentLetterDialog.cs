// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class StudentLetterDialog : CancelAndHelpDialog
    {
        private const string StudentIdStepMsgText = "Please enter your student ID?";
        private const string PurposeStepMsgText = "What is the purpose of appointment?";
        private const string ProffStepMsgText = "Who would you like to have the appointment with?";

        public StudentLetterDialog()
            : base(nameof(StudentLetterDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            //AddDialog(new AdaptiveDialog(nameof(AdaptiveDialog)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                GetInfoStepAsync,
                PurposeStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> GetInfoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var cardResourcePath = "CoreBot.Cards.studentLetterCard.json";
            var studentLetterCard = CreateAdaptiveCardAttachment(cardResourcePath);

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Attachments = new List<Attachment>() {
                     studentLetterCard
                    },
                    Type = ActivityTypes.Message,
                    Text = "Please fill this form",
                }
            };

            return await stepContext.PromptAsync(InitialDialogId, opts, cancellationToken);
        }

        private async Task<DialogTurnResult> PurposeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var txt = stepContext.Context.Activity.Text;
            dynamic val = stepContext.Context.Activity.Value;
            var a = 1;

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var AppointmentDt = (Appointment)stepContext.Options;
            AppointmentDt.Date = (string)stepContext.Result;

            var messageText = $"Please confirm, Booking {AppointmentDt.purpose} with {AppointmentDt.professor} on: {AppointmentDt.Date}. Is this correct?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var AppointmentDt = (Appointment)stepContext.Options;

                return await stepContext.EndDialogAsync(AppointmentDt, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private Attachment CreateAdaptiveCardAttachment(string cardResourcePath)
        {

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }


    }
}
