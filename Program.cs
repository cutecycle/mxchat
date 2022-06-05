using System.Collections.Concurrent;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

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
	static Dictionary<String, SpeechRecognitionResult> KeyMap(List<String> keyList, ConcurrentStack<SpeechRecognitionResult> results)
	{
		// Dictionary<String, SpeechRecognitionResult> keyMap = new Dictionary<String, SpeechRecognitionResult>();
		IEnumerable<SpeechRecognitionResult> resultList = results.ToList();

		// Dictionary<String, SpeechRecognitionResult> keyMap = resultList.Select(result =>
		// new KeyValuePair<String, SpeechRecognitionResult>(result.Text, result)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

		Dictionary<String, SpeechRecognitionResult> keyMap = resultList
		// .Take(8)
		.Zip(keyList, (result, key) => new KeyValuePair<String, SpeechRecognitionResult>(key, result))
		.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		// .ToDictionary(kvp => kvp.Item1.Text, kvp => kvp.Item2);
		// .ToDictionary(
		// 	kvp => kvp.Item1,
		// 	kvp => kvp.Item2
		// );


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




	static ConcurrentStack<SpeechRecognitionResult> results = new ConcurrentStack<SpeechRecognitionResult>();
	static ConcurrentStack<String> events = new ConcurrentStack<String>();
	async static Task Main(string[] args)
	{
		// String key = Environment.GetEnvironmentVariable("COGNITIVE_SERVICES_KEY");
		Console.WriteLine("Paste your key:\n");
		String key = Console.ReadLine();
		if (key is null)
		{
			throw new ArgumentNullException("oof");
		}
		SpeechConfig speechConfig = SpeechConfig.FromSubscription(key, "eastus");
		// using AudioConfig speaker = AudioConfig.FromDefaultSpeakerOutput();
		// using AudioConfig mic = AudioConfig.FromDefaultMicrophoneInput();
		// using SfromSpeakerOutputRecognizer = new SpeechRecognizer(speechConfig, speaker);
		List<KeyValuePair<String, SpeechRecognizer>> recognizers =
		// (
		// 	(new List<AudioConfig>
		// 		{
		// 			speaker,
		// 			mic
		// 		}
		// 	)
		// 	.Select(config =>
		// 		new SpeechRecognizer(speechConfig, config)
		// 	)
		// )
		(new List<KeyValuePair<String, SpeechRecognizer>>
		{
			new KeyValuePair<String,SpeechRecognizer>("mic", new SpeechRecognizer(SpeechConfig.FromSubscription(key, "eastus"), AudioConfig.FromDefaultMicrophoneInput()))
			// ,
			// new KeyValuePair<String,SpeechRecognizer>("mic", new SpeechRecognizer(SpeechConfig.FromSubscription(key, "eastus"), AudioConfig.FromDefaultMicrophoneInput()))
		});


		// .ToList();
		// Parallel.ForEach(recognizers, (SpeechRecognizer rec) =>
		// {
		Console.WriteLine("we're in!");
		foreach (KeyValuePair<String, SpeechRecognizer> rec in recognizers)
		{
			await rec.Value.StartContinuousRecognitionAsync();
			rec.Value.Recognized += (
				(s, e) =>
					{
						if (e.Result.Reason == ResultReason.RecognizedSpeech)
						{
							results.Push(e.Result);
							try
							{
								Console.Clear();
							}
							catch { }
							Console.WriteLine($"latest result result from {rec.Key}: {e.Result.Text}\n\n\n");
							// Console.WriteLine(String.Join("\n", results.Take(8).Select(x => $"{x.Text}").ToList()));
							var mapString = KeyMap(keys, results);
							foreach (var x in mapString)
							{
								Console.WriteLine($"[Ctrl-{x.Key}]:\t {x.Value.Text}\n\n");
							}

							// Console.WriteLine(String.Join("\n", events.ToArray()));
						}
						else
						{
							// status.Enqueue()
							// status
							events.Append(e.Result.Reason.ToString());
						}
					}
				);
		}
		var stop = new TaskCompletionSource<int>();
		Console.CancelKeyPress += async (s, e) =>
		{
			e.Cancel = true;
			stop.SetResult(0);
			foreach (var rec in recognizers)
			{
				// await rec.Value.StopContinuousRecognitionAsync();
				// await rec.Value.StopContinuousRecognitionAsync();
			}
		};
		Task.WaitAny(stop.Task);


		// Dictionary<string, VoiceResult> board;

		// while (true)
		// {
		// 	await Recognize(recognizer);
		// }

		// on a loop, listen for audio input. then, immediately place the result in the queue and listen again.
		// await fromSpeakerOutputRecognizer.StartContinuousRecognitionAsync();
		// https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-recognize-speech?pivots=programming-language-csharp#use-continuous-recognition
		// on a loop, map each result to a keyboard key. limit the number of results to 8. 
		// if there are more than 8 results, then discard the oldest result.
	}
}
