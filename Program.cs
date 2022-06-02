using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Windows.Forms; //Dang i didn't know how fast we were gonna break dotnet core here

class Program
{
	static List<string> keys = new List<string> {
		"a","s","d","f",
		"j","k","l",";"
	};
	class VoiceResult
	{
		readonly String Speaker;
		readonly String RecognitionTime;
		readonly String Content;
		readonly String Source; // todo: make enum
		readonly SpeechRecognitionResult CognitiveServicesResponse;

	}

	class VoiceResults
	{
		Dictionary<String, VoiceResult> RecognitionHistory;
	}
	async static Task<SpeechRecognitionResult> Recognize(SpeechRecognizer recognizer)
	{
		//Asks user for mic input and prints transcription result on screen
		// Console.WriteLine("Speak into your microphone.");
		SpeechRecognitionResult result = await recognizer.RecognizeOnceAsync();
		// Console.WriteLine($"RECOGNIZED: Text={result.Text}");
		return result;
	}

	// async static Task<List<VoiceResult>> Listen() { 

	// }
	static Dictionary<String, SpeechRecognitionResult> KeyMap(List<String> keyList, ConcurrentQueue<SpeechRecognitionResult> results)
	{
		// Dictionary<String, SpeechRecognitionResult> keyMap = new Dictionary<String, SpeechRecognitionResult>();
		IEnumerable<SpeechRecognitionResult> resultList = results.ToList();

		Dictionary<String, SpeechRecognitionResult> keyMap = resultList.Select(result =>
		new KeyValuePair<String, SpeechRecognitionResult>(result.Text, result)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		return keyMap;
	}
	static String MapString(Dictionary<String, SpeechRecognitionResult> keyMap)
	{
		String mappedString = ""
		+ keyMap.Select(
			x => $"{x.Key}: {x.Value.Text}"
		);
		return mappedString;
	}


	static ConcurrentQueue<SpeechRecognitionResult> results = new ConcurrentQueue<SpeechRecognitionResult>();
	async static Task Main(string[] args)
	{
		//Find your key and resource region under the 'Keys and Endpoint' tab in your Speech resource in Azure Portal
		//Remember to delete the brackets <> when pasting your key and region!
		var speechConfig = SpeechConfig.FromSubscription(
		Environment.GetEnvironmentVariable("COGNITIVE_SERVICES_KEY"),
			"eastus"
			);
		using var speaker = AudioConfig.FromDefaultSpeakerOutput();
		using var mic = AudioConfig.FromDefaultMicrophoneInput();
		using var recognizer = new SpeechRecognizer(speechConfig, speaker);

		// Dictionary<string, VoiceResult> board;

		// while (true)
		// {
		// 	await Recognize(recognizer);
		// }

		// on a loop, listen for audio input. then, immediately place the result in the queue and listen again.
		await recognizer.StartContinuousRecognitionAsync();
		// https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-recognize-speech?pivots=programming-language-csharp#use-continuous-recognition
		recognizer.Recognized += (
			(s, e) =>
			{
				if (e.Result.Reason == ResultReason.RecognizedSpeech)
				{
					results.Enqueue(e.Result);
					Console.Clear();
					Console.WriteLine(MapString(KeyMap(keys, results)));
				}
			}
		);
		// on a loop, map each result to a keyboard key. limit the number of results to 8. 
		// if there are more than 8 results, then discard the oldest result.

	}
}
