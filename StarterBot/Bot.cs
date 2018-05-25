using System;
using System.Collections.Generic;
using System.Linq;
using StarterBot.Entities;
using StarterBot.Enums;

namespace StarterBot
{
    public class Bot
    {
        private readonly GameState gameState;

        private readonly BuildingStats towerPrefab;
        private readonly BuildingStats wallPrefab;
        private readonly BuildingStats energyPrefab;

        private readonly int mapWidth;
        private readonly int mapHeight;
        private readonly Player player;
        private readonly Random random;

        public Bot(GameState gameState)
        {
            this.gameState = gameState;
            this.mapHeight = gameState.GameDetails.MapHeight;
            this.mapWidth = gameState.GameDetails.MapWidth;

            this.towerPrefab = gameState.GameDetails.BuildingsStats[BuildingType.Attack];
            this.wallPrefab = gameState.GameDetails.BuildingsStats[BuildingType.Defense];
            this.energyPrefab = gameState.GameDetails.BuildingsStats[BuildingType.Energy];

            this.random = new Random((int) DateTime.Now.Ticks);

            player = gameState.Players.Single(x => x.PlayerType == PlayerType.A);
        }

        public string Run()
        {            
            var earnRate = gameState.GameDetails.RoundIncomeEnergy;
            

            return string.Empty;
        }

        private string BuildTower()
        {
            throw new NotImplementedException();
        }

        private void GetBuildings(PlayerType player, BuildingType type)
        {
            gameState.GameMap.Select(cell => cell.pl
        }
    }
}