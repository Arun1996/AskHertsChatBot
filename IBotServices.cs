// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.AI.Luis;
//using Microsoft.Bot.Builder.AI.Orchestrator;
using Microsoft.Bot.Builder.AI.QnA;

namespace Microsoft.BotBuilderSamples
{
    public interface IBotServices
    {
        //LuisRecognizer LuisHomeAutomationRecognizer { get; }
        
        //LuisRecognizer LuisWeatherRecognizer { get; }

        //LuisRecognizer LuisAskHertsRecognizer { get; }

        //LuisRecognizer LuisFlightBookingRecognizer { get; }

        //OrchestratorRecognizer Dispatch { get; }
        
        QnAMaker QnA { get; }
    }
}
