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
        private readonly int frontLine;
        private readonly Player player;
        private readonly Player enemy;
        private readonly Random random;

        private readonly List<Command> previousCommands;

        public Bot(GameState gameState, List<Command> history)
        {
            this.gameState = gameState;
            mapHeight = gameState.GameDetails.MapHeight;
            mapWidth = gameState.GameDetails.MapWidth;
            frontLine = mapWidth / 2 - 1;

            attackPrefab = gameState.GameDetails.BuildingsStats[BuildingType.Attack];
            defensePrefab = gameState.GameDetails.BuildingsStats[BuildingType.Defense];
            energyPrefab = gameState.GameDetails.BuildingsStats[BuildingType.Energy];

            previousCommands = history;

            random = new Random((int)DateTime.Now.Ticks);

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

            float attackEnergyAndTowers = gameState.GameMap.Count(row => row.Count(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(building => building.BuildingType == BuildingType.Energy)) > 0 &&
                                                             row.Count(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(building => building.BuildingType == BuildingType.Defense)) == 0);
            attackEnergyAndTowers *= (gameState.GameDetails.Round < gameState.GameDetails.MaxRounds / 4) ? 0.1f : 1f;


            float attackOnlyEnergy = gameState.GameMap.Count(row => row.Count(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(building => building.BuildingType == BuildingType.Energy)) > 0 &&
                                                              row.Count(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(building => building.BuildingType == BuildingType.Attack)) == 0 &&
                                                              row.Count(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(building => building.BuildingType == BuildingType.Defense)) == 0);
            attackOnlyEnergy *= (gameState.GameDetails.Round < gameState.GameDetails.MaxRounds / 4) ? 0.2f : 2f;


            float goDefensive = gameState.GameMap.Count(row => row.Count(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(building => building.BuildingType == BuildingType.Attack)) >= 2 &&
                                                           row.Count(cell => cell.CellOwner == PlayerType.A && cell.Buildings.Any(building => building.BuildingType == BuildingType.Defense)) == 0);

            goDefensive *= (gameState.GameDetails.Round < gameState.GameDetails.MaxRounds / 4) ? 1f : 0.5f;

            if (goDefensive >= attackEnergyAndTowers &&
                goDefensive >= attackOnlyEnergy && 
                player.Energy >= defensePrefab.Price)
            {
                return DefendAgainstAttack();
            }

            if (player.Energy >= attackPrefab.Price)
            {
                return AttackLeastDefendedRow();
            }

            return new Command();
        }

        private Command DefendAgainstAttack()
        {
            var selectedRow = gameState.GameMap.Where(row => row.Any(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(building => building.BuildingType == BuildingType.Attack)))
                                               .OrderBy(row => row.Count(cell => cell.CellOwner == PlayerType.A && cell.Buildings.Any(building => building.BuildingType == BuildingType.Defense)))
                                               .ThenByDescending(row => row.Count(cell => cell.CellOwner == PlayerType.A && cell.Buildings.Any(building => building.BuildingType == BuildingType.Energy)))
                                               .FirstOrDefault();

            if (selectedRow != null)
            {
                return BuildDefense(selectedRow);
            }

            selectedRow = gameState.GameMap.OrderBy(row => row.Count(cell => cell.CellOwner == PlayerType.A && cell.Buildings.Any(building => building.BuildingType == BuildingType.Defense)))
                                           .ThenByDescending(row => row.Count(cell => cell.CellOwner == PlayerType.A && cell.Buildings.Any(building => building.BuildingType == BuildingType.Energy)))
                                           .First();

            return BuildDefense(selectedRow);
        }

        private static Command BuildDefense(CellStateContainer[] selectedRow)
        {
            var freeCell = selectedRow.Where(cell => cell.CellOwner == PlayerType.A && cell.Buildings.Count == 0).OrderByDescending(cell => cell.X).FirstOrDefault();

            if (freeCell != null)
            {
                return new Command { X = freeCell.X, Y = freeCell.Y, Type = BuildingType.Defense };
            }
            else
            {
                return new Command();
            }
        }

        private Command BuildEnergy()
        {
            var leastDangerous = gameState.GameMap.Where(row => row.First().Buildings.Count == 0)
                                                  .GroupBy(row => row.Count(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(building => building.BuildingType == BuildingType.Attack)))
                                                  .OrderBy(group => group.Key)
                                                  .First();

            var selectedRow = leastDangerous.ToList()[random.Next(leastDangerous.Count())];

            return new Command { X = 0, Y = selectedRow[0].Y, Type = BuildingType.Energy };
        }

        public Command AttackLeastDefendedRow()
        {
            var selectedRow = gameState.GameMap.Where(row => row.Count(cell => cell.CellOwner == PlayerType.A && cell.Buildings.Count > 0) < mapWidth / 2)
                                               //.OrderBy(row => row.Count(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(building => building.BuildingType == BuildingType.Attack)))
                                               .OrderByDescending(row => row.Count(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(building => building.BuildingType == BuildingType.Energy)))
                                               .ThenBy(row => row.Where(cell => cell.CellOwner == PlayerType.B && cell.Buildings.Any(buildling => buildling.BuildingType == BuildingType.Defense))
                                                                           .Sum(cell => cell.Buildings.Sum(building => building.Health * ((building.ConstructionTimeLeft > 0) ? 0 : 1)))
                                               )
                                               .ThenByDescending(row => row.Count(cell => cell.CellOwner == PlayerType.A && cell.Buildings.Any(building => building.BuildingType == BuildingType.Energy)))
                                               .FirstOrDefault();

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