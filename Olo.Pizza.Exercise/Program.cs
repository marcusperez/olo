using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MoreLinq;
using Newtonsoft.Json;

namespace Olo.Pizza.Exercise
{
    public class Order
    {
        public string[] toppings { get; set; }
        public int hashedToppings { get; set; }
    }

    public class ComboSummaryItem
    {
        public int Key { get; set; }
        public string[] Toppings { get; set; }
        public int Count { get; set; }
    }

    internal class Program
    {
        private static IEnumerable<ComboSummaryItem> GetUniqueComboSummary(IReadOnlyList<Order> orders)
        {
            // get distinct list of hashes
            var uniqueOrders = orders.DistinctBy(order => order.hashedToppings).ToList();

            var uniqueComboSummary = new List<ComboSummaryItem>();

            for (var index = 0; index < uniqueOrders.Count; index++)
            {
                var order = orders[index];

                var uniqueComboHash = order.hashedToppings;

                var uniqueComboExists = uniqueOrders.Any(u => u.hashedToppings == uniqueComboHash);

                if (uniqueComboExists)
                {
                    // compute the count the 
                    var existingComboSummary = uniqueComboSummary.SingleOrDefault(u => u.Key == uniqueComboHash);
                    var summaryExists = uniqueComboSummary.Contains(existingComboSummary);

                    if (summaryExists)
                    {
                        var currentSummary = uniqueComboSummary.Single(u => u.Key == uniqueComboHash);

                        // remove the original and replace with the new
                        uniqueComboSummary.Remove(currentSummary);
                        uniqueComboSummary.Add(new ComboSummaryItem
                        {
                            Toppings = currentSummary.Toppings,
                            Count = currentSummary.Count + 1,
                            Key = currentSummary.Key
                        });
                    }
                    else
                    {
                        // first time
                        uniqueComboSummary.Add(new ComboSummaryItem
                        {
                            Key = uniqueComboHash,
                            Toppings = order.toppings,
                            Count = 1
                        });
                    }
                }
            }

            return uniqueComboSummary;
        }

        private static int GetOrderAgnosticHashCode<T>(IEnumerable<T> source)
        {
            var hash = 0;
            var valueCounts = new Dictionary<T, int>();

            foreach (var element in source)
            {
                var currentHash = EqualityComparer<T>.Default.GetHashCode(element);
                if (valueCounts.TryGetValue(element, out var bitOffset))
                    valueCounts[element] = bitOffset + 1;
                else
                    valueCounts.Add(element, bitOffset);

                hash = unchecked(hash + ((currentHash << bitOffset) |
                                         (currentHash >> (32 - bitOffset))) * 37);
            }

            return hash;
        }

        private static List<Order> CreateHashedOrders(IEnumerable<Order> orders) =>
            orders.Select(order =>
            new Order
            {
                toppings = order.toppings,
                hashedToppings = GetOrderAgnosticHashCode(order.toppings)
            }).ToList();

        private static int GetLongestLength(IReadOnlyList<ComboSummaryItem> summarySorted)
        {
            return summarySorted.Select(topComboItem => 
                topComboItem.Toppings
                .OrderBy(t => t)
                .ToList())
                .Select(sortToppings => string.Join(" - ", sortToppings))
                .Select((topComboItemDisplay, index) => $"{index + 1}.{topComboItemDisplay} = ")
                .Select(segment1 => segment1.Length).Concat(new[] { 0 })
                .Max();
        }

        private static int GetLongestCount(IEnumerable<ComboSummaryItem> summarySorted)
        {
            return summarySorted.Select(topComboItem => 
                    topComboItem.Count.ToString())
                    .Select(segment1 => segment1.Length)
                    .Concat(new[] { 0 })
                    .Max();
        }

        private static void PrintResults(IReadOnlyList<ComboSummaryItem> summarySorted)
        {
            Console.WriteLine("*****************************************");
            Console.WriteLine("Top 20 pizza toppings");
            Console.WriteLine("*****************************************");

            var longestDisplayLength = GetLongestLength(summarySorted);
            var longestIndex = summarySorted.Count.ToString().Length;
            var longestNumber = GetLongestCount(summarySorted);

            for (var index = 0; index < summarySorted.Count; index++)
            {
                var topComboItem = summarySorted[index];
                var sortedToppings = topComboItem.Toppings.OrderBy(t => t).ToList();
                var topComboItemDisplay = string.Join(" - ", sortedToppings);

                var segment1 = $"{index + 1}".PadLeft(longestIndex);
                var segment2 = $"{segment1}. {topComboItemDisplay}".PadRight(longestDisplayLength);
                var segment3 = $"{topComboItem.Count}".PadLeft(longestNumber);

                var output = $"{segment2} = {segment3}";

                Console.WriteLine(output);
            }

            Console.WriteLine("*****************************************");
        }

        private static void Main()
        {
            var url = "http://files.olo.com/pizzas.json";

            // get the json data
            var json = new WebClient().DownloadString(url);

            var orders = JsonConvert.DeserializeObject<List<Order>>(json);
            var hashedOrders = CreateHashedOrders(orders);

            var summary = GetUniqueComboSummary(hashedOrders);
            var summarySorted = summary.OrderByDescending(s => s.Count).Take(20).ToList();

            PrintResults(summarySorted);

            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }
    }
}