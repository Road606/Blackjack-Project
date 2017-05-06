using System;
using System.Collections.Generic;
using System.Linq;


namespace BlackJackGame
{
    public enum GameResult { Win = 1, Lose = -1, Draw = 0, Pending = 2 }
    public enum MenuKey { Hit = 0, Stand = 1, Double = 2, Split = 3 }



    public struct Card
    {
        public string ID;
        public string Suit;
        public int Value;

        public Card(string id, string suit, int value)
        {
            this.ID = id;
            this.Suit = suit;
            this.Value = value;
        }
    }

    public struct Deck
    {
        public Stack<Card> deck;


        public Deck(Card[] card) : this()
        {
            deck = new Stack<Card>();
            foreach (Card item in card)
            {

                this.deck.Push(item);
            }


        }

        public void Init()
        {
            this.deck = new Stack<Card>();
        }

        public double Value()
        {
            return BlackJackRules.HandValue(this);
        }


    }
    public struct Member
    {
        public Deck Hand;
        public int Credit;

        public Member(int credit)
        {
            Hand = new Deck();
            Hand.Init();
            Credit = credit;
        }
    }


    public struct BlackJackRules
    {
        // Hodnoty karet
        public static string[] ids = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "A", "J", "K", "Q" };

        // Barvy karet    
        public static string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };


        // Vrací nový balíček karet
        public static Deck Newdeck
        {
            get
            {
                Deck d = new Deck();
                d.Init();
                int value;

                foreach (string suit in suits)
                {
                    foreach (string id in ids)
                    {
                        value = Int32.TryParse(id, out value) ? value : id == "A" ? 1 : 10;
                        d.deck.Push(new Card(id, suit, value));
                    }
                }
                return d;

            }
        }
        /// <summary>
        /// Vrací zamíchaný balíček karet
        /// </summary>
        public static Deck ShuffledDeck
        {
            get
            {
                return new Deck(Newdeck.deck.OrderBy(card => Guid.NewGuid()).ToArray());
            }
        }

        /// <summary>
        /// Metoda vypočítá hodnoty karet v ruce.
        /// </summary>
        /// <param name="deck"></param>
        /// <returns></returns>
        public static double HandValue(Deck deck)
        {
            int val1 = deck.deck.Sum(c => c.Value);

            double aces = deck.deck.Count(c => c.ID == "A");
            double val2 = aces > 0 ? val1 + (10 * aces) : val1;

            return new double[] { val1, val2 }
            .Select(handVal => new
            {
                handVal,
                weight = Math.Abs(handVal - 21) + (handVal > 21 ? 100 : 0)
            }).OrderBy(n => n.weight).First().handVal;
        }

        /// <summary>
        /// Kontrola, jestli Dealer může táhnout na základě hodnoty karet v ruce.
        /// Předpokládejme, že Dealer bude chtít vždy 17.
        /// </summary>
        public static bool CanDealerHit(Deck deck, int standLimit)
        {
            return deck.Value() < standLimit;
        }

        /// <summary>
        /// Nemá smysl dělat svůj tah nad 21.
        /// </summary>
        /// <param name="deck"></param>
        /// <returns></returns>
        public static bool CanPlayerHit(Deck deck)
        {
            return deck.Value() < 21;
        }

        /// <summary>
        /// Vrací stav hry - výhra, prohra nebo čerpání rukou hráčů.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="dealer"></param>
        /// <returns></returns>
        public static GameResult GetResult(Member player, Member dealer)
        {
            GameResult result = GameResult.Win;

            double playerValue = HandValue(player.Hand);
            double dealerValue = HandValue(dealer.Hand);

            // hráč může zvítězit, pokud...
            if (playerValue <= 21)
            {
                // a ...
                if (playerValue != dealerValue)
                {
                    double closestValue = new double[] { playerValue, dealerValue }
                    .Select(handVal => new { handVal, weight = Math.Abs(handVal - 21) + (handVal > 21 ? 100 : 0) })
                    .OrderBy(n => n.weight)
                    .First().handVal;

                    result = playerValue == closestValue ? GameResult.Win : GameResult.Lose;
                }
                else
                {
                    result = GameResult.Draw;
                }
            }
            else
            {
                result = GameResult.Lose;
            }

            return result;
        }

    }
    public struct BlackJack
    {
        public Member Dealer;
        public Member Player;
        public GameResult Result;

        public Deck MainDeck;

        public int StandLimit;

        public int Bet;
        public BlackJack(int dealerStandLimit) : this()
        {
            // Zamíchá karty a položí na stůl.
            Dealer = new Member(0);
            Player = new Member(100);
            StandLimit = dealerStandLimit;
            MainDeck = BlackJackRules.ShuffledDeck;
            this.Init();
        }

        /// <summary>
        /// Povolení hráči udělat tah. Dealer automaticky táhne až když uživatel stojí.
        /// </summary>
        public void Hit()
        {
            if (BlackJackRules.CanPlayerHit(Player.Hand) && Result == GameResult.Pending)
            {
                Player.Hand.deck.Push(MainDeck.deck.Pop());
            }
            else
            {
                Result = BlackJackRules.GetResult(Player, Dealer);
            }
        }

        /// <summary>
        /// Když hráč stojí, umožní Dealerovi pokračovat v tahu, dokud nevyprší limit.
        /// Pak hra pokračuje a nastaví výsledek hry.
        /// </summary>
        public void Stand()
        {
            if (Result == GameResult.Pending)
            {
                while (BlackJackRules.CanDealerHit(Dealer.Hand, StandLimit))
                {
                    Dealer.Hand.deck.Push(MainDeck.deck.Pop());
                }

                Result = BlackJackRules.GetResult(Player, Dealer);
            }
        }
        public void Init()
        {
            // Příprava hry ..
            Result = GameResult.Pending;

            // Vyčistí ruce hráčů a Dealerů (i rukávy :) )
            Dealer.Hand.deck.Clear();
            Player.Hand.deck.Clear();

            // Přiřadí první dvě karty hráči a Dealerovi...
            for (int i = 0; ++i < 3;)
            {
                Dealer.Hand.deck.Push(MainDeck.deck.Pop());
                Player.Hand.deck.Push(MainDeck.deck.Pop());
            }
        }
        public void Reset()
        {
            this.Init();
            // Pokud je v balíčku méně jak 1/3 karet zamíchá nový balíček
            if (MainDeck.deck.Count < 17)
            {
                MainDeck = BlackJackRules.ShuffledDeck;
            }
        }





    }
    public struct MenuItem
    {
        public string Key;
        public string Caption;
        public bool IsActive;

        public MenuItem(string key, string caption, bool isactive)
        {
            Key = key;
            Caption = caption;
            IsActive = isactive;
        }


        public class Program
        {
            public const int MINBET = 5;

            public static void CreateMenu(out MenuItem[] menu)
            {
                menu = new MenuItem[4];
                menu[(int)MenuKey.Hit] = new MenuItem("h", "hrát", true);
                menu[(int)MenuKey.Stand] = new MenuItem("s", "stát", true);
                menu[(int)MenuKey.Double] = new MenuItem("d", "double", false);
                menu[(int)MenuKey.Split] = new MenuItem("t", "split", false);
            }



            public static void ShowStats(BlackJack bj)
            {
                // Dealer
                Console.WriteLine("Dealer: ");
                // U dealera se během hry zobrazuje pouze jedna karta
                if (bj.Result == GameResult.Pending)
                {

                    Console.WriteLine(string.Format("{0} {1}", bj.Dealer.Hand.deck.Peek().ID, bj.Dealer.Hand.deck.Peek().Suit));
                    Console.WriteLine("* *");
                }
                else
                {
                    foreach (Card c in bj.Dealer.Hand.deck)
                    {
                        Console.WriteLine(string.Format("{0} {1}", c.ID, c.Suit));
                    }
                    Console.WriteLine("Celkem: " + bj.Dealer.Hand.Value());
                }


                Console.WriteLine(Environment.NewLine);

                // Hráč
                Console.WriteLine("Hráč: ");
                foreach (Card c in bj.Player.Hand.deck)
                {
                    Console.WriteLine(string.Format("{0} {1}", c.ID, c.Suit));
                }

                Console.WriteLine("Celkem: " + bj.Player.Hand.Value());

                Console.WriteLine(Environment.NewLine);
            }
            static void Main(string[] args)
            {

                string input = "";
                int bet = 0;
                Console.OutputEncoding = System.Text.Encoding.Unicode;
                CreateMenu(out MenuItem[] menu);
                bool nextGame = true;
                BlackJack bj = new BlackJack(17);
                do
                {

                    while (true)
                    {
                        // Žádá uživatele o zadání 
                        Console.WriteLine("Tvůj kredit je: {0} Kolik sázíš? (minimální sázka je {1})", bj.Player.Credit, MINBET);
                        try
                        {
                            bet = int.Parse(Console.ReadLine());
                            if (bet > bj.Player.Credit)
                            {
                                throw new Exception("Nemáš dostatečný kredit na tuto sázku");
                            }
                            if (bet < MINBET)
                            {
                                throw new Exception("Sázka je menší než minimální sázka.");
                            }
                            break;
                        }
                        catch (FormatException e)
                        {
                            Console.WriteLine("Nebylo zadáno číslo.");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }

                    }
                    ShowStats(bj);

                    while (bj.Result == GameResult.Pending)
                    {


                        Console.Write("Jsi na řadě. ");

                        foreach (MenuItem item in menu)
                        {
                            if (item.IsActive)
                            {
                                Console.Write("{0}:{1}; ", item.Key, item.Caption);
                            }
                        }
                        Console.Write(Environment.NewLine);
                        input = Console.ReadLine();
                        MenuItem result = Array.Find(menu, k => k.Key == input.ToLower());

                        //v případě, že uživatel zadá volbu, která sice může být v menu, ale není aktivní, nastaví výslednou klávesu na null, čímž spustí default switch
                        if (!result.IsActive)
                        {
                            result.Key = null;
                        }


                        switch (result.Key)
                        {
                            case "h":
                                bj.Hit();
                                ShowStats(bj);
                                break;


                            case "s":
                                bj.Stand();
                                ShowStats(bj);
                                break;

                            default:
                                Console.Write("Nesprávná volba. ");
                                break;
                        }

                    }

                    Console.WriteLine(bj.Result);
                    switch (bj.Result)
                    {
                        case GameResult.Win:
                            bj.Player.Credit += bet;
                            break;
                        case GameResult.Lose:
                            bj.Player.Credit -= bet;
                            break;
                        default:
                            break;

                    }
                    if (bj.Player.Credit < MINBET)
                    {
                        Console.WriteLine("Konec hry...");
                        Console.ReadLine();
                        break;
                    }
                    Console.WriteLine("Další hra? A/N");
                    input = Console.ReadLine();
                    if (input.ToLower() == "a")
                    {
                        nextGame = true;
                        bj.Reset();

                    }
                    else { nextGame = false; }



                } while (nextGame);
            }
        }
    }
}