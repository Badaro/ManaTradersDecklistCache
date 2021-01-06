using CsvHelper;
using HtmlAgilityPack;
using MTGODecklistParser.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Updater
{
    public static class TournamentDetailsLoader
    {
        public static TournamentDetails GetTournamentDetails(string csvUrl, string standingsUrl, string bracketUrl)
        {
            var standings = ParseStandings(standingsUrl);
            var decks = ParseDecks(csvUrl, standings);
            var bracket = ParseBracket(bracketUrl);

            return new TournamentDetails()
            {
                Decks = decks,
                Standings = standings,
                Bracket = bracket
            };
        }

        private static Deck[] ParseDecks(string csvUrl, Standing[] standings)
        {
            List<Deck> result = new List<Deck>();

            string csvData = new WebClient().DownloadString(csvUrl);

            ManaTradersCsvRecord[] records;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvData)))
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        csv.Configuration.HasHeaderRecord = true;
                        records = csv.GetRecords<ManaTradersCsvRecord>().ToArray();
                    }
                }
            }

            foreach (var player in records.Select(r => r.Player).Distinct())
            {
                ManaTradersCsvRecord[] playerCards = records.Where(r => r.Player.Trim() == player.Trim()).ToArray();

                string playerName = player;
                string playerResult = null;
                if (standings != null)
                {
                    var playerStanding = standings.FirstOrDefault(s => s.Player.ToLower().Trim() == player.ToLower().Trim());

                    if (playerStanding != null)
                    {
                        var rankSuffix = "th";
                        if (playerStanding.Rank == 1) rankSuffix = "st";
                        if (playerStanding.Rank == 2) rankSuffix = "nd";
                        if (playerStanding.Rank == 3) rankSuffix = "rd";

                        playerResult = playerStanding.Rank.ToString() + rankSuffix;
                        playerName = playerStanding.Player.Trim(); // Name from standings has the correct casing, name from CSV is forced lowercase
                    }
                    else
                    {
                        playerResult = "-";
                        playerName = player.Trim();
                    }
                }

                result.Add(new Deck()
                {
                    AnchorUri = null,
                    Date = null,
                    Player = playerName,
                    Result = playerResult,
                    Mainboard = playerCards.Where(c => !c.Sideboard).Select(c => new DeckItem() { Count = c.Count, CardName = FixCardName(c.Card) }).ToArray(),
                    Sideboard = playerCards.Where(c => c.Sideboard).Select(c => new DeckItem() { Count = c.Count, CardName = FixCardName(c.Card) }).ToArray(),
                });
            }

            return result.OrderBy(r => r.Result != "-" ? Int32.Parse(r.Result.Substring(0, r.Result.Length - 2)) : Int32.MaxValue).ToArray();
        }

        private static string FixCardName(string cardName)
        {
            // Normalizes card format with MTGO website
            if (cardName.Contains("Full Art")) return cardName.Replace("Full Art", "").Trim();
            if (cardName.Contains("/")) return cardName.Replace("/", " // ");
            return cardName;
        }

        private static Standing[] ParseStandings(string standingsUrl)
        {
            string pageContent = new WebClient().DownloadString(standingsUrl);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageContent);

            var standingsRoot = doc.DocumentNode.SelectSingleNode("//table[@class='table table-tournament-rankings']");
            if (standingsRoot == null) return null;

            List<Standing> result = new List<Standing>();

            var standingNodes = standingsRoot.SelectNodes("tbody/tr");
            foreach (var standingNode in standingNodes)
            {
                var rows = standingNode.SelectNodes("td");

                int rank = int.Parse(rows[0].InnerText);
                string player = rows[1].InnerText.Trim();
                int points = int.Parse(rows[2].InnerText);
                double omwp = double.Parse(rows[5].InnerText.Trim('%'), CultureInfo.InvariantCulture) / 100d;
                double gwp = double.Parse(rows[6].InnerText.Trim('%'), CultureInfo.InvariantCulture) / 100d;
                double ogwp = double.Parse(rows[7].InnerText.Trim('%'), CultureInfo.InvariantCulture) / 100d;

                result.Add(new Standing()
                {
                    Rank = rank,
                    Player = player,
                    Points = points,
                    OMWP = omwp,
                    GWP = gwp,
                    OGWP = ogwp
                });
            }

            return result.ToArray();
        }

        private static Bracket ParseBracket(string bracketUrl)
        {
            string pageContent = new WebClient().DownloadString(bracketUrl);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageContent);

            var bracketRoot = doc.DocumentNode.SelectSingleNode("//div[@class='tournament-brackets']");
            if (bracketRoot == null) return null;

            List<BracketItem> brackets = new List<BracketItem>();

            var bracketNodes = bracketRoot.SelectNodes("ul/li");
            foreach (var bracketNode in bracketNodes)
            {
                List<string> players = new List<string>();
                List<int> wins = new List<int>();

                foreach (var playerNode in bracketNode.SelectNodes("div"))
                {
                    players.Add(playerNode.SelectNodes("div").First().InnerText);
                    wins.Add(Convert.ToInt32(playerNode.SelectNodes("div").Last().InnerText));
                }

                if (wins[0] > wins[1])
                {
                    brackets.Add(new BracketItem()
                    {
                        Player1 = players[0],
                        Player2 = players[1],
                        Result = wins[0] + "-" + wins[1]
                    });
                }
                else
                {
                    brackets.Add(new BracketItem()
                    {
                        Player1 = players[1],
                        Player2 = players[0],
                        Result = wins[1] + "-" + wins[0]
                    });
                }
            }

            return new Bracket()
            {
                Quarterfinals = brackets.Take(4).ToArray(),
                Semifinals = brackets.Skip(4).Take(2).ToArray(),
                Finals = brackets.Skip(6).First()
            };
        }
    }
}
