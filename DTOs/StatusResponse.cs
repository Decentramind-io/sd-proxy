namespace sd_proxy.DTOs
{
	public class StatusResponse
	{
		public string status { get; set; }
		public int queue_length { get; set; }
		public int queued_before_me { get; set; }
	}
}
