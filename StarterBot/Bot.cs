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

            this.random = new Random((int)DateTime.Now.Ticks);

            player = gameState.Players.Single(x => x.PlayerType == PlayerType.A);
        }

        public string Run()
        {
            var earnRate = gameState.GameDetails.RoundIncomeEnergy + GetBuildings(PlayerType.A, BuildingType.Energy).Count() * energyPrefab.EnergyGeneratedPerTurn;

            if (earnRate < 14)
            {
                return BuildEnergy();
            }

            if (!GetBuildings(PlayerType.A, BuildingType.Attack).Any())
            {
                return BuildTower();

            }

            return BuildTower();

            return string.Empty;
        }

        private string BuildEnergy()
        {
            if (player.Energy < energyPrefab.Price)
            {
                return string.Empty;
            }

            var freeCells = gameState.GameMap.Select(c => c.First()).Where(c => c.Buildings.Count == 0).ToList();

            if (freeCells.Count > 0)
            {
                var space = freeCells[random.Next(freeCells.Count)];

                return $"{space.X},{space.Y},2";
            }
            else
            {
                return string.Empty;
            }
        }

        private string BuildTower()
        {
            if (player.Energy < towerPrefab.Price)
            {
                return string.Empty;
            }

            var freeCells = gameState.GameMap.Select(c => c[1]).Where(c => c.Buildings.Count == 0).ToList();

            if (freeCells.Count > 0)
            {
                var space = freeCells[random.Next(freeCells.Count)];

                return $"{space.X},{space.Y},1";
            }
            else
            {
                return string.Empty;
            }
        }

        private IEnumerable<CellStateContainer> GetBuildings(PlayerType player, BuildingType type)
        {
            return gameState.GameMap.SelectMany(c => c).Where(c => c.CellOwner == player && c.Buildings.Any(b => b.BuildingType == type));
        }
    }
}