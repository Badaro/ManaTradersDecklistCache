using MTGODecklistParser.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Updater
{
    public static class TournamentLoader
    {
        public static Tournament[] GetTournaments(string rawDataFolder)
        {
            List<Tournament> tournaments = new List<Tournament>();
            foreach (string tournamentFile in Directory.GetFiles(rawDataFolder, "*.json"))
            {
                dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(tournamentFile));
                DateTime date = json.Date;
                string csv = json.Csv;
                tournaments.Add(new ManaTradersTournament()
                {
                    Name = json.Name,
                    Date = date.ToUniversalTime(),
                    Uri = json.Url,
                    CsvFile = new FileInfo(Path.Combine(rawDataFolder, csv)).FullName
                });
            }
            return tournaments.ToArray();
        }
    }
}
