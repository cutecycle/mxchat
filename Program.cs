using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

class Program
{
	static List<String> keys = new List<string> {
		"a","s","d","f",
		"j","k","l",";",
		"z","x","c","v"
	};
	static bool enterMode = false;
	static ConcurrentStack<SpeechRecognitionResult> results = new ConcurrentStack<SpeechRecognitionResult>();
	static ConcurrentStack<String> events = new ConcurrentStack<String>();
	//todo: valuetuple?
	static Dictionary<String, String> roles = new Dictionary<string, string>{
			{"q", "Speaker"},
			{"w", "Facilitator"},
			{"e", "Participant"},
			{"r", "Other"}
		};
	static Dictionary<String, String> speakers = new Dictionary<string, string>{
			{"q", "Speaker"},
			{"w", "Facilitator"},
			{"e", "Participant"},
			{"r", "Other"}
		};
	static String speakerKey = "q";
	static String lastCopied = "";


	async static Task<SpeechRecognitionResult> Recognize(SpeechRecognizer recognizer)
	{
		SpeechRecognitionResult result = await recognizer.RecognizeOnceAsync();
		return result;
	}
	static Dictionary<String, SpeechRecognitionResult> KeyMap(List<String> keyList, ConcurrentStack<SpeechRecognitionResult> results)
	{
		// Dictionary<String, SpeechRecognitionResult> keyMap = new Dictionary<String, SpeechRecognitionResult>();
		IEnumerable<SpeechRecognitionResult> resultList = results.ToList();

		// Dictionary<String, SpeechRecognitionResult> keyMap = resultList.Select(result =>
		// new KeyValuePair<String, SpeechRecognitionResult>(result.Text, result)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

		Dictionary<String, SpeechRecognitionResult> keyMap = resultList
		.Take(keys.Count())
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
	[DllImport("user32.dll")]
	static extern void SetForegroundWindow(System.IntPtr hWnd);

	static void Paste(String content)
	{
		IntPtr targetWindow;
		try
		{
			targetWindow = Process.GetProcessesByName("teams")[0].MainWindowHandle;
		}
		catch
		{
			Console.WriteLine("Couldn't find a teams window. Pasting to notepad");
			targetWindow = Process.GetProcessesByName("notepad")[0].MainWindowHandle;
		}
		try
		{
			var thisTerminalWindow = Process.GetCurrentProcess().MainWindowHandle;
			Console.WriteLine("entering text in " + targetWindow.ToString());
			//send string to the target window and press enter
			SetForegroundWindow(targetWindow);
		}
		catch
		{
			Console.WriteLine("Couldn't return to the console window. quick, alt-tab it!");
			// SetForegroundWindow(thisTerminalWindow);
		}
		try
		{
			if (enterMode)
			{
				SendKeys.SendWait("{ENTER}");
			}

		}
		catch (Exception e)
		{
			Console.WriteLine("Couldn't paste.");
			Console.WriteLine(e);
		}
	}
	static void updateUI()
	{
		try
		{
			Console.Clear();
		}
		catch { }
		// Console.WriteLine($"latest result result from {rec.Key}: {e.Result.Text}\n\n\n");

		Console.WriteLine("Prepend Text with:\n");
		foreach (var y in roles)
		{
			if (speakerKey == y.Key)
			{
				Console.BackgroundColor = ConsoleColor.Green;
			}
			else
			{
				Console.ResetColor();
			}
			Console.WriteLine($"[{y.Key}]:\t({y.Value})\t{(speakers[y.Key] == y.Value ? "" : $"({speakers[y.Key]})")}");
		}
		Console.ResetColor();
		// Console.WriteLine("Press enter when inserting text? " + enterMode);
		int MaxLength = 90;
		Console.WriteLine(
			"Last line copied: " +
			lastCopied
				.Substring(
					0,
					Math.Min(lastCopied.Length, MaxLength)
				) +
				(lastCopied.Length > MaxLength ?
					"..." :
					""
				)
		);

		var mapString = KeyMap(keys, results);
		Console.WriteLine("\nRecent Lines:\n");
		foreach (var x in mapString)
		{
			Console.WriteLine($"[{x.Key}]:\t {x.Value.Text} ()\n\n");
		}

	}
	static String speaker = "";
	async static Task Main(string[] args)
	{
		SpeechConfig speechConfig;
		String key = Environment.GetEnvironmentVariable("COGNITIVE_SERVICES_KEY");
		roles["q"] = Environment.GetEnvironmentVariable("SPEAKER_NAME");
		roles["w"] = Environment.GetEnvironmentVariable("FACILITATOR_NAME");
		while (key is null)
		{
			Console.WriteLine("Paste your Azure Cognitive Services key and press enter:\n");
			key = Console.ReadLine();
			try
			{
				speechConfig = SpeechConfig.FromSubscription(key, "eastus");
				speechConfig.OutputFormat = OutputFormat.Detailed;
			}
			catch
			{
				key = null;
			}
		}
		List<KeyValuePair<String, SpeechRecognizer>> recognizers =
		(new List<KeyValuePair<String, SpeechRecognizer>>
		{
			new KeyValuePair<String,SpeechRecognizer>(
				"mic",
				new SpeechRecognizer(
					SpeechConfig.FromSubscription(key, "eastus"),
					AudioConfig.FromDefaultMicrophoneInput()
				)
			)
		});

		try
		{
			Console.Clear();
		}
		catch { }
		updateUI();

		foreach (KeyValuePair<String, SpeechRecognizer> rec in recognizers)
		{
			await rec.Value.StartContinuousRecognitionAsync();
			rec.Value.Recognized += (
				(s, e) =>
					{
						if (e.Result.Reason == ResultReason.RecognizedSpeech)
						{
							results.Push(e.Result);
							updateUI();
						}
						else
						{
							events.Append(e.Result.Reason.ToString());
						}
					}
				);
		}
		while (true)
		{
			if (Console.KeyAvailable)
			{
				var keyReceived = Console.ReadKey(true);
				if (keys.Contains(keyReceived.KeyChar.ToString()))
				{
					//TODO 
					String useKey = keyReceived.KeyChar.ToString();

					Console.WriteLine("received:" + useKey);
					String line;
					try
					{
						line = KeyMap(keys, results)[useKey].Text;
					}
					catch
					{
						line = "There was no line available in that register.";
					}
					bool richTextMode = false;
					String Txt;
					String unfancyText;
					// if (richTextMode)
					// {
					// 	Txt =
					// 	@"{\rtf1\ansi "
					// 	+ $@"**\b[mxchat]** [{speaker}]\b0:\t "
					// 	+ @"}";
					// 	unfancyText = $"{roles[speakerKey]} [{speakers[speakerKey]}]: ";

					// }
					// else
					// {
					Txt = @$"
						Version:0.9
						StartHTML:000125
						EndHTML:000260
						StartFragment:000209
						EndFragment:000222
						<HTML>
						<head>
						<title>HTML clipboard</title>
						</head>
						<body>
						<!–StartFragment–>
							<i>[mxchat]: {roles[speakerKey]}</i> <b>[{speakers[speakerKey]}]</b>: 	
						<!–EndFragment–>
						</body>
						</html>
						";
					unfancyText = $"[mxchat]: {roles[speakerKey]} [{speakers[speakerKey]}]: ";
					// }
					Txt += line;
					unfancyText += line;

					var thread = new Thread( () =>
					{
						Clipboard.SetText(unfancyText);
					});
					thread
						.SetApartmentState(ApartmentState.STA);
					thread
						.Start();
					thread
						.Join();
					lastCopied = unfancyText;
					updateUI();
				}
				if (roles.Keys.Contains(keyReceived.KeyChar.ToString()))
				{
					speaker = roles[keyReceived.KeyChar.ToString()];
					speakerKey = keyReceived.KeyChar.ToString();
					updateUI();
				}
			}
		}
	}
}
