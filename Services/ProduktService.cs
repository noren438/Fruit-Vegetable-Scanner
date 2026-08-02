using Frugt_Grønt_Scanner.Models;
using System.Linq;

namespace Frugt_Grønt_Scanner.Services
{
    public class ProduktService
    {
        private readonly Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["æble"] = "apple",
            ["aeble"] = "apple",
            ["banan"] = "banana",
            ["kirsebær"] = "cherry",
            ["kirsebaer"] = "cherry",
            ["vindrue"] = "grapes",
            ["vindruer"] = "grapes",
            ["grønne druer"] = "green grapes",
            ["gronne druer"] = "green grapes",
            ["appelsin"] = "orange",
            ["jordbær"] = "strawberry",
            ["jordbaer"] = "strawberry",
            ["ananas"] = "pineapple",
            ["vandmelon"] = "watermelon",
            ["peberfrugt"] = "bell pepper",
            ["aubergine"] = "eggplant",
            ["agurk"] = "cucumber",
            ["hvidløg"] = "garlic",
            ["hvidlog"] = "garlic",
            ["ingefær"] = "ginger",
            ["ingefaer"] = "ginger",
            ["løg"] = "onion",
            ["log"] = "onion",
            ["kartoffel"] = "potato",
            ["radise"] = "radish",
            ["tomat"] = "tomato",
            ["græskar"] = "pumpkin",
            ["graeskar"] = "pumpkin"
        };

        private readonly ProduktInfo[] produktListe =
        {
                // Frugter
                new("apple", "1001", "Frugt"),
                new("banana", "1002", "Frugt"),
                new("cherry", "1003", "Frugt"),
                new("dragon fruit", "1004", "Frugt"),
                new("grapes", "1005", "Frugt"),
                new("green grapes", "1006", "Frugt"),
                new("lemon", "1007", "Frugt"),
                new("lychee", "1008", "Frugt"),
                new("mango", "1009", "Frugt"),
                new("orange", "1010", "Frugt"),
                new("pear", "1011", "Frugt"),
                new("pineapple", "1012", "Frugt"),
                new("pomelo", "1013", "Frugt"),
                new("strawberry", "1014", "Frugt"),
                new("sugar apple", "1015", "Frugt"),
                new("watermelon", "1016", "Frugt"),
                
                // Grøntsager
                new("bell pepper", "2001", "Grøntsag"),
                new("bitter melon", "2002", "Grøntsag"),
                new("broccoli", "2003", "Grøntsag"),
                new("cabbage", "2004", "Grøntsag"),
                new("calabash", "2005", "Grøntsag"),
                new("carrot", "2006", "Grøntsag"),
                new("cauliflower", "2007", "Grøntsag"),
                new("cucumber", "2008", "Grøntsag"),
                new("eggplant", "2009", "Grøntsag"),
                new("garlic", "2010", "Grøntsag"),
                new("ginger", "2011", "Grøntsag"),
                new("green chili", "2012", "Grøntsag"),
                new("lady finger", "2013", "Grøntsag"),
                new("onion", "2014", "Grøntsag"),
                new("peanut", "2015", "Grøntsag"),
                new("potato", "2016", "Grøntsag"),
                new("pumpkin", "2017", "Grøntsag"),
                new("radish", "2018", "Grøntsag"),
                new("red chili", "2019", "Grøntsag"),
                new("tomato", "2020", "Grøntsag"),
        };
        public ProduktInfo Resolve(string navn)
        {
            var normalized = NormalizeName(navn);

            if (aliases.TryGetValue(normalized, out var canonical))
            {
                normalized = canonical;
            }

            return produktListe.FirstOrDefault(x => string.Equals(x.Navn, normalized,
                StringComparison.OrdinalIgnoreCase)) ?? new ProduktInfo(navn, "0000", "Ukendt");
        }

        private static string NormalizeName(string navn)
        {
            return (navn ?? string.Empty)
                .Trim()
                .Trim('"', '\'', '{', '}', '[', ']')
                .ToLowerInvariant();
        }
    }

}
