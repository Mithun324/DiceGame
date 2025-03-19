using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ConsoleTables; // Install with: dotnet add package ConsoleTables

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
        FairRandom fairRandom = new();
        fairRandom.Generate(2);
        Console.WriteLine($"I selected a random value in the range 0..1 (HMAC={fairRandom.Hmac}).");

        Console.WriteLine("Try to guess my selection.");
        Console.WriteLine("0 - 0\n1 - 1\nX - exit");
        string userGuess = Console.ReadLine()?.Trim().ToLower();
        if (userGuess == "x") return;

        Console.WriteLine($"Your selection: {userGuess}");
        Console.WriteLine($"My selection: {fairRandom.GeneratedNumber} (KEY={fairRandom.RevealKey()}).");

        int computerIndex = random.Next(diceSet.Count);
        Console.WriteLine($"I make the first move and choose the [{string.Join(",", diceSet[computerIndex].Faces)}] dice.");

        Console.WriteLine("Choose your dice:");
        for (int i = 0; i < diceSet.Count; i++)
            Console.WriteLine($"{i} - {string.Join(",", diceSet[i].Faces)}");
        Console.WriteLine("X - exit");

        string userChoice = Console.ReadLine()?.Trim().ToLower();
        if (userChoice == "x") return;

        if (!int.TryParse(userChoice, out int userIndex) || userIndex < 0 || userIndex >= diceSet.Count)
        {
            Console.WriteLine("Invalid choice. Exiting round.");
            return;
        }

        Console.WriteLine($"You choose the [{string.Join(",", diceSet[userIndex].Faces)}] dice.");

        Console.WriteLine("It's time for my roll.");
        fairRandom.Generate(6);
        Console.WriteLine($"I selected a random value in the range 0..5 (HMAC={fairRandom.Hmac}).");

        Console.WriteLine("Add your number modulo 6.");
        for (int i = 0; i < 6; i++)
            Console.WriteLine($"{i} - {i}");
        Console.WriteLine("X - exit");

        string userModInput = Console.ReadLine()?.Trim().ToLower();
        if (userModInput == "x") return;

        if (!int.TryParse(userModInput, out int userMod) || userMod < 0 || userMod > 5)
        {
            Console.WriteLine("Invalid selection. Exiting round.");
            return;
        }
        int firstResult = (fairRandom.GeneratedNumber + userMod) % 6;
        int computerValue = diceSet[computerIndex].Faces[firstResult];
        Console.WriteLine($"The fair number generation result is {fairRandom.GeneratedNumber} + {userMod} = {firstResult} (mod 6).");
        Console.WriteLine($"Computer rolled: {computerValue}");

        Console.WriteLine("It's time for your roll.");
        fairRandom.Generate(6);
        Console.WriteLine($"I selected a random value in the range 0..5 (HMAC={fairRandom.Hmac}).");

        Console.WriteLine("Add your number modulo 6.");
        for (int i = 0; i < 6; i++)
            Console.WriteLine($"{i} - {i}");
        Console.WriteLine("X - exit");

        userModInput = Console.ReadLine()?.Trim().ToLower();
        if (userModInput == "x") return;

        if (!int.TryParse(userModInput, out userMod) || userMod < 0 || userMod > 5) // Reuse `userMod`
        {
            Console.WriteLine("Invalid selection. Exiting round.");
            return;
        }
        int secondResult = (fairRandom.GeneratedNumber + userMod) % 6;
        int userValue = diceSet[userIndex].Faces[secondResult];
        Console.WriteLine($"The fair number generation result is {fairRandom.GeneratedNumber} + {userMod} = {secondResult} (mod 6).");
        Console.WriteLine($"User rolled: {userValue}");

        // 🎉 Final Win/Loss/Tie Check with Comparison
        if (userValue > computerValue)
            Console.WriteLine($"🎉 You win ({userValue} > {computerValue})!");
        else if (userValue < computerValue)
            Console.WriteLine($"😢 You lose ({userValue} < {computerValue}).");
        else
            Console.WriteLine($"🤝 It's a tie ({userValue} = {computerValue}).");
    }

    private static void DisplayProbabilityTable()
    {
        Console.WriteLine("\n📊 Probability of the win for the user:\n");

        var table = new ConsoleTable(new string[] { "User Dice v" }
            .Concat(diceSet.Select(d => string.Join(",", d.Faces))).ToArray());

        for (int i = 0; i < diceSet.Count; i++)
        {
            List<string> row = new() { string.Join(",", diceSet[i].Faces) };
            for (int j = 0; j < diceSet.Count; j++)
            {
                if (i == j)
                    row.Add("- (0.3333)"); // Tie probability when using the same dice
                else
                {
                    double probability = CalculateWinProbability(diceSet[i], diceSet[j]);
                    row.Add($"{probability:F4}");
                }
            }
            table.AddRow(row.ToArray());
        }

        table.Write(Format.Alternative);
    }

    private static double CalculateWinProbability(Dice userDice, Dice computerDice)
    {
        int userWins = 0;
        int totalRounds = 0;

        for (int userRoll = 0; userRoll < 6; userRoll++)
        {
            for (int computerRoll = 0; computerRoll < 6; computerRoll++)
            {
                int userValue = userDice.Faces[userRoll];
                int computerValue = computerDice.Faces[computerRoll];

                if (userValue > computerValue) userWins++;
                totalRounds++;
            }
        }

        return totalRounds == 0 ? 0 : (double)userWins / totalRounds;
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

            DiceGame.Start(diceSet);
        }
    }
}