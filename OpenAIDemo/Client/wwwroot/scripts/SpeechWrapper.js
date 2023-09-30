/// <reference path="speechsdk-javascript-1.31.0/microsoft.cognitiveservices.speech.sdk.bundle.js" />
import * as sdk from "./speechsdk-javascript-1.31.0/microsoft.cognitiveservices.speech.sdk.bundle.js";

export async function sttFromMic(tokenObj) {
    const speechsdk = Window.SpeechSDK;

    const speechConfig = speechsdk.SpeechConfig.fromAuthorizationToken(tokenObj.AuthToken, tokenObj.Region);
    speechConfig.speechRecognitionLanguage = 'en-US';

    const audioConfig = speechsdk.AudioConfig.fromDefaultMicrophoneInput();
    const recognizer = new speechsdk.SpeechRecognizer(speechConfig, audioConfig);

    recognizer.recognizeOnceAsync(result => {
        let displayText;
        if (result.reason === ResultReason.RecognizedSpeech) {
            displayText = `RECOGNIZED: Text =${result.text}`
        }
        else {
            displayText = 'ERROR: Speech was cancelled or could not be recognized. Ensure your microphone is working properly.';
        }
        console.log(displayText)
    });
}