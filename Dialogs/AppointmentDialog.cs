// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using CoreBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class AppointmentDialog : CancelAndHelpDialog
    {
        private const string StudentIdStepMsgText = "Please enter your student ID?";
        private const string PurposeStepMsgText = "What is the purpose of appointment?";
        private const string ProffStepMsgText = "Who would you like to have the appointment with?";

        public AppointmentDialog()
            : base(nameof(AppointmentDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                StudentIdStepAsync,
                PurposeStepAsync,
                ApptProfAsync,
                ApptDateStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> StudentIdStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var AppointmentDt = (Appointment)stepContext.Options;

            if (AppointmentDt.studentId == null)
            {
                var promptMessage = MessageFactory.Text(StudentIdStepMsgText, StudentIdStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(AppointmentDt.studentId, cancellationToken);
        }

        private async Task<DialogTurnResult> PurposeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var AppointmentDt = (Appointment)stepContext.Options;
            AppointmentDt.studentId = (string)stepContext.Result;

            if (AppointmentDt.purpose == null)
            {
                if (AppointmentDt.professor == null)
                {
                    var promptMessage = MessageFactory.Text(PurposeStepMsgText, PurposeStepMsgText, InputHints.ExpectingInput);
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
                else 
                {
                    AppointmentDt.purpose = "one-to-one";
                }
                    
            }

            return await stepContext.NextAsync(AppointmentDt.purpose, cancellationToken);
        }

        private async Task<DialogTurnResult> ApptProfAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var AppointmentDt = (Appointment)stepContext.Options;
            AppointmentDt.purpose = (string)stepContext.Result;

            if (AppointmentDt.professor == null)
            {
                var promptMessage = MessageFactory.Text(ProffStepMsgText, ProffStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(AppointmentDt.professor, cancellationToken);
        }

        private async Task<DialogTurnResult> ApptDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var AppointmentDt = (Appointment)stepContext.Options;
            AppointmentDt.professor = (string)stepContext.Result;

            if (AppointmentDt.Date == null || IsAmbiguous(AppointmentDt.Date))
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), AppointmentDt.Date, cancellationToken);
            }

            return await stepContext.NextAsync(AppointmentDt.Date, cancellationToken);
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

        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }
    }
}
