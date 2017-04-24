using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using System.Threading;
using System.Text.RegularExpressions;
namespace Bumper_3._0
{
    public static class Program
    {
        public static CookieAwareWebClient Client = new CookieAwareWebClient();
        public static string AccName { get; set; }
        public static string AccPass { get; set; }
        public static string TradePartner { get; set; }
        public static string TradeToken { get; set; }
        public static string ShopName = "my";
        public static int BuyPercent = 82;
        public static int Timer, Bumps = 0;
        public static XmlDocument Doc = new XmlDocument();
        public static List<TradeOption> TradeOptions = new List<TradeOption>();
        public static List<Offer> Offers = new List<Offer>();
        public static string Version = "3.5";

        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;     //Mono error

            #region entrance

            Console.WriteLine();
            string top = "CSGOLOUNGE BUMPER ver. " + Version;
            string bottom = "Coded by Erexo";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("@".Repeat(Console.WindowWidth));
            Console.Write("@@@" + " ".Repeat((Console.WindowWidth - top.Length - 6) / 2) + top + " ".Repeat((Console.WindowWidth - top.Length - 6) / 2) + "@@@");
            Console.Write("@@@" + " ".Repeat((Console.WindowWidth - bottom.Length - 6) / 2) + bottom + " ".Repeat((Console.WindowWidth - bottom.Length - 6) / 2) + "@@@");
            Console.WriteLine("@".Repeat(Console.WindowWidth));
            Console.ResetColor();

            #endregion

            #region load config

            if (args.Length > 1)
            {
                AccName = args[0];
                AccPass = args[1];
            }
            else if (File.Exists("config.xml"))
            {
                Log("Loading config", false);
                try
                {
                    Doc.Load("config.xml");
                    if (Doc.DocumentElement.SelectSingleNode("email") != null)
                        AccName = Doc.DocumentElement.SelectSingleNode("email").InnerText;
                    if (Doc.DocumentElement.SelectSingleNode("password") != null)
                        AccPass = Doc.DocumentElement.SelectSingleNode("password").InnerText;
                    if (Doc.DocumentElement.SelectSingleNode("shopName") != null)
                        ShopName = Doc.DocumentElement.SelectSingleNode("shopName").InnerText;
                    if (Doc.DocumentElement.SelectSingleNode("buyPercent") != null)
                        BuyPercent = int.Parse(Doc.DocumentElement.SelectSingleNode("buyPercent").InnerText);
                    if (Doc.DocumentElement.SelectSingleNode("tradePartner") != null)
                        TradePartner = Doc.DocumentElement.SelectSingleNode("tradePartner").InnerText;
                    if (Doc.DocumentElement.SelectSingleNode("tradeToken") != null)
                        TradeToken = Doc.DocumentElement.SelectSingleNode("tradeToken").InnerText;
                    loadTrades();
                    loadOffers();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("success");
                    Console.ResetColor();
                }
                catch(Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }

            }

            #endregion

            if (string.IsNullOrEmpty(AccName) || string.IsNullOrEmpty(AccPass))
            {
                Console.WriteLine("Usage: [" + Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location) + " email password]");
                return;
            }

            #region connection

            Log("Establishing connection", false);

