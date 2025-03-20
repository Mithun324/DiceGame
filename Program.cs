using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ConsoleTables;

class Dice
{
    public List<int> Faces { get; }

    public Dice(List<int> faces)
    {
        if (faces.Count != 6)
            throw new ArgumentException("Each die must have exactly six faces.");
        Faces = faces;
    }
}

class FairRandom
{
    private byte[] key;
    private HMACSHA256 hmac;
    public int GeneratedNumber { get; private set; }
    public string Hmac { get; private set; }

    public FairRandom()
    {
        key = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }
        hmac = new HMACSHA256(key);
    }

    public void Generate(int maxExclusive)
    {
        GeneratedNumber = new Random().Next(maxExclusive);
        Hmac = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(GeneratedNumber.ToString()))).Replace("-", "");
    }

    public string RevealKey()
    {
        return BitConverter.ToString(key).Replace("-", "");
    }
}

class DiceGame
{
    private static readonly Random random = new();
    private static List<Dice> diceSet;

    public static void Start(List<Dice> dice)
    {
        if (dice.Count < 3)
        {
            Console.WriteLine("Error: You must provide at least three dice sets.");
            return;
        }

        diceSet = dice;
        Console.WriteLine("🎲 Welcome to the Dice Game!");

        while (true)
        {
            PlayRound();

            Console.WriteLine("\n🔄 Options: Type 'table' for probability table, 'exit' to quit, or press Enter to play again.");
            string nextAction = Console.ReadLine()?.ToLower();

            if (nextAction == "table")
                DisplayProbabilityTable();
            else if (nextAction == "exit" || nextAction == "x")
                break;
        }
    }


    private static void PlayRound()
    {
        Console.WriteLine("Let's determine who makes the first move.");

        // Generate first random number (0 or 1)
        FairRandom fairRandom1 = new();
        fairRandom1.Generate(2);
        Console.WriteLine($"I generated a secret value in range 0..1 (HMAC={fairRandom1.Hmac})");

        // User inputs their first guess (0 or 1)
        Console.WriteLine("Try to guess my number (0 or 1):");
        string userGuess = Console.ReadLine()?.Trim();

        if (!int.TryParse(userGuess, out int userGuessInt) || (userGuessInt != 0 && userGuessInt != 1))
        {
            Console.WriteLine("Invalid input! Exiting.");
            return;
        }

        Console.WriteLine($"Your guess: {userGuessInt}");
        Console.WriteLine($"My number: {fairRandom1.GeneratedNumber} (Key={fairRandom1.RevealKey()})");

        // Determine who picks dice first
        bool userGuessedCorrectly = (userGuessInt == fairRandom1.GeneratedNumber);
        int computerIndex = random.Next(diceSet.Count);
        int userIndex;

        if (userGuessedCorrectly)
        {
            Console.WriteLine("You guessed correctly! You choose the dice first.");
        }
        else
        {
            Console.WriteLine("You guessed wrong. I choose the dice first.");
            Console.WriteLine($"I choose: [{string.Join(",", diceSet[computerIndex].Faces)}]");
        }

        // User selects their dice
        Console.WriteLine("Choose your dice:");
        for (int i = 0; i < diceSet.Count; i++)
            Console.WriteLine($"{i} - {string.Join(",", diceSet[i].Faces)}");

        Console.WriteLine("X - exit");
        string userChoice = Console.ReadLine()?.Trim();
        if (userChoice == "x") return;

        if (!int.TryParse(userChoice, out userIndex) || userIndex < 0 || userIndex >= diceSet.Count)
        {
            Console.WriteLine("Invalid selection. Exiting round.");
            return;
        }
        Console.WriteLine($"You chose: [{string.Join(",", diceSet[userIndex].Faces)}]");

        if (!userGuessedCorrectly)
        {
            computerIndex = random.Next(diceSet.Count);
        }

        // Roll 1: Computer Roll
        FairRandom fairRandom2 = new();
        fairRandom2.Generate(6);
        Console.WriteLine($"I generated a secret value in range 0..5 (HMAC={fairRandom2.Hmac})");

        Console.WriteLine("Now, add your number (0-5) modulo 6:");
        string userModInput = Console.ReadLine()?.Trim();

        if (!int.TryParse(userModInput, out int userMod) || userMod < 0 || userMod > 5)
        {
            Console.WriteLine("Invalid selection. Exiting round.");
            return;
        }

        int firstResult = (fairRandom2.GeneratedNumber + userMod) % 6;
        int computerValue = diceSet[computerIndex].Faces[firstResult];

        Console.WriteLine($"My roll was: {fairRandom2.GeneratedNumber}");
        Console.WriteLine($"Resulting dice value: {computerValue} (Key={fairRandom2.RevealKey()})");

        // Roll 2: User Roll
        FairRandom fairRandom3 = new();
        fairRandom3.Generate(6);
        Console.WriteLine($"I generated a secret value in range 0..5 (HMAC={fairRandom3.Hmac})");

        Console.WriteLine("Now, add your number (0-5) modulo 6 for your roll:");
        string userRollInput = Console.ReadLine()?.Trim();

        if (!int.TryParse(userRollInput, out int userRoll) || userRoll < 0 || userRoll > 5)
        {
            Console.WriteLine("Invalid selection. Exiting round.");
            return;
        }

        int secondResult = (fairRandom3.GeneratedNumber + userRoll) % 6;
        int userValue = diceSet[userIndex].Faces[secondResult];

        Console.WriteLine($"Your roll was: {fairRandom3.GeneratedNumber}");
        Console.WriteLine($"Your resulting dice value: {userValue} (Key={fairRandom3.RevealKey()})");



        // Final Result
        if (userValue > computerValue)
            Console.WriteLine($"🎉 You win ({userValue} > {computerValue})!");
        else if (userValue < computerValue)
            Console.WriteLine($"😢 You lose ({userValue} < {computerValue}).");
        else
            Console.WriteLine($"🤝 It's a tie ({userValue} = {computerValue}).");

        // HMAC Verification Links
        Console.WriteLine("\n Verify fairness of the rolls using an HMAC calculator:");
        Console.WriteLine($"- HMAC: https://www.liavaag.org/English/SHA-Generator/?key={fairRandom1.RevealKey()}&message={fairRandom1.GeneratedNumber}");
    }


