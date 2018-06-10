using StarterBot.Entities;
using StarterBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarterBot
{

    public class Priority
    {
        public BuildingType Type { get; set; }
        public float Value { get; set; }
    }

    public class Bot
    {
        private readonly GameState gameState;

        private readonly BuildingStats attackPrefab;
        private readonly BuildingStats defensePrefab;
        private readonly BuildingStats energyPrefab;

        private readonly int mapWidth;
        private readonly int mapHeight;
        private readonly Player player;
        private readonly Player enemy;
        private readonly Random random;

        private readonly List<Command> previousCommands;

        public Bot(GameState gameState, List<Command> history)
        {
            this.gameState = gameState;
            this.mapHeight = gameState.GameDetails.MapHeight;
            this.mapWidth = gameState.GameDetails.MapWidth;

            this.attackPrefab = gameState.GameDetails.BuildingsStats[BuildingType.Attack];
            this.defensePrefab = gameState.GameDetails.BuildingsStats[BuildingType.Defense];
            this.energyPrefab = gameState.GameDetails.BuildingsStats[BuildingType.Energy];

            this.previousCommands = history;

            this.random = new Random((int)DateTime.Now.Ticks);

            player = gameState.Players.Single(x => x.PlayerType == PlayerType.A);
            enemy = gameState.Players.Single(x => x.PlayerType == PlayerType.B);
        }

        public Command Run()
        {
            var earnRate = gameState.GameDetails.RoundIncomeEnergy + GetBuildings(PlayerType.A, BuildingType.Energy).Count() * energyPrefab.EnergyGeneratedPerTurn;

            if (earnRate < 17)
            {
                if (player.Energy >= energyPrefab.Price)
                {
                    return BuildEnergy();
                }
            }

            if (player.Energy >= attackPrefab.Price)
            {
                var suggested = AttackLeastDefendedRow();

                if (suggested.Type == BuildingType.Attack)
                {
                    var lastAction = previousCommands.Where(command => command.Type != null).OrderByDescending(command => command.Round).FirstOrDefault();

                    if (lastAction != null && suggested.X == lastAction.X && suggested.Y == lastAction.Y && lastAction.Type == BuildingType.Attack)
                    {
                        suggested.Type = BuildingType.Defense;
                    }

                    return suggested;
                }
            }

            return new Command();
        }

        private Command BuildEnergy()
        {
            var leastDangerous = gameState.GameMap.Where(row => row.First().Buildings.Count == 0)
                                                  .GroupBy(row => row.Count(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(building => building.BuildingType == BuildingType.Attack)))
                                                  .First();

            var selectedRow = leastDangerous.ToList()[random.Next(leastDangerous.Count())];

            return new Command { X = 0, Y = selectedRow[0].Y, Type = BuildingType.Energy };
        }
        
        public Command AttackLeastDefendedRow()
        {
            var selectedRow = gameState.GameMap.Where(row => row.Count(cell => cell.CellOwner == PlayerType.A && cell.Buildings.Count > 0) < mapWidth / 2)
                                               .OrderByDescending(row => row.Count(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(building => building.BuildingType == BuildingType.Energy)))
                                               .ThenByDescending(row => row.Where(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(buildling => buildling.BuildingType == BuildingType.Defense))
                                                                           .Sum(cell => cell.Buildings.Sum(building => building.Health * ((building.ConstructionTimeLeft > 0) ? 0 : 1)))
                                                       )
                                               .First();

            return BuildAttack(selectedRow);
        }

        private Command BuildAttack(CellStateContainer[] selectedRow)
        {
            var freeCell = selectedRow.Where(cell => cell.CellOwner == PlayerType.A && cell.Buildings.Count == 0).OrderByDescending(cell => cell.X).FirstOrDefault();

            if (freeCell != null)
            {
                return new Command { X = freeCell.X, Y = freeCell.Y, Type = BuildingType.Attack };
            }
            else
            {
                return new Command();
            }
        }

        private IEnumerable<CellStateContainer> GetBuildings(PlayerType player, BuildingType type)
        {
            return gameState.GameMap.SelectMany(row => row)
                                    .Where(cell => cell.CellOwner == player &&
                                                   cell.Buildings.Any(building => building.BuildingType == type));
        }
    }
}