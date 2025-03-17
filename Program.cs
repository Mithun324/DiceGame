using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using ConsoleTables; // using Third-party library for tables

class ProbabilityTable
{
    private List<Dice> diceSet;

    public ProbabilityTable(List<Dice> diceSet)
    {
        this.diceSet = diceSet;
    }

    public void Display()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n Probability of winning for the user:");
        Console.ResetColor();

        var table = new ConsoleTable(new[] { "User Dice v" }.Concat(diceSet.Select(d => string.Join(",", d.Faces))).ToArray());

        for (int i = 0; i < diceSet.Count; i++)
        {
            List<string> row = new() { string.Join(",", diceSet[i].Faces) };
            for (int j = 0; j < diceSet.Count; j++)
            {
                if (i == j)
                    row.Add("- (0.3333)"); // Diagonal case (tie probability)
                else
                    row.Add(CalculateWinProbability(diceSet[i], diceSet[j]).ToString("0.####"));
            }
            table.AddRow(row.ToArray());
        }

        Console.ForegroundColor = ConsoleColor.DarkRed;
        table.Write(Format.Alternative);
        Console.ResetColor();
    }

    private double CalculateWinProbability(Dice userDice, Dice opponentDice)
    {
        int wins = 0, total = 0;

        foreach (int u in userDice.Faces)
        {
            foreach (int o in opponentDice.Faces)
            {
                if (u > o) wins++;
                total++;
            }
        }
        return (double)wins / total;
    }
}

class Dice
{
    public List<int> Faces { get; }
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    public Dice(List<int> faces) => Faces = faces;

    public int Roll()
    {
        Span<byte> bytes = stackalloc byte[4];
        Rng.GetBytes(bytes);
        return Faces[Math.Abs(BitConverter.ToInt32(bytes)) % Faces.Count];
    }
}


class FairRandom
{
    private byte[] secretKey;
    public int GeneratedNumber { get; private set; }
    public string Hmac { get; private set; }

    public FairRandom()
    {
        secretKey = new byte[32];
        RandomNumberGenerator.Fill(secretKey);
    }

    public void Generate(int maxRange)
    {
        byte[] numberBytes = new byte[4];
        RandomNumberGenerator.Fill(numberBytes);
        GeneratedNumber = Math.Abs(BitConverter.ToInt32(numberBytes, 0) % maxRange);

        using var hmac = new HMACSHA256(secretKey);
        Hmac = BitConverter.ToString(hmac.ComputeHash(numberBytes)).Replace("-", "");
    }


    public string RevealKey()
    {
        return BitConverter.ToString(secretKey).Replace("-", "");
    }
}

class Game
{
    private List<Dice> diceSet;
    private ProbabilityTable probabilityTable;

    public Game(List<Dice> diceSet)
    {
        this.diceSet = diceSet;
        this.probabilityTable = new ProbabilityTable(diceSet);
    }

    public void Start()
    {
        Console.WriteLine(" Welcome to the Dice Game!");
        Console.WriteLine("You will roll against the computer using different dice.");
        Console.WriteLine("The probability table will be displayed after a few rounds.\n");

        PlayRounds();
    }

    private void PlayRounds()
    {
        Random random = new();
        while (true)
        {
            Console.WriteLine("\n Choose a dice index (0, 1, 2) or type 'exit' to quit:");
            for (int i = 0; i < diceSet.Count; i++)
                Console.WriteLine($"[{i}] {string.Join(",", diceSet[i].Faces)}");

            string input = Console.ReadLine();
            if (input?.ToLower() == "exit") break;

            if (!int.TryParse(input, out int userIndex) || userIndex < 0 || userIndex >= diceSet.Count)
            {
                Console.WriteLine("❌ Invalid choice. Try again.");
                continue;
            }

            Dice userDice = diceSet[userIndex];
            Dice opponentDice = diceSet[random.Next(diceSet.Count)];

            FairRandom fairRandom = new FairRandom();
            fairRandom.Generate(6);
            int userRoll = userDice.Roll();
            int opponentRoll = opponentDice.Roll();

            Console.WriteLine($"\n You rolled: {userRoll}");
            Console.WriteLine($" Computer rolled: {opponentRoll}");
            Console.WriteLine($" Verification: HMAC={fairRandom.Hmac}, KEY={fairRandom.RevealKey()}");

            if (userRoll > opponentRoll)
                Console.WriteLine(" You win this round!");
            else if (userRoll < opponentRoll)
                Console.WriteLine(" You lose this round.");
            else
                Console.WriteLine(" It's a tie!");

            Console.WriteLine("\n Play another round or type 'table' to see probabilities.");
            string nextAction = Console.ReadLine()?.ToLower();

            if (nextAction == "table")
                probabilityTable.Display();
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Error: You must provide at least three dice sets.");
            Console.WriteLine("Example: dotnet run -- 2,2,4,4,9,9 6,8,1,1,8,6 7,5,3,7,5,3");
            return;
        }

        List<Dice> diceSet = new();
        foreach (var arg in args)
        {
            try
            {
                List<int> faces = arg.Split(',').Select(int.Parse).ToList();
                if (faces.Count != 6) throw new FormatException();
                diceSet.Add(new Dice(faces));
            }
            catch
            {
                Console.WriteLine($"Error: Invalid dice configuration '{arg}'. Each die must have exactly six integer faces.");
                return;
            }
        }

        Console.WriteLine($"> dotnet run -- {string.Join(" ", args)}");

        Game game = new(diceSet);
        game.Start();
    }
}
