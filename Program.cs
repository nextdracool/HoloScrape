using AngleSharp;
using AngleSharp.Dom;
using System.Text.Json;
using System.Text.Unicode;

using HoloScrape;

Dictionary<string, List<string>> talentMap = new()
{
	["gen0"] = new()
	{
		"Tokino_Sora",
		"Roboco",
		"Sakura_Miko",
		"Hoshimachi_Suisei",
		"AZKi"
	},
	["gen1"] = new()
	{
		"Yozora_Mel",
		"Shirakami_Fubuki",
		"Natsuiro_Matsuri",
		"Aki_Rosenthal",
		"Akai_Haato",
	},
	["gen2"] = new()
	{
		"Minato_Aqua",
		"Murasaki_Shion",
		"Nakiri_Ayame",
		"Yuzuki_Choco",
		"Oozora_Subaru",
	},
	["gamers"] = new()
	{
		"Shirakami_Fubuki",
		"Ookami_Mio",
		"Nekomata_Okayu",
		"Inugami_Korone",
	},
	["gen3"] = new()
	{
		"Usada_Pekora",
		"Uruha_Rushia",
		"Shiranui_Flare",
		"Shirogane_Noel",
		"Houshou_Marine",
	},
	["gen4"] = new()
	{
		"Amane_Kanata",
		"Tsunomaki_Watame",
		"Tokoyami_Towa",
		"Himemori_Luna",
	},
	["gen5"] = new()
	{
		"Yukihana_Lamy",
		"Momosuzu_Nene",
		"Shishiro_Botan",
		"Omaru_Polka",
	},
	["gen6"] = new()
	{
		"La+_Darknesss",
		"Takane_Lui",
		"Hakui_Koyori",
		"Sakamata_Chloe",
		"Kazama_Iroha",
	}
};


var context = new BrowsingContext(Configuration.Default.WithDefaultLoader());

foreach (var (gen, talents) in talentMap)
{
	foreach (var id in talents)
	{
		Console.WriteLine($"Processing {id}");
		var directory = $"./hololive/{gen}/{id}/";
		Directory.CreateDirectory(directory);
		var talent = new Talent { id = id };

		var document = await context.OpenAsync("https://hololive.wiki/wiki/" + Uri.EscapeDataString(id));
		var infobox = document.QuerySelector("table.infobox");

		if (infobox is not null)
		{
			ProcessInformation(talent, infobox);
			await ProcessOutfits(directory, talent, infobox);
			talent.icon = new()
			{
				borderColor = "",
				backgroundColor = "",
			};
		}

		File.WriteAllText(
			directory + "_data.json",
			JsonSerializer.Serialize(
				talent,
				options: new(JsonSerializerDefaults.Web)
				{
					WriteIndented = true,
					Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All)
				}
			)
		);
	}
}

static void ProcessInformation(Talent talent, IElement infobox)
{
	var children = infobox.FirstElementChild?.Children;
	if (children is not null)
	{
		ProcessQuotes(talent, children);
		foreach (var child in children)
		{
			var header = child.QuerySelector("th");
			switch (header?.TextContent.Trim().ToLowerInvariant().Replace(" ", ""))
			{
				case "japanesename":
					talent.name = header.NextElementSibling?.TextContent.Trim();
					break;
				case "englishname":
					talent.name_en = header.NextElementSibling?.TextContent.Trim();
					break;
				case "emoji/oshimark":
					talent.oshiMark = header.NextElementSibling?.TextContent.Trim();
					break;
				case "youtube":
				case "twitter":
				case "spotify":
				case "bilibili":
				case "twitcasting":
				case "marshmallow":
					var social = new Social
					{
						service = header.TextContent.Trim()
					};
					var a = header.NextElementSibling?.QuerySelector("a");
					social.url = a?.GetAttribute("href");
					social.label = a?.TextContent?.Trim();
					if (social.url is not null && social.label is not null && social.service is not null)
						talent.socials.Add(social);
					break;
				default:
					break;
			}
		}
		talent.name ??= talent.name_en;
	}
}

static void ProcessQuotes(Talent talent, IHtmlCollection<IElement> children)
{
	if (
		children.Length > 1 &&
		children[1].Children.Length > 0 &&
		children[1].Children[0].Children.Length > 1)
	{
		var quotes = children[1]?.Children[0]?.Children[1];
		if (quotes is not null)
		{
			var split = quotes.InnerHtml.Split("<br>", 2);
			if (split.Length > 1)
			{
				talent.quote_en = split[0].Trim();
				talent.quote = split[1].Replace("<br>", "\n").Trim();
			}
			else
			{
				talent.quote = split[0].Trim();
			}
		}
	}
}

static async Task ProcessOutfits(string directory, Talent talent, IElement infobox)
{
	var tablist = infobox.QuerySelectorAll(".tabber");
	var tabs = tablist.Skip(1).FirstOrDefault(tablist.FirstOrDefault());
	if (tabs is not null)
	{
		var panels = tabs.QuerySelectorAll(".tabber__panel");
		if (panels.Length > 0)
		{
			foreach (var panel in panels)
			{
				var outfit = await ProcessOutfit(directory, panel);
				if (outfit is not null)
					talent.outfits.Add(outfit);
			}
		}
	}
}

static async Task<string?> ProcessOutfit(string directory, IElement panel)
{
	var outfit = panel.GetAttribute("title")?.Trim();
	var img = panel.QuerySelector("img");
	var src = img?.GetAttribute("src");
	if (src is not null && outfit is not null)
	{
		Console.WriteLine($"Downloading outfit {outfit}");
		src = GetFullQuality(src);
		try
		{
			await DownloadImageAsync(directory + $"{outfit}.png", src);
		}
		catch (Exception)
		{
			try
			{
				Console.WriteLine($"Downloading outfit {outfit} (retry)");
				await DownloadImageAsync(directory + $"{outfit}.png", src);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Outfit download failed for {outfit}: {ex.Message}");
			}
		}
		return outfit;
	}

	return null;

	static string GetFullQuality(string src)
	{
		src = src.Replace("/thumb/", "/");
		var last = src.LastIndexOf("/");
		return src[0..last];
	}

	static async Task DownloadImageAsync(string path, string src)
	{
		using var client = new HttpClient();
		var data = await client.GetByteArrayAsync(src);
		await File.WriteAllBytesAsync(path, data);
	}
}