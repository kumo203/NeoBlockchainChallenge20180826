using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

// This program uses NEO Scan, neoscan.io, Web API service
// for debug address AGRC6p8BS6AdD7YcgR6LCpTZVbhj6f8Qsd was used.
// Example 1: https://neoscan.io/address/AGRC6p8BS6AdD7YcgR6LCpTZVbhj6f8Qsd/1
// Example 2: https://neoscan.io/api/main_net/v1/get_address_abstracts/AGRC6p8BS6AdD7YcgR6LCpTZVbhj6f8Qsd/1

namespace Net4._7BalanceChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Net4.7BalanceChecker.exe <Address> <Block Height#>");
                return;
            }

            string address = args[0];
            string url = $"https://neoscan.io/api/main_net/v1/get_address_abstracts/{address}/";
            if ( uint.TryParse(args[1], out uint targetBlockHeight) != true)
            {
                Console.WriteLine("<Block Height#> Parsing Error.");
                return;
            }

            using (var client = new WebClient())
            {
                int page = 1;
                List<NeoScanTransaction> AddressTransactions = new List<NeoScanTransaction>();
                for ( ; ; )
                {
                    string urlPlusPage = url + page;
                    string source = client.DownloadString(urlPlusPage);


                    NeoScanGetAddressAbstract obj = JsonConvert.DeserializeObject<NeoScanGetAddressAbstract>(source);
                    if (obj != null && obj.Entry.Length != 0)
                    {
                        AddressTransactions.AddRange(obj.Entry);
                        page++;
                    } else
                    {
                        break;
                    }
                }
                AddressTransactions.Sort( (x, y) => x.BlockHeight - y.BlockHeight);

                // For NEO: c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b
                // For GAS: 602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7

                Dictionary<string, decimal> assetBalance = new Dictionary<string, decimal>();
                foreach (var t in AddressTransactions)
                {
                    //Console.WriteLine(t.BlockHeight);
                    if ( t.BlockHeight > targetBlockHeight )
                    {
                        // Stop at the target Block Height
                        break;
                    }
                    if (t.AddressFrom == t.AddressTo)
                    {
                        // do nothing
                    } else if (t.AddressTo == address)
                    {
                        AddAsset(assetBalance, t);
                        // Add Amount
                        assetBalance[t.Asset] += t.Amount;
                    }
                    else if (t.AddressFrom == address)
                    {
                        AddAsset(assetBalance, t);
                        // Cut Amount
                        assetBalance[t.Asset] -= t.Amount;
                    } else
                    {
                        // Unexpected case
                        //Debug.Assert(false, "Unexpected asset transfer case");
                    }
                }

                Console.WriteLine($"Address:{address} 's asset status at Block Height: {targetBlockHeight}");
                Console.WriteLine();
                foreach (var pair in assetBalance)
                {
                    Console.WriteLine($"{pair.Key} :: {pair.Value}");
                }
                Console.WriteLine("Please type a key to exit...");
                Console.ReadKey();
            }
        }

        private static void AddAsset(Dictionary<string, decimal> assetBalance, NeoScanTransaction t)
        {
            if (assetBalance.ContainsKey(t.Asset) == false)
            {
                assetBalance.Add(t.Asset, 0);
            }
        }
    }

    [JsonObject]
    class NeoScanGetAddressAbstract
    {
        [JsonProperty("total_pages")]
        public int TotalPage;
        [JsonProperty("total_entries")]
        public int TotalEntry;
        [JsonProperty("page_size")]
        public int PageSize;
        [JsonProperty("page_number")]
        public int PageNo;
        [JsonProperty("entries")]
        public NeoScanTransaction[] Entry;
    }

    [JsonObject]
    class NeoScanTransaction
    {
        [JsonProperty("txid")]
        public string TxId;
        [JsonProperty("time")]
        public int Time;        
        [JsonProperty("block_height")]
        public int BlockHeight;
        [JsonProperty("asset")]
        public string Asset;
        [JsonProperty("amount")]
        public decimal Amount;
        [JsonProperty("address_to")]
        public string AddressTo;
        [JsonProperty("address_from")]
        public string AddressFrom;
    }
}
