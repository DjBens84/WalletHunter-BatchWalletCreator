using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
class Program
{
    // Data structure to store Ethereum addresses and balances
    static Dictionary<string, double> ethAddressBalanceMap;

    // Counter to track total statistics
    static int totalKeyCount = 0;

    // Counter to track local statistics
    static int localKeyCount = 0;

    // StreamWriter için bir kilit nesnesi
    private static readonly object writerLock = new object();

    // Interval to clean up memory
    static int cleanupInterval = Convert.ToInt32(ConfigurationManager.AppSettings["CleanupInterval"]);

    // Max degree of parallelism
    static int maxDegreeOfParallelism = Convert.ToInt32(ConfigurationManager.AppSettings["MaxDegreeOfParallelism"]);

    static void Main()
    {
        // Load eth_list.tsv file
        LoadEthList(Environment.CurrentDirectory + "\\eth_list.tsv");

        // Check if the data structure is loaded successfully
        if (ethAddressBalanceMap != null && ethAddressBalanceMap.Any())
        {
            Console.WriteLine("Data loaded successfully.");
            Console.WriteLine($"Number of addresses in the loaded data: {ethAddressBalanceMap.Count}");
        }
        else
        {
            Console.WriteLine("Error loading data or no addresses found.");
            Console.ReadKey();
        }



        // Ethereum connection
        //var web3 = new Web3();

        // Start time
        DateTime startTime = DateTime.Now;

        // Parallel options with max degree of parallelism
        ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        };

        // StreamWriter for writing to a file
        using (StreamWriter writer = new StreamWriter("export.txt"))
        {
            // Generate random private key and address in parallel with a limited number of threads
            Parallel.ForEach(Enumerable.Range(0, int.MaxValue), parallelOptions, _ =>
            {
                // Generate random private key and address in an infinite loop
                while (true)
                {
                    // Generate a random private key
                    var privateKey = GenerateRandomPrivateKey();

                    // Create an Ethereum account (address) from the private key
                    var account = new Account(privateKey);



                    // Get the balance of the address
                    //var balance = GetBalance(web3, account.Address);

                    // Increment local counter
                    localKeyCount++;

                    // Increment total counter atomically
                    System.Threading.Interlocked.Increment(ref totalKeyCount);

                    // Check the newly generated address
                    bool status = CheckEthAddress(account.Address);

                    // Check if the balance is not zero
                    if (status == true)
                    {
                        // Write the results to the file
                        lock (writerLock)
                        {
                            // Write the results to the file
                            writer.WriteLine($"Private Key: {privateKey}");
                            writer.WriteLine($"Address: {account.Address}");
                            //writer.WriteLine($"Balance: {balance} wei");
                            writer.WriteLine();
                        }

                        // Print the results to the console if desired
                        Console.WriteLine($"Private Key: {privateKey}");
                        Console.WriteLine($"Address: {account.Address}");
                        //Console.WriteLine($"Balance: {balance} wei");
                        Console.WriteLine();

                        // Wait for a key press before generating the next key
                        Console.WriteLine("Press any key to generate the next key...");
                        Console.ReadKey(true);
                    }

                    // Print statistics when a certain number of keys have been generated
                    if (localKeyCount % 10000 == 0)
                    {
                        // End time
                        DateTime endTime = DateTime.Now;

                        // Calculate elapsed time in seconds
                        double elapsedSeconds = (endTime - startTime).TotalSeconds;

                        // Calculate total keys per second
                        double totalKeysPerSecond = totalKeyCount / elapsedSeconds;

                        // Print total statistics to the console
                        Console.WriteLine($"Total Key Count: {totalKeyCount}");
                        Console.WriteLine($"Total Keys Per Second: {totalKeysPerSecond:F2} keys/second");
                        Console.WriteLine();
                        // Check if elapsed time is greater than or equal to 60 seconds
                        //if (elapsedSeconds >= 60)
                        //{
                        // End the program
                        //return;
                        //}
                    }

                    // Check if it's time to clean up memory
                    if (localKeyCount % cleanupInterval == 0)
                    {
                        // Clean up memory
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        Console.WriteLine("Memory cleaned up.");
                    }
                }
            });
        }
    }

    // Function to load Ethereum addresses and balances from the file into a dictionary
    static void LoadEthList(string filePath)
    {
        ethAddressBalanceMap = new Dictionary<string, double>();

        try
        {
            // Dosyanın var olup olmadığını kontrol et
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }

            // Read the file line by line
            foreach (var line in File.ReadLines(filePath).Skip(1)) // Skip the header line
            {
                // Split the line into parts
                string[] parts = line.Split('\t');

                // Extract the address and balance
                string address = parts[0];
                double balance = double.Parse(parts[1]);

                // Add to the dictionary
                ethAddressBalanceMap.Add(address, balance);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"File loading error: {ex.Message}");
        }
    }


    // Function to generate a random Ethereum address (replace with your own implementation)
    static string GenerateRandomEthAddress()
    {
        return "0x" + Guid.NewGuid().ToString("N").Substring(0, 40);
    }

    // Function to check if an Ethereum address is in the loaded dictionary
    static bool CheckEthAddress(string address)
    {
        if (ethAddressBalanceMap.ContainsKey(address))
        {
            double balance = ethAddressBalanceMap[address];
            //Console.WriteLine($"Address found: {address}, Balance: {balance}");
            return true;
        }
        else
        {
            //Console.WriteLine($"Address not found: {address}");
            return false;
        }
    }

    // Function to generate a random private key
    static string GenerateRandomPrivateKey()
    {
        var randomBytes = new byte[32];
        using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
        {
            rng.GetBytes(randomBytes);
        }
        return "0x" + BitConverter.ToString(randomBytes).Replace("-", "");
    }

    // Function to get the balance of an Ethereum address
    static ulong GetBalance(Web3 web3, string address)
    {
        // Call the web3.Eth.GetBalance.SendRequestAsync method to get the balance
        var balanceTask = web3.Eth.GetBalance.SendRequestAsync(address);

        // Wait for the task to complete and return the result
        return balanceTask.Result.ToUlong();
    }
}
