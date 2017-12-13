using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.NotificationHubs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;

namespace CoinTimer
{    
    public static class GetPricesFromCoinbase
    {        
        [FunctionName("GetPricesFromCoinbase")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, 
                                     [NotificationHub]IAsyncCollector<Notification> notification,
                                     TraceWriter log)
        {            
            CancellationTokenSource cts = new CancellationTokenSource(10_000);

            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            
            using (var httpClient = new HttpClient())
            {
                try
                {
                    httpClient.DefaultRequestHeaders.Add("CB-VERSION", "2017-08-07");

                    HttpResponseMessage result = await httpClient.GetAsync($"https://api.coinbase.com/v2/prices/BTC-USC/spot", cts.Token);
                    HttpResponseMessage ltcResult = await httpClient.GetAsync($"https://api.coinbase.com/v2/prices/LTC-USD/spot", cts.Token);

                    if (result.IsSuccessStatusCode && ltcResult.IsSuccessStatusCode)
                    {
                        string btcString = await result.Content.ReadAsStringAsync();
                        CoinbaseResponse btcResponse = JsonConvert.DeserializeObject<CoinbaseResponse>(btcString);

                        string ltcString = await ltcResult.Content.ReadAsStringAsync();
                        CoinbaseResponse ltcResponse = JsonConvert.DeserializeObject<CoinbaseResponse>(ltcString);

                        TileVisual tileVisual = new TileVisual()
                        {
                            TileSmall = new TileBinding
                            {
                                Content = new TileBindingContentAdaptive
                                {
                                    TextStacking = TileTextStacking.Center,
                                    Children = 
                                    {
                                        new AdaptiveText { Text = "BTC to USD" },
                                        new AdaptiveText { Text = $"${btcResponse.Data.Amount}", HintStyle = AdaptiveTextStyle.CaptionSubtle }
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
                                        new AdaptiveText { Text = "BTC to USD" },
                                        new AdaptiveText { Text = $"${btcResponse.Data.Amount}", HintStyle = AdaptiveTextStyle.CaptionSubtle }
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
                                        new AdaptiveText { Text = "BTC to USD" },
                                        new AdaptiveText { Text = $"${btcResponse.Data.Amount}", HintStyle = AdaptiveTextStyle.CaptionSubtle },
                                        new AdaptiveText { Text = "LTC to USD"},
                                        new AdaptiveText { Text = $"${ltcResponse.Data.Amount}", HintStyle = AdaptiveTextStyle.CaptionSubtle}
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
                                        new AdaptiveText { Text = "BTC to USD" },
                                        new AdaptiveText { Text = $"${btcResponse.Data.Currency}", HintStyle = AdaptiveTextStyle.CaptionSubtle },
                                        new AdaptiveText { Text = "LTC to USD"},
                                        new AdaptiveText { Text = $"${ltcResponse.Data.Amount}", HintStyle = AdaptiveTextStyle.CaptionSubtle}
                                    }
                                }
                            }
                        };

                        TileContent tileContent = new TileContent
                        {
                            Visual = tileVisual
                        };

                        await notification.AddAsync(new WindowsNotification(tileContent.GetContent()));
                    }
                    else
                    {
                        log.Error($"Failed to get a response from Coinbase. HTTP Status: {result.StatusCode}, Response body: {await result.Content.ReadAsStringAsync()}");
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
    }
}