            if (HasConnection())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("success");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("failure");
                Console.ResetColor();
                return;
            }

            #endregion

            Log("Logging in", false);
            if (!LogIn())
                return;

            #region thread start

            Log("Initializing worker thread", false);
            try
            {
                Thread timeThread = new Thread(timerThread);
                timeThread.IsBackground = true;
                timeThread.Start();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("failure");
                Console.ResetColor();
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("success");
            Console.ResetColor();

            #endregion

            Console.WriteLine("~".Repeat(Console.WindowWidth));

            #region endLoop

            string command = "";
            while (!command.inArray(new string[] { "quit", "exit", "q" }))
            {
                command = Console.ReadLine();

                switch (command)
                {
                    case "status":
                        Log(">Bumps: " + Bumps.ToString());
                        Log(">Current timer: " + Timer.ToString());
                        break;
                    case "quit":
                    case "q":
                    case "exit":
                        Log(">Exitting...");
                        break;
                    case "reload":
                        loadTrades();
                        Log(">Trades reloaded");
                        break;
                    default:
                        Log(">Unknown command");
                        break;
                }

            }

            #endregion
        }

        static void Log(string value, bool newLine = true)
        {
            if (newLine)
                Console.WriteLine("[" + DateTime.Now.ToString("H:mm:ss") + "] " + value);
            else
            {
                string writeLine = "[" + DateTime.Now.ToString("H:mm:ss") + "] " + value;
                Console.Write(writeLine + " ".Repeat(Console.WindowWidth - writeLine.Length - 8));
            }
        }
        static bool HasConnection()
        {
            try
            {
                System.Net.Dns.GetHostEntry("www.google.com");
                return true;
            }
            catch
            {
                return false;
            }
        }
        static string getLLSS()
        {
            string loginString = Client.DownloadString("https://csgolounge.com/");
            string llss = loginString.Substring(loginString.IndexOf("var lngSlt = '") + "var lngSlt = '".Length, 32);
            return llss;
        }
        static bool LogIn()
        {
            var values = new NameValueCollection
                    {
                        { "em", AccName},
                        { "pass", AccPass},
                        { "llss", getLLSS()}
                    };
            Client.UploadValues("https://csgolounge.com/ajax/logIn.php", values);

            if (!loggedIn())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("failure");
                Console.ResetColor();
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("success");
            Console.ResetColor();
            return true;
        }
        static bool loggedIn()
        {
            string loginString = Client.DownloadString("https://csgolounge.com/");
            if (!loginString.Contains("LogInSignUpModal"))
            {
                return true;
            }

            return false;
        }
        static List<string> availableBumps()
        {
            List<string> bumpList = new List<string>();

            if (!loggedIn())
            {
                Log("Failed to retrieve bump list. Not logged in.");
                return bumpList;
            }

            string page = Client.DownloadString("https://csgolounge.com/mytrades");
            string[] pageLines = page.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (string pageLine in pageLines)
            {
                if (pageLine.Contains("bumpTrade"))
                {
                    int startNewBump = pageLine.IndexOf("$(this).hide();bumpTrade('") + "$(this).hide();bumpTrade('".Length;
                    int endNewBump = pageLine.IndexOf("')\">") - startNewBump;
                    string newBump = pageLine.Substring(startNewBump, endNewBump);
                    bumpList.Add(newBump);
                }
            }
            return bumpList;
        }
        static void bumpTrade(List<string> tradeIDs = null, string name = "")
        {
            if (!loggedIn())
            {
                Log("Failed to bump. Not logged in.");
                return;
            }

            List<string> bumpList = availableBumps();
            foreach (string bumpV in bumpList)
            {
                try
                {
                    if (tradeIDs == null || tradeIDs.Count < 1 || tradeIDs.Contains(bumpV))
                    {
                        var values = new NameValueCollection
                        {
                            { "trade", bumpV}
                        };
                        Client.UploadValues("https://csgolounge.com/ajax/bumpTrade.php", values);
                        Bumps++;
                        if (string.IsNullOrEmpty(name))
                            Log("Trade [" + bumpV + "] bumped");
                        else
                            Log("Trade [" + name + "] bumped");
                        break;
                    }
                }
                catch
                {
                    Log("Something went wrong with bumping trade with id [" + name + "]");
                }
            }
        }
        static void loadTrades()
        {
            TradeOptions.Clear();

            if (Doc.DocumentElement.SelectNodes("bumpGroup").Count > 0)
            {
                foreach (XmlNode bumpGroup in Doc.DocumentElement.SelectNodes("bumpGroup"))
                {
                    string nodeName = "";
                    int nodeTime = 10;
                    bool nodeActive = true;
                    List<string> nodeTrades = new List<string>();

                    foreach (XmlNode bumpParam in bumpGroup.ChildNodes)
                    {
                        switch (bumpParam.Name)
                        {
                            case "name":
                                nodeName = bumpParam.InnerText;
                                break;
                            case "refresh":
                                try
                                {
                                    nodeTime = int.Parse(bumpParam.InnerText);
                                }
                                catch { }
                                break;
                            case "active":
                                if (bumpParam.InnerText != "true")
                                    nodeActive = false;
                                break;
                            case "tradeID":
                                nodeTrades.Add(bumpParam.InnerText);
                                break;
                            default:
                                break;
                        }
                    }

                    if (nodeTrades.Count > 0)
                    {
                        TradeOptions.Add(TradeOption.Create(nodeName, nodeTime, nodeActive, nodeTrades));
                    }
                }
            }
        }
        static void loadOffers()
        {
            Offers.Clear();

            if (Doc.DocumentElement.SelectNodes("tradeOffer").Count > 0)
            {
                foreach (XmlNode tradeOffer in Doc.DocumentElement.SelectNodes("tradeOffer"))
                {
                    string tradeID = "";
                    List<string> itemTrades = new List<string>();

                    foreach (XmlNode trade in tradeOffer.ChildNodes)
                    {
                        switch (trade.Name)
                        {
                            case "tradeID":
                                tradeID = trade.InnerText;
                                break;
                            case "analystID":
                                itemTrades.Add(trade.InnerText);
                                break;
                            default:
                                break;
                        }
                    }

                    if (!string.IsNullOrEmpty(tradeID) && itemTrades.Count > 0)
                    {
                        Offers.Add(Offer.Create(tradeID, itemTrades.ToArray()));
                    }
                }
            }
        }
        static void timerThread()
        {
            int startTimeInMillis;
            while (true)
            {
                if (!HasConnection())
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Log("Connection lost, retrying...");
                    Console.ResetColor();
                    Thread.Sleep(10 * 1000);
                    continue;
                }

                if(Timer % (60*60) == 0)
                    updatePrices();

                startTimeInMillis = System.Environment.TickCount;
                if (TradeOptions.Count < 1)
                    if (Timer % 3 == 0)
                        bumpTrade();
                    else { }
                else
                {
                    try
                    {
                        foreach (TradeOption to in TradeOptions)
                        {
                            if (to.Active && Timer % to.Time == 0)
                                bumpTrade(to.Trades, to.Name);
                        }
                    }
                    catch { }
                }

                Timer++;
                Thread.Sleep(Math.Max(0, 1000 - (System.Environment.TickCount - startTimeInMillis)));
            }
        }
        static void updatePrices()
        {
            if (Offers.Count < 1)
                return;

            Log("Updating prices...");

            try
            {
                foreach (Offer of in Offers)
                {
                    string description = "░▒▓   Welcome in "+ ShopName +" trade shop   ▓▒░\n░▒▓ Im buying every item listed here ▓▒░\n\n";
                    foreach (ItemPrice item in getPrices(of.Items))
                    {
                        description += "♯ " + item.Name + " (" + item.Exterior + ")  ▪ " + item.PriceTaxKeys + " keys\n";
                    }
                    description += "\n              ►   send me trade offer   ◄\n";
                    if (!string.IsNullOrEmpty(TradePartner) && !string.IsNullOrEmpty(TradeToken))
                        description += "https://steamcommunity.com/tradeoffer/new/?partner=" + TradePartner + "&token=" + TradeToken;

                    var values = new NameValueCollection
                    {
                        { "trade", of.ID},
                        { "notes", description}
                    };
                    Client.UploadValues("https://csgolounge.com/ajax/tradeNoteSave.php", values);
                }
                Log("Prices updated succesfully");
            }
            catch
            {
                Log("Something went wrong, retrying...");
            }
        }
        public static string Repeat(this string instr, int n)
        {
            if (string.IsNullOrEmpty(instr))
                return instr;

            var result = new StringBuilder(instr.Length * n);
            return result.Insert(0, instr, n).ToString();
        }
        public static bool inArray(this string instr, string[] strArr)
        {
            foreach (string str in strArr)
            {
                if (instr == str)
                    return true;
            }
            return false;
        }

        public static List<ItemPrice> getPrices(string[] itemsURL)
        {
            List<ItemPrice> returnList = new List<ItemPrice>();

            if (itemsURL.Length < 1)
                return returnList;

            WebClient wc = new WebClient();
            foreach(string itemURL in itemsURL)
            {
                string name, exterior, price;
                try
                {
                    string wci = wc.DownloadString("https://csgo.steamanalyst.com/id/" + itemURL);
                    string[] wcs = wci.Split(new string[] { "\n\r", "\n" }, StringSplitOptions.None);
                    bool found = false;

                    int nameBegin = wci.IndexOf("Trade Calculator, Inventory Worth, Floats Database | ") + "Trade Calculator, Inventory Worth, Floats Database | ".Length;
                    int nameEnd = wci.IndexOf("(") - nameBegin;
                    name = wci.Substring(nameBegin, nameEnd);
                    name = Regex.Replace(name, "[^A-Za-z0-9 _-]", "|").TrimStart(new char[] {'|', ' '}).Trim();
                    int extBegin = wci.IndexOf(" (") + " (".Length;
                    int extEnd = wci.IndexOf(")</title>") - extBegin;
                    exterior = wci.Substring(extBegin, extEnd);
                    if (name.Length > 1)
                    {
                        foreach (string wcst in wcs)
                        {
                            if (wcst.Contains(" Keys)"))
                            {
                                string[] eachExts = wcst.Split(new string[] { "class=\"exterior\"" }, StringSplitOptions.None);
                                foreach (string eachExt in eachExts)
                                {
                                    if (eachExt.Contains(" Keys)") && eachExt.Contains(exterior))
                                    {
                                        int indexPrice = eachExt.IndexOf("price pull-right\">$") + "price pull-right\">$".Length;
                                        price = eachExt.Substring(indexPrice, 6);
                                        if (!string.IsNullOrEmpty(exterior) && !string.IsNullOrEmpty(price))
                                        {
                                            try
                                            {
                                                found = true;
                                                float fPrice = float.Parse(price, System.Globalization.CultureInfo.InvariantCulture);
                                                returnList.Add(new ItemPrice(name, exterior, fPrice));
                                                break;
                                            }
                                            catch(Exception ex)
                                            {
                                                Log(ex.Message);
                                            }
                                        }
                                    }
                                }

                            }
                        }
                    }

                    if(!found)
                        Log("Didnt found " + name + " on " + itemURL);
                }
                catch
                {
                    Log("Failed to resolve " + itemURL);
                }
            }
            return returnList;
        }
    }

    //////////////////////////////
    public class TradeOption
    {
        public string Name { get; private set; }
        public int Time { get; private set; }
        public bool Active { get; private set; }
        public List<string> Trades { get; private set; }

        private TradeOption(string Name, int Time, bool Active, List<string> Trades)
        {
            this.Name = Name;
            this.Time = Time;
            this.Active = Active;
            this.Trades = Trades;
        }

        public static TradeOption Create(string name, int time, bool active, List<string> trades)
        {
            return new TradeOption(name, time, active, trades);
        }
    }

    public class ItemPrice
    {
        public string Name { get; private set; }
        public string Exterior { get; private set; }
        public float PriceUSD { get; set; }
        public int PriceKeys
        { 
            get 
            { 
                return (int)Math.Round(PriceUSD / 2.50); 
            } 
        }
        public int PriceTaxKeys
        {
            get
            {
                return (int)Math.Round((PriceUSD / 2.50) * Program.BuyPercent / 100);
            }
        }

        public ItemPrice(string Name, string Exterior, float PriceUSD)
        {
            this.Name = Name;
            this.Exterior = Exterior;
            this.PriceUSD = PriceUSD;
        }
    }

    public class Offer
    {
        public string ID { get; private set; }
        public string[] Items { get; private set; }

        public Offer(string ID, string[] Items)
        {
            this.ID = ID;
            this.Items = Items;
        }

        public static Offer Create(string ID, string[] Items)
        {
            return new Offer(ID, Items);
        }
    }

    public class CookieAwareWebClient : WebClient
    {
        public CookieContainer CookieContainer { get; private set; }

        public CookieAwareWebClient()
        {
            CookieContainer = new CookieContainer();
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest)base.GetWebRequest(address);
            request.CookieContainer = CookieContainer;
            return request;
        }
    }
}