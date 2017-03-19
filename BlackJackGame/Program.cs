using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackJackGame
{
    // GAME States
    public enum GameResult { Win = 1, Lose = -1, Draw = 0, Pending = 2};

    /// <summary>
    /// Moje karty
    /// </summary>
    public class Card
    {
        public string ID { get; set; }
        public string Suit { get; set; }
        public int Value { get; set; }

        public Card(string id, string suit, int value)
        {
            ID = id;
            Suit = suit;
            Value = value;
        }
    }

    /// <summary>
    /// Balíček karet
    /// </summary>
    public class Deck : Stack<Card>
    {
        public Deck(IEnumerable<Card> collection) : base(collection) { }
        public Deck() : base(52) { }

        public Card this[int index]
        {
            get
            {
                Card item;
                if (index >= 0 && index <= this.Count - 1)
                {
                    item = this.ToArray()[index];
                }
                else
                {
                    item = null;
                }

                return item;
            }
        }

        /// <summary>
        /// Hodnota karty
        /// </summary>
        public double Value
        {
            get
            {
                return BlackJackRules.HandValue(this);
            }
        }
    }

    /// <summary>
    /// Uživatelé:
    /// Dealer
    /// Hráč
    /// </summary>
    public class Member
        {
             public Deck Hand;
            public Member()
            {
                Hand = new Deck();
            }
        }

    /// <summary>
    /// Pravidla hry.
    /// Nastavení: barvy a hodnoty karet.
    /// </summary>
    public static class BlackJackRules
        {
            // Hodnoty karet
            public static string[] ids = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "A", "J", "K", "Q" };

        // Barvy karet    
        public static string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };

            // Vrací nový balíček karet
            public static Deck NewDeck
            {
               get
                 {
                    Deck d = new Deck();
                    int value;

                    foreach (string suit in suits)
                    {
                        foreach (string id in ids)
                        {
                            value = Int32.TryParse(id, out value) ? value : id == "A" ? 1 : 10;
                            d.Push(new Card(id, suit, value));
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
                return new Deck(NewDeck.OrderBy(card => System.Guid.NewGuid()).ToArray());
            }
        }

        /// <summary>
        /// Metoda vypočítá hodnoty karet v ruce.
        /// </summary>
        /// <param name="deck"></param>
        /// <returns></returns>
        public static double HandValue(Deck deck)
        {
            int val1 = deck.Sum(c => c.Value);

            double aces = deck.Count(c => c.Suit == "A");
            double val2 = aces > 0 ? val1 + (10 * aces) : val1;

            return new double[] { val1, val2 }
                .Select(handVal => new
                {
                    handVal,
                    weight = Math.Abs(handVal - 21) + (handVal > 21 ? 100 : 0)
                })
                .OrderBy(n => n.weight)
                .First().handVal;
        }

        /// <summary>
        /// Kontrola, jestli Dealer může táhnout na základě hodnoty karet v ruce.
        /// Předpokládejme, že Dealer bude chtít vždy 17.
        /// </summary>
        public static bool CanDealerHit(Deck deck, int standLimit)
        {
            return deck.Value < standLimit;
        }

        /// <summary>
        /// Nemá smysl dělat svůj tah nad 21.
        /// </summary>
        /// <param name="deck"></param>
        /// <returns></returns>
        public static bool CanPlayerHit(Deck deck)
        {
            return deck.Value < 21;
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
            if(playerValue <= 21)
            {
                // a ...
                if(playerValue != dealerValue)
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

    /// <summary>
    /// Příprava a začátek hry
    /// </summary>
    public class BlackJack
    {
        public Member Dealer = new Member();
        public Member Player = new Member();
        public GameResult Result { get; set; }

        public Deck MainDeck;

        public int StandLimit { get; set; }
        public BlackJack(int dealerStandLimit)
        {
            // Příprava hry ..
            Result = GameResult.Pending;
            StandLimit = dealerStandLimit;

            // Zamíchá karty a položí na stůl.
            MainDeck = BlackJackRules.ShuffledDeck;

            // Vyčistí ruce hráčů a Dealerů (i rukávy :) )
            Dealer.Hand.Clear();
            Player.Hand.Clear();

            // Přiřadí první dvě karty hráči a Dealerovi...
            for(int i = 0; ++i < 3;)
            {
                Dealer.Hand.Push(MainDeck.Pop());
                Player.Hand.Push(MainDeck.Pop());
            }
        }

        /// <summary>
        /// Povolení hráči udělat tah. Dealer automaticky táhne až když uživatel stojí.
        /// </summary>
        public void Hit()
        {
            if(BlackJackRules.CanPlayerHit(Player.Hand) && Result == GameResult.Pending)
            {
                Player.Hand.Push(MainDeck.Pop());
            }
        }

        /// <summary>
        /// Když hráč stojí, umožní Dealerovi pokračovat v tahu, dokud nevyprší limit.
        /// Pak hra pokračuje a nastaví výsledek hry.
        /// </summary>
        public void Stand()
        {
            if(Result == GameResult.Pending)
            {
                while(BlackJackRules.CanDealerHit(Dealer.Hand, StandLimit))
                {
                    Dealer.Hand.Push(MainDeck.Pop());
                }

                Result = BlackJackRules.GetResult(Player, Dealer);
            }
        }
    }

    /// <summary>
    /// Samotné spuštění hry.
    /// Možnosti ovládání.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Zobrazí informace o tahu Dealera a hráče.
        /// </summary>
        /// <param name="bj"></param>
        public static void ShowStats(BlackJack bj)
        {
            // Dealer
            Console.WriteLine("Dealer: ");
            foreach(Card c in bj.Dealer.Hand)
            {
                Console.WriteLine(string.Format("{0} {1}", c.ID, c.Suit));
            }

            Console.WriteLine("Celkem: " + bj.Dealer.Hand.Value);

            Console.WriteLine(Environment.NewLine);

            // Hráč
            Console.WriteLine("Hráč: ");
            foreach (Card c in bj.Player.Hand)
            {
                Console.WriteLine(string.Format("{0} {1}", c.ID, c.Suit));
            }

            Console.WriteLine("Celkem: " + bj.Player.Hand.Value);

            Console.WriteLine(Environment.NewLine);
        }

        public static void Main(string[] args)
        {
            string input = "";
            
            BlackJack bj = new BlackJack(17);

            ShowStats(bj);

            Console.WriteLine("Jsi na řadě. Táhneš (h) nebo stojíš (s)?");

            while (bj.Result == GameResult.Pending)
            {
                input = Console.ReadLine();

                // Napiš H/h pro hit (hraju) 
                // nebo S/s pro stand (stojím)
                if(input.ToLower() == "h")
                {
                    bj.Hit();
                    ShowStats(bj);
                }
                else
                {
                    bj.Stand();
                    ShowStats(bj);
                }
            }

            Console.WriteLine(bj.Result);
            Console.ReadLine();
        }
    }
}
