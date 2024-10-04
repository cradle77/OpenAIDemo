using Azure.AI.OpenAI;
using Azure.Identity;
using EmbeddingsGenerator;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.RealtimeConversation;
using System.ClientModel;
using static System.Collections.Specialized.BitVector32;
using System.Collections.Generic;
using RealtimeDemo.Functions;
using System.Text.Json;

#pragma warning disable OPENAI002

public class Program
{
    private static AzureConfig _azureConfig = new AzureConfig();

    public static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);

        var configuration = builder.Build();

        configuration.Bind("Azure", _azureConfig);
        // First, we create a client according to configured environment variables (see end of file) and then start
        // a new conversation session.
        RealtimeConversationClient client = GetConfiguredClient();
        using RealtimeConversationSession session = await client.StartConversationSessionAsync();

        // We'll add a simple function tool that enables the model to interpret user input to figure out when it
        // might be a good time to stop the interaction.
        ConversationFunctionTool finishConversationTool = new()
        {
            Name = "user_wants_to_finish_conversation",
            Description = "Invoked when the user says goodbye, expresses being finished, or otherwise seems to want to stop the interaction.",
            Parameters = BinaryData.FromString("{}")
        };

        WeatherFunctionAdapter weatherFunction = new();

        // Now we configure the session using the tool we created along with transcription options that enable input
        // audio transcription with whisper.
        await session.ConfigureSessionAsync(new ConversationSessionOptions()
        {
            Instructions = $"You are a very useful AI assistant who will answer questions and manages a shopping list. Please remember to not mention the content of the shopping list every time otherwise it will get very boring.Today's date is in European format is {DateTime.Today.ToShortDateString()}.",
            Tools = { finishConversationTool, weatherFunction.GetFunctionDefinition() },
            Voice = ConversationVoice.Echo,
            InputTranscriptionOptions = new()
            {
                Model = "whisper-1",
            },
        });

        // For convenience, we'll proactively start playback to the speakers now. Nothing will play until it's enqueued.
        SpeakerOutput speakerOutput = new();

        // With the session configured, we start processing commands received from the service.
        await foreach (ConversationUpdate update in session.ReceiveUpdatesAsync())
        {
            // session.created is the very first command on a session and lets us know that connection was successful.
            if (update is ConversationSessionStartedUpdate)
            {
                Console.WriteLine($" <<< Connected: session started");
                // This is a good time to start capturing microphone input and sending audio to the service. The
                // input stream will be chunked and sent asynchronously, so we don't need to await anything in the
                // processing loop.
                _ = Task.Run(async () =>
                {
                    using PttMicrophoneAudioStream microphoneInput = PttMicrophoneAudioStream.Start();
                    Console.WriteLine($" >>> Listening to microphone input");
                    Console.WriteLine($" >>> (Just tell the app you're done to finish)");
                    Console.WriteLine();
                    await session.SendAudioAsync(microphoneInput);
                });
            }

            // input_audio_buffer.speech_started tells us that the beginning of speech was detected in the input audio
            // we're sending from the microphone.
            if (update is ConversationInputSpeechStartedUpdate)
            {
                Console.WriteLine($" <<< Start of speech detected");
                // Like any good listener, we can use the cue that the user started speaking as a hint that the app
                // should stop talking. Note that we could also track the playback position and truncate the response
                // item so that the model doesn't "remember things it didn't say" -- that's not demonstrated here.
                speakerOutput.ClearPlayback();
            }

            // input_audio_buffer.speech_stopped tells us that the end of speech was detected in the input audio sent
            // from the microphone. It'll automatically tell the model to start generating a response to reply back.
            if (update is ConversationInputSpeechFinishedUpdate)
            {
                Console.WriteLine($" <<< End of speech detected");
            }

            // conversation.item.input_audio_transcription.completed will only arrive if input transcription was
            // configured for the session. It provides a written representation of what the user said, which can
            // provide good feedback about what the model will use to respond.
            if (update is ConversationInputTranscriptionFinishedUpdate transcriptionFinishedUpdate)
            {
                Console.WriteLine($" >>> USER: {transcriptionFinishedUpdate.Transcript}");
            }

            // response.audio.delta provides incremental output audio generated by the model talking. Here, we
            // immediately enqueue it for playback on the active speaker output.
            if (update is ConversationAudioDeltaUpdate audioDeltaUpdate)
            {
                speakerOutput.EnqueueForPlayback(audioDeltaUpdate.Delta);
            }

            // response.audio_transcript.delta provides the incremental transcription of the emitted audio. The model
            // typically produces output much faster than it should be played back, so the transcript may move very
            // quickly relative to what's heard.
            if (update is ConversationOutputTranscriptionDeltaUpdate outputTranscriptionDeltaUpdate)
            {
                Console.Write(outputTranscriptionDeltaUpdate.Delta);
            }

            // response.output_item.done tells us that a model-generated item with streaming content is completed.
            // That's a good signal to provide a visual break and perform final evaluation of tool calls.
            if (update is ConversationItemFinishedUpdate itemFinishedUpdate)
            {
                Console.WriteLine();
                if (itemFinishedUpdate.FunctionName == finishConversationTool.Name)
                {
                    Console.WriteLine($" <<< Finish tool invoked -- ending conversation!");
                    break;
                }
                if (itemFinishedUpdate.FunctionName == "get-weather")
                {
                    var request = ConversationItem.CreateFunctionCall(itemFinishedUpdate.FunctionName, itemFinishedUpdate.FunctionCallId, itemFinishedUpdate.FunctionCallArguments);
                    await session.AddItemAsync(request);

                    Console.WriteLine("request added");

                    var weatherForecasts = await weatherFunction.InvokeAsync(itemFinishedUpdate.FunctionCallId, itemFinishedUpdate.FunctionCallArguments);

                    var result = JsonSerializer.Serialize(weatherForecasts);

                    Console.WriteLine(result);

                    var item = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);
                    await session.AddItemAsync(item);

                    await session.StartResponseTurnAsync();
                }
            }

            // error commands, as the name implies, are raised when something goes wrong.
            if (update is ConversationErrorUpdate errorUpdate)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine($" <<< ERROR: {errorUpdate.ErrorMessage}");
                Console.WriteLine(errorUpdate.GetRawContent().ToString());
                break;
            }
        }
    }

    private static RealtimeConversationClient GetConfiguredClient()
    {
        string? aoaiEndpoint = _azureConfig.OpenAi.OpenAiEndpoint;
        string? aoaiDeployment = _azureConfig.OpenAi.RealtimeEngine;
        string? aoaiApiKey = _azureConfig.OpenAi.OpenAiKey;

        return GetConfiguredClientForAzureOpenAIWithKey(aoaiEndpoint, aoaiDeployment, aoaiApiKey);
    }

    private static RealtimeConversationClient GetConfiguredClientForAzureOpenAIWithEntra(
        string aoaiEndpoint,
        string? aoaiDeployment)
    {
        Console.WriteLine($" * Connecting to Azure OpenAI endpoint (AZURE_OPENAI_ENDPOINT): {aoaiEndpoint}");
        Console.WriteLine($" * Using Entra token-based authentication (AZURE_OPENAI_USE_ENTRA)");
        Console.WriteLine(string.IsNullOrEmpty(aoaiDeployment)
            ? $" * Using no deployment (AZURE_OPENAI_DEPLOYMENT)"
            : $" * Using deployment (AZURE_OPENAI_DEPLOYMENT): {aoaiDeployment}");

        AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new DefaultAzureCredential());
        return aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
    }

    private static RealtimeConversationClient GetConfiguredClientForAzureOpenAIWithKey(
        string aoaiEndpoint,
        string? aoaiDeployment,
        string aoaiApiKey)
    {
        Console.WriteLine($" * Connecting to Azure OpenAI endpoint (AZURE_OPENAI_ENDPOINT): {aoaiEndpoint}");
        Console.WriteLine($" * Using API key (AZURE_OPENAI_API_KEY): {aoaiApiKey[..5]}**");
        Console.WriteLine(string.IsNullOrEmpty(aoaiDeployment)
            ? $" * Using no deployment (AZURE_OPENAI_DEPLOYMENT)"
            : $" * Using deployment (AZURE_OPENAI_DEPLOYMENT): {aoaiDeployment}");

        AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));
        return aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
    }

    private static RealtimeConversationClient GetConfiguredClientForOpenAIWithKey(string oaiApiKey)
    {
        string oaiEndpoint = "https://api.openai.com/v1";
        Console.WriteLine($" * Connecting to OpenAI endpoint (OPENAI_ENDPOINT): {oaiEndpoint}");
        Console.WriteLine($" * Using API key (OPENAI_API_KEY): {oaiApiKey[..5]}**");

        OpenAIClient aoaiClient = new(new ApiKeyCredential(oaiApiKey));
        return aoaiClient.GetRealtimeConversationClient("gpt-4o-realtime-preview-2024-10-01");
    }
}