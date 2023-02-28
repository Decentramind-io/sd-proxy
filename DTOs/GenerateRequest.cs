namespace sd_proxy.DTOs
{
	public class GenerateRequest
	{
		public string prompt { get; set; }
		public string negative_prompt { get; set; }
		public int cfg_scale { get; set; }
		public int sampling_steps { get; set; }
	}
}
