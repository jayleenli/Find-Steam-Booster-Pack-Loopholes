using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace FindSteamBoosterPackLoopholes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public class Booster 
    {
        public int appID;
        public String name;
        public int gemPrice;

        public Booster(int a, String n, int g)
        {
            appID = a;
            name = n;
            gemPrice = g;
        }
        public override string ToString()
        {
            return "[appid=" + appID + ", name=" + name + ", gemPrice=" + gemPrice + "]";
        }

        public int getAppID ()
        {
            return appID;
        }
        
        public String getName()
        {
            return name;
        }

        public int getGemPrice()
        {
            return gemPrice;
        }
    }
    public partial class MainWindow : Window
    {
        List<Booster> boosterInfoList = new List<Booster>(); //Array to store all booster creator information
        double sackVal = 0.0; // store sack of gem info so it doesnt have to keep being called, help reduce the chance of 429 error
        int loadEnd = 0;
        int loadProgress = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void showInfo(Object sender, EventArgs e)
        {
            InformationBox info = new InformationBox();
            info.Show();
        }

        public void placeholder_remove(object sender, RoutedEventArgs e)
        {
            webpageHTML.Text = "";
        }

        public void findLoopholes(Object sender, EventArgs e)
        {
            //Signal Start Process
            startSearch.Content = "Loading...May take a while";

            //Cuts out any unnesscary HTML page information
            var contents = "";

            contents = webpageHTML.Text;

            var boosterInfoStart = contents.IndexOf("CBoosterCreatorPage.Init") + 24;
            var boosterInfoEnd = contents.IndexOf("parseFloat");
            if (boosterInfoStart < 24 && boosterInfoEnd < 0)
            {
                startSearch.Content = "Error. Incorrect format.";
            }
            else
            {
                var cutContents = contents.Substring(boosterInfoStart, (boosterInfoEnd - boosterInfoStart));
                //results.Text = cutContents;

                putintoArray(cutContents);
                /*new Thread(() =>
                //{
                    loadEnd = boosterInfoList.Count;
                    progressCounter.Text = "0/" + loadEnd;
                //}).Start();*/

                /* Setting a bound at 60 cents since sack of gems prices never go below this price.
                * Why? - because to try to prevent the "sack of gem is 4 cent" error.
                * I am making a check to see if it is less than 60 cents. Because if the price is less than 60 cents, then it's most likely due to error.
                */
                sackVal = getSackOfGemsCurValue();
                if (sackVal < .60) //note seems to be broken.
                {
                    //Don't run, display error to try again in a moment
                    startSearch.Content = "Steam Market Error, try again";
                }
                else
                {
                    String resulttest = "Cuurent sack of gems value = " + sackVal + "\nGames with boosters that can get you profit:\n";
                    int countNumOfGames = 0;
                    for (int i = 0; i < boosterInfoList.Count; i++)
                    {

                        double profit = howMuchProfitable(boosterInfoList[i]);
                        if (profit > 0.0) // if this game set has profit
                        {
                            resulttest += "Game ID: " + boosterInfoList[i].getAppID() + ", Game name: " + boosterInfoList[i].getName() + ", Cost of Booster:" + boosterInfoList[i].getGemPrice() + ", Profit Per sack of gems(1000): " + profit + "\n";
                            countNumOfGames++;
                        }
                        else // not profitable, print nothing
                        { }
                        //System.Threading.Thread.Sleep(3000); // wait one second between webclient requests, to avoid 429 error. Not sure what the max time is to make it most efficient

                        /*Update progress bar
                        loadProgress++;
                        progressCounter.Text = loadProgress + " / " + loadEnd;*/

                    }
                    if (countNumOfGames == 0) //no profitable games
                    {
                        resulttest += "...Sorry! None of your games will get you booster pack profit :/";
                    }

                    results.Text = resulttest;

                    //Signal End process
                    startSearch.Content = "Process Complete";
                }
            }
        }

        //Places all the booster creator information into an array so that program can read it
        public void putintoArray(String x)
        {
            
            int loc = x.IndexOf("appid");
            while (loc != -1)
            {
                //Get appid
                //Look for the end of appid attribute. Start with loc + 7
                int endcut = x.IndexOf("}", loc);
                String cut = x.Substring(loc + 7, endcut - (loc + 8)); //cut out from appid to end of } but does not include bracket.
                int endlocApp = (cut).IndexOf(",");
                int appid = Int32.Parse(cut.Substring(0, endlocApp));

                //Get the name of game
                int loc_Name = cut.IndexOf("name");
                int end_Name = cut.IndexOf(",", endlocApp + 1);
                String name = cut.Substring(loc_Name + 7, end_Name - (loc_Name + 8));

                //Get the gem price of booster for game
                int loc_Price = cut.IndexOf("price");
                int price = 0;
                //if this badge is one you have crafted within the last 24 hours (special case
                if (cut.Contains("unavailable"))
                {
                    int end_Price = cut.IndexOf(",", loc_Price);
                    price = Int32.Parse(cut.Substring((loc_Price + 8), end_Price - (loc_Price + 9)));
                }
                else //Normally get price
                {
                    price = Int32.Parse(cut.Substring(loc_Price + 8));
                }
                

                boosterInfoList.Add(new Booster(appid, name, price));
                
                loc = x.IndexOf("appid",loc + 10);
            }

            /*print array contents in the results, here for debugging
            string printboosterlist = "";
            for (int i = 0; i < boosterInfoList.Count; i++)
            {
                printboosterlist += boosterInfoList[i].ToString() + " , ";
            }

            results.Text = printboosterlist;*/
            
        }
        
        public double getAverageCardValue(Booster b)
        {
            /*check steam market for values
             * issues with this method
             * buy order values are sometimes inaccurate to what is actually sold at. 
             * sell order values change constantly. 
             */
            //http://steamcommunity.com/market/search?q=&category_753_Game%5B%5D=tag_app_271670&category_753_cardborder%5B%5D=tag_cardborder_0&category_753_item_class%5B%5D=tag_item_class_2&appid=753
            List<double> cardInfoList = new List<double>(); //Array to store card for badge info
            var steamMarketHTML = "";
            String steamMarketLink = "http://steamcommunity.com/market/search?q=&category_753_Game%5B%5D=tag_app_" + b.getAppID() + "&category_753_cardborder%5B%5D=tag_cardborder_0&category_753_item_class%5B%5D=tag_item_class_2&appid=753";
            using (var client = new System.Net.WebClient())
            {
                steamMarketHTML = client.DownloadString(steamMarketLink).Trim();
                System.Threading.Thread.Sleep(5000); // wait 2 second?
            }
            int loc = steamMarketHTML.IndexOf("class=\"normal_price\"");
            int end_loc = 0;
            while (loc != -1)
            {
                end_loc = steamMarketHTML.IndexOf("</span>", loc);
                double cardVal = Convert.ToDouble(steamMarketHTML.Substring(loc + 23, end_loc - (loc + 23)));
                cardInfoList.Add(cardVal);
                loc = steamMarketHTML.IndexOf("class=\"normal_price\"", loc+10);
            }

            // Get the average value of the cards
            double sum = 0.0;
            for (int i = 0; i < cardInfoList.Count; i++)
            {
                sum += cardInfoList[i];
            }
            
            return sum/cardInfoList.Count;
        }

        public double getSackOfGemsCurValue()
        {
            var steamGemHTML = "";
            String steamGemLink = "http://steamcommunity.com/market/search?q=sack+of+gems";
            /*
             * side note since I can't use the direct link for the sack of gem price, must rely on the preview price,
             * which sometimes is majorly screwed up (sometimes at like 4 cents when it really should be 80)
             * -may add something to help counter? but for now, the only way is to use the preview page. 
             */
            using (var client = new System.Net.WebClient())
            {
                steamGemHTML = client.DownloadString(steamGemLink).Trim();
            }
            System.Threading.Thread.Sleep(5000);

            int loc = steamGemHTML.IndexOf("class=\"normal_price\"");
            int end_loc = steamGemHTML.IndexOf("</span>", loc);
            double gemVal = Convert.ToDouble(steamGemHTML.Substring(loc + 23, end_loc - (loc + 23)));

            return gemVal;
        }

        public double howMuchProfitable(Booster b)
        {
            double avgCardValT3 = getAverageCardValue(b)*3; //times three because booster packs give you three cards
            double sackOfGemsVal = sackVal;
            
                // Now time for the algorithm!! (not super complicated but still there
                /* Steam fee calculating was derived from steam.tools main.js */
                var fee_cost = avgCardValT3 * 0.1304;
                fee_cost = Math.Max(fee_cost, 0.02);
                double cardSellingVal = avgCardValT3 - fee_cost;

                double numOfBoostersCreat = 1000.0 / b.getGemPrice(); //finds number of boosters than can be created per 1000 gems (sack)
                double profit = (numOfBoostersCreat * cardSellingVal) - sackOfGemsVal;
                return profit;
            
            /*var resulttest = "";
            for (int i = 0; i < boosterInfoList.Count; i++)
            {
                resulttest += b.getGemPrice() + " | " + avgCardValT3 + " | " + numOfBoostersCreat + " | " + cardSellingVal + " | " + sackOfGemsVal + " | " + profit;
            }
            results.Text = resulttest;*/

            
        }

            
        
    }
}
