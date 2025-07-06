using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Any2GSX.AppConfig
{
    public enum MatchData
    {
        Airline = 1,
        Title = 2,
        AtcId = 3,
        SimObject = 4,
    }

    public enum MatchOperation
    {
        Equals = 1,
        StartsWith = 2,
        Contains = 3,
    }

    //Legacy
    public enum ProfileMatchType
    {
        Default = 0,
        Airline = 1,
        Title = 2,
        AtcId = 3,
        AircraftString = 4,
    }

    public class ProfileMatching
    {
        public MatchData MatchData { get; set; } = MatchData.SimObject;
        [JsonIgnore]
        public static Dictionary<MatchData, int> MatchScores => new()
        {
            {MatchData.Airline, 1 },
            {MatchData.Title, 2 },
            {MatchData.AtcId, 4 },
            {MatchData.SimObject, 8 },
        };
        [JsonIgnore]
        public virtual string MatchDataText => MatchDataTexts[MatchData];
        [JsonIgnore]
        public static Dictionary<MatchData, string> MatchDataTexts { get; } = new()
        {
            {MatchData.Airline, "Airline" },
            {MatchData.Title, "Title/Livery" },
            {MatchData.AtcId, "ATC ID" },
            {MatchData.SimObject, "SimObject" },
        };
        public MatchOperation MatchOperation { get; set; } = MatchOperation.Contains;
        [JsonIgnore]
        public virtual string MatchOperationText => MatchOperationTexts[MatchOperation];
        [JsonIgnore]
        public static Dictionary<MatchOperation, string> MatchOperationTexts { get; } = new()
        {
            {MatchOperation.Equals, "equals" },
            {MatchOperation.StartsWith, "starts with" },
            {MatchOperation.Contains, "contains" },
        };
        public string MatchString { get; set; } = "";


        public ProfileMatching()
        {

        }

        public ProfileMatching(MatchData data, MatchOperation? operation = null, string matchString = null)
        {
            MatchData = data;

            if (operation != null)
                MatchOperation = (MatchOperation)operation;
            else
            {
                MatchOperation = MatchData switch
                {
                    MatchData.Airline => MatchOperation.StartsWith,
                    MatchData.Title => MatchOperation.Contains,
                    MatchData.AtcId => MatchOperation.Equals,
                    MatchData.SimObject => MatchOperation.Contains,
                    _ => MatchOperation.Contains,
                };
            }

            MatchString = matchString ?? "";
        }

        public override string ToString()
        {
            return $"{MatchDataText} {MatchOperationText} '{MatchString}' [+{MatchScores[MatchData]}]";
        }

        public int Match(AppService appService)
        {
            int score = 0;
            string[] strings = MatchString.Split('|');
            if (MatchData == MatchData.Airline)
            {
                foreach (var s in strings)
                    if (Match(appService.GetAirline(), MatchOperation, s))
                        score = MatchScores[MatchData];
            }
            else if (MatchData == MatchData.Title)
            {
                foreach (var s in strings)
                    if (Match(appService.GetTitle(), MatchOperation, s))
                        score = MatchScores[MatchData];
            }
            else if (MatchData == MatchData.AtcId)
            {
                foreach (var s in strings)
                    if (Match(appService.GetAtcId(), MatchOperation, s))
                        score = MatchScores[MatchData];
            }
            else if (MatchData == MatchData.SimObject)
            {
                foreach (var s in strings)
                    if (Match(appService.GetAircraftString(), MatchOperation, s))
                        score = MatchScores[MatchData];
            }

            return score;
        }

        public static bool Match(string source, MatchOperation operation, string matchString)
        {
            if (string.IsNullOrWhiteSpace(matchString))
                return false;
            else if (operation == MatchOperation.Equals)
                return source?.Equals(matchString, StringComparison.InvariantCultureIgnoreCase) == true;
            else if (operation == MatchOperation.StartsWith)
                return source?.StartsWith(matchString, StringComparison.InvariantCultureIgnoreCase) == true;
            else if (operation == MatchOperation.Contains)
                return source?.Contains(matchString, StringComparison.InvariantCultureIgnoreCase) == true;
            else
                return false;
        }
    }
}
