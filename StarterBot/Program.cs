using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using StarterBot.Entities;

namespace StarterBot
{
    public class Program
    {
        private static string commandFileName = "command.txt";
        private static string stateFileName = "state.json";
        private static string historyFilename = "history.json";

        static void Main(string[] args)
        {
            var gameState = JsonConvert.DeserializeObject<GameState>(File.ReadAllText(stateFileName));
            var history = new List<Command>();
            if (File.Exists(historyFilename))
            {
                history = JsonConvert.DeserializeObject<List<Command>>(File.ReadAllText(historyFilename));
            }

            var command = new Bot(gameState, history).Run();
            command.Round = gameState.GameDetails.Round;

            history.Add(command);
            var output = JsonConvert.SerializeObject(history);
            File.WriteAllText(historyFilename, output);

            File.WriteAllText(commandFileName, command.ToString());
        }
    }
}