    public static void DisplayProbabilityTable()
    {
        var table = new ConsoleTable("Your Dice", "Computer Dice", "Win %", "Loss %", "Tie %");

        for (int i = 0; i < diceSet.Count; i++)
        {
            for (int j = 0; j < diceSet.Count; j++)
            {
                double wins = 0, losses = 0, ties = 0;

                for (int a = 0; a < 6; a++)
                {
                    for (int b = 0; b < 6; b++)
                    {
                        int userValue = diceSet[i].Faces[a];
                        int computerValue = diceSet[j].Faces[b];

                        if (userValue > computerValue) wins++;
                        else if (userValue < computerValue) losses++;
                        else ties++;
                    }
                }

                double total = 36.0; // 6x6 possibilities

                // ANSI Escape Codes for Colors
                string winColor = "\u001b[32m";  // Green for Wins
                string lossColor = "\u001b[31m"; // Red for Losses
                string tieColor = "\u001b[33m";  // Yellow for Ties
                string resetColor = "\u001b[0m"; // Reset to default color

                table.AddRow(
                    $"[{string.Join(",", diceSet[i].Faces)}]",
                    $"[{string.Join(",", diceSet[j].Faces)}]",
                    $"{winColor}{(wins / total):P}{resetColor}",
                    $"{lossColor}{(losses / total):P}{resetColor}",
                    $"{tieColor}{(ties / total):P}{resetColor}"
                );
            }
        }

        Console.WriteLine("\n📊 PROBABILITY TABLE 📊");
        table.Write(Format.Alternative);
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

            List<Dice> dice = args.Select(arg => new Dice(arg.Split(',').Select(int.Parse).ToList())).ToList();
        }
        DiceGame.Start(diceSet);
    }
}
