using System;

namespace sd_proxy
{
	public class ReadyItem
	{
		public DateTime finishedAt { get; set; }
		public string sessionId { get; set; }
		public byte[] img { get; set; }
	}
}
