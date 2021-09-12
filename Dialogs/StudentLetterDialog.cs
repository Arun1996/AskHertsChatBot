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
                LetterTypeStepAsync,
                ConfirmStepAsync,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> GetInfoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var StudentLetterDt = (StudentLetter)stepContext.Options;

            if (StudentLetterDt.studentId == null)
            {
                var promptMessage = MessageFactory.Text(StudentIdStepMsgText, StudentIdStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(StudentLetterDt.studentId, cancellationToken);

        }

        private async Task<DialogTurnResult> LetterTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var StudentLetterDt = (StudentLetter)stepContext.Options;
            StudentLetterDt.studentId = (string)stepContext.Result;

            //var reply = MessageFactory.Text("What type of letter do you want?");

            //reply.SuggestedActions = new SuggestedActions()
            //{
            //    Actions = new List<CardAction>()
            //    {
            //        new CardAction() { Title = "Bank Letter", Type = ActionTypes.ImBack, Value = "Bank Letter"},
            //        new CardAction() { Title = "Student status Letter", Type = ActionTypes.ImBack, Value = "Student status Letter"},
            //    },
            //};
            var card = new HeroCard
            {
                Text = "What type of letter do you want?",
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, title: "1. Bank Letter", value: "Bank Letter"),
                    new CardAction(ActionTypes.ImBack, title: "2. Student status Letter", value: "Student status Letter"),
                },
            };

            var reply = MessageFactory.Attachment(card.ToAttachment());

            if (StudentLetterDt.type == null)
            {
                //var promptMessage = MessageFactory.Text(reply, reply, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = (Activity)reply }, cancellationToken);
            }

            return await stepContext.NextAsync(StudentLetterDt.type, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var StudentLetterDt = (StudentLetter)stepContext.Options;
            StudentLetterDt.type = (string)stepContext.Result;

            var messageText = $"Please confirm, You need a {StudentLetterDt.type}";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var StudentLetterDt = (StudentLetter)stepContext.Options;
                return await stepContext.EndDialogAsync(StudentLetterDt, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }


    }
}
