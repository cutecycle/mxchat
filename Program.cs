using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

class Program 
{
    async static Task RecognizeFromMic(SpeechConfig speechConfig)
    {
        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        //Asks user for mic input and prints transcription result on screen
        Console.WriteLine("Speak into your microphone.");
        var result = await recognizer.RecognizeOnceAsync();
        Console.WriteLine($"RECOGNIZED: Text={result.Text}");
    }

    async static Task Main(string[] args)
    {
        //Find your key and resource region under the 'Keys and Endpoint' tab in your Speech resource in Azure Portal
        //Remember to delete the brackets <> when pasting your key and region!
        var speechConfig = SpeechConfig.FromSubscription("<paste-your-resource-key>" "<paste-your-region>");
        await RecognizeFromMic(speechConfig);
    }
}
