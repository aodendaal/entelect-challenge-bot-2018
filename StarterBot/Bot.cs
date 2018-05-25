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

            return DefendTower();
        }

        private string BuildEnergy()
        {
            if (player.Energy < energyPrefab.Price)
            {
                return string.Empty;
            }

            var freeCells = gameState.GameMap.Select(row => row.First()).Where(cell => cell.Buildings.Count == 0).ToList();

            if (freeCells.Count > 0)
            {
                var space = freeCells[random.Next(freeCells.Count)];

                return $"{space.X},{space.Y},{(int)BuildingType.Energy}";
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

            var freeCells = gameState.GameMap.Select(row => row[1]).Where(cell => cell.Buildings.Count == 0).ToList();

            if (freeCells.Count > 0)
            {
                var space = freeCells[random.Next(freeCells.Count)];

                return $"{space.X},{space.Y},{(int)BuildingType.Attack}";
            }
            else
            {
                return string.Empty;
            }
        }

        private string DefendTower()
        {
            if (player.Energy < wallPrefab.Price)
            {
                return string.Empty;
            }

            var freeRows = gameState.GameMap.Where(row => row[1].Buildings.Count == 1 && row[3].Buildings.Count == 0).ToList();

            if (freeRows.Count == 0)
            {
                if (gameState.GameMap.Where(row => row[1].Buildings.Count == 1).Count() < mapHeight)
                {
                    return BuildTower();
                }
                else
                {
                    return BuildDoubleTower();
                }
            }

            var space = freeRows[random.Next(freeRows.Count)][3];

            return $"{space.X},{space.Y},{(int)BuildingType.Defense}";
        }

        private string BuildDoubleTower()
        {
            if (player.Energy < towerPrefab.Price)
            {
                return string.Empty;
            }

            var freeRows = gameState.GameMap.Where(row => row[1].Buildings.Count == 1 && row[2].Buildings.Count == 0).ToList();

            if (freeRows.Count == 0)
            {
                return BuildEnergy();
            }

            var space = freeRows[random.Next(freeRows.Count)][2];

            return $"{space.X},{space.Y},{(int)BuildingType.Attack}";
        }


        private IEnumerable<CellStateContainer> GetBuildings(PlayerType player, BuildingType type)
        {
            return gameState.GameMap.SelectMany(row => row)
                                    .Where(cell => cell.CellOwner == player &&
                                                   cell.Buildings.Any(building => building.BuildingType == type));
        }
    }
}