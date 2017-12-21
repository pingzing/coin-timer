using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoinTimer
{
    public static class GetPricesFromCoinbase
    {
        [FunctionName("GetPricesFromCoinbase")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer,
            [NotificationHub(ConnectionStringSetting = "AzureWebJobsNotificationHubsConnectionString",
                HubName = "cointesthub",
                Platform = NotificationPlatform.Wns)] IAsyncCollector<Notification> notification,
            TraceWriter log)
        {
            log.Info("Running!");
            CancellationTokenSource cts = new CancellationTokenSource(10_000);

            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            using (var httpClient = new HttpClient())
            {
                try
                {
                    httpClient.DefaultRequestHeaders.Add("CB-VERSION", "2017-08-07");

                    List<TileContent> tiles = new List<TileContent>();
                    tiles.Add(await GetCoinTileContent(httpClient, "BTC", cts.Token, log));
                    tiles.Add(await GetCoinTileContent(httpClient, "LTC", cts.Token, log));
                    tiles.Add(await GetCoinTileContent(httpClient, "BCH", cts.Token, log));

                    foreach (var successfulTile in tiles.Where(x => x != null))
                    {
                        await notification.AddAsync(new WindowsNotification(successfulTile.GetContent()));
                    }
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is HttpRequestException)
                {
                    log.Error($"Failed to get a response from Coinbase. Exception: {ex.Message}.");
                }
                catch (Exception ex)
                {
                    log.Error($"Failed to Coinbase info. Exception: {ex.Message}.");
                }
            }
        }

        private static async Task<TileContent> GetCoinTileContent(HttpClient httpClient, string coinType, CancellationToken token, TraceWriter log)
        {
            HttpResponseMessage result = await httpClient.GetAsync($"https://api.coinbase.com/v2/prices/{coinType}-USD/spot", token);

            if (result.IsSuccessStatusCode)
            {
                string coinString = await result.Content.ReadAsStringAsync();
                CoinbaseResponse coinResponse = JsonConvert.DeserializeObject<CoinbaseResponse>(coinString);

                TileVisual tileVisual = new TileVisual()
                {
                    TileSmall = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            TextStacking = TileTextStacking.Center,
                            Children =
                            {
                                new AdaptiveText { Text = $"{coinType} to USD" },
                                new AdaptiveText { Text = $"${coinResponse.Data.Amount}", HintStyle = AdaptiveTextStyle.CaptionSubtle }
                            }
                        }
                    },

                    TileMedium = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            TextStacking = TileTextStacking.Center,
                            Children =
                            {
                                new AdaptiveText { Text = $"{coinType} to USD" },
                                new AdaptiveText { Text = $"${coinResponse.Data.Amount}", HintStyle = AdaptiveTextStyle.CaptionSubtle }
                            }
                        }
                    },

                    TileLarge = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            TextStacking = TileTextStacking.Center,
                            Children =
                            {
                                new AdaptiveText { Text = $"{coinType} to USD" },
                                new AdaptiveText { Text = $"${coinResponse.Data.Amount}", HintStyle = AdaptiveTextStyle.CaptionSubtle },
                            }
                        }
                    },

                    TileWide = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            TextStacking = TileTextStacking.Center,
                            Children =
                            {
                                new AdaptiveText { Text = $"{coinType} to USD" },
                                new AdaptiveText { Text = $"${coinResponse.Data.Amount}", HintStyle = AdaptiveTextStyle.CaptionSubtle },
                            }
                        }
                    }
                };

                TileContent tileContent = new TileContent
                {
                    Visual = tileVisual
                };                

                return tileContent;
            }
            else
            {
                log.Error($"Failed to get a response from Coinbase. HTTP Status: {result.StatusCode}, Response body: {await result.Content.ReadAsStringAsync()}");
                return null;
            }
        }

    }
}
