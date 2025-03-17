
### **Dice Game 🎲 – A Fair and Transparent Dice Game**  

This is a **C# console-based dice game** that ensures fair play using **cryptographic random number generation with HMAC verification**. Players roll dice against a computer, and a **probability table** shows the likelihood of winning based on different dice configurations.  

#### **Key Features:**  
✅ **Fairness with HMAC Verification** – Every roll is generated securely and verifiable.  
✅ **Custom Dice Sets** – Players can define their own dice with unique number distributions.  
✅ **Probability Calculation** – Displays a probability table for strategic insights.  
✅ **Cryptographic Randomization** – Uses `RandomNumberGenerator` for unbiased rolls.  
✅ **Interactive Gameplay** – Choose dice, roll against the computer, and check results dynamically.  

#### **Technologies Used:**  
- **C# (.NET)**
- **ConsoleTables** (for structured probability tables)
- **HMAC-SHA256** (for result verification)
- **RandomNumberGenerator** (for cryptographic randomness)  

🚀 **Run the game with:**  
```sh
dotnet run -- 2,2,4,4,9,9 6,8,1,1,8,6 7,5,3,7,5,3
```
```
dotnet run -- 1,2,3,4,5,6 1,2,3,4,5,6 1,2,3,4,5,6 1,2,3,4,5,6
```

```
dotnet run -- 2,2,4,4,9,9 1,1,6,6,8,8 3,3,5,5,7,7

```

👨‍💻 **Contributions & Feedback Welcome!**  
🔗 **Check out the source code and start playing!** 🎲
