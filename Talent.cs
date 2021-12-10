namespace HoloScrape;

internal class Talent
{
	public string? id { get; set; }
	public string? oshiMark { get; set; }
	public string? name { get; set; }
	public string? quote { get; set; }
	public string? name_en { get; set; }
	public string? quote_en { get; set; }
	public List<Social> socials { get; set; } = new();
	public List<string> outfits { get; set; } = new();
	public Icon? icon { get; set; }
}
