using sd_proxy.DTOs;
using System;
using System.Linq;

namespace sd_proxy
{
	public class QueueItem
	{
		public string prompt { get; set; }
		public string negativePrompt { get; set; }
		public int cfgScale { get; set; }
		public int samplingSteps { get; set; }
		public DateTime finishedAt { get; set; }
		public string sessionId { get; set; }

		public QueueItem(GenerateRequest req)
		{
			prompt = req.prompt;
			cfgScale = req.cfg_scale;
			samplingSteps = req.sampling_steps;
			negativePrompt = req.negative_prompt;

			GenerateSessionId();
		}

		public QueueItem()
		{
			GenerateSessionId();
		}

		private void GenerateSessionId()
		{
			Random random = new Random();
			const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
			sessionId = new string(Enumerable.Repeat(chars, 20)
				.Select(s => s[random.Next(s.Length)]).ToArray());
		}
	}
}
