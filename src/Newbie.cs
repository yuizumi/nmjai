using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using NMahjong.Aux;
using NMahjong.Base;
using NMahjong.Japanese;

using TA = NMahjong.Japanese.TileAnnotations;

namespace Yuizumi.Mahjong.Intelligences
{
    public class Newbie : IIntelligenceFactory
    {
        public Intelligence Create(IntelligenceArgs args,
                                   IEventHandlerRegisterer registerer)
        {
            return new NewbieBody(args, registerer);
        }
    }

    internal class NewbieBody : Intelligence
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly ImmutableList<Int32> NumberTileScore =
            ImmutableList.Of(0, 5, 7, 9, 9, 9, 9, 9, 7, 5);

        private readonly Random mRandom;
        private ICollection<Tile> mValuedTiles;

        internal NewbieBody(IntelligenceArgs args, IEventHandlerRegisterer registerer)
            : base(args)
        {
            mRandom = new Random();
            registerer.AddOnHandStarting((sender, e) => SetValuedTiles());
        }

        public override IPlayerAction OnTurn()
        {
            return DoIt("OnTurn()");
        }

        public override IPlayerAction OnRiichi()
        {
            return DoIt("OnRiichi()");
        }

        public override IPlayerAction OnDiscard(PlayerId player, AnnotatedTile tile)
        {
            return DoIt(String.Format("OnDiscard({0}, {1})", player, tile));
        }

        public override IPlayerAction OnKong(PlayerId player, CanonicalTile tile)
        {
            return DoIt(String.Format("OnKong({0}, {1})", player, tile));
        }

        private IPlayerAction DoIt(string caller)
        {
            Log.Info("Entering {0}.", caller);

            var choices = RuleAdvisor.GetValidActions();
            int[] hand = HandAnalysis.GetVector(Self.Tiles);
            int[] rest = HandAnalysis.GetRemainderVector(GameState);

            var bestChoices = new List<IPlayerAction>();
            var bestScore = Int32.MinValue;
            foreach (IPlayerAction choice in choices) {
                int score = Evaluate(choice, hand, rest);
                if (score > bestScore) { bestChoices.Clear(); bestScore = score; }
                if (score == bestScore) bestChoices.Add(choice);
            }
            var outcome = bestChoices[mRandom.Next(bestChoices.Count)];

            Log.Info("Exiting {0} with {1}.", caller, outcome);

            return outcome;
        }

        private int Evaluate(IPlayerAction action, int[] hand, int[] rest)
        {
            if (action is MahjongAction) {
                return 50000;
            }
            if (action is RiichiAction) {
                return 40000;
            }
            if (action is AbortiveDrawAction) {
                return 30000;
            }

            if (action is NoneAction) {
                return 1;
            }
            if (action is PungAction) {
                return EvaluatePung((action as PungAction).Meld.Tiles[0], hand, rest);
            }
            if (action is MeldAction) {
                return 0;
            }
            if (action is DiscardAction) {
                return EvaluateDiscard((action as DiscardAction).Tile, hand, rest);
            }

            Log.Error("Unknown action: {0}.", action);
            return -1;
        }

        private int EvaluatePung(Tile tile, int[] hand, int[] rest)
        {
            if (!mValuedTiles.Contains(tile)) {
                return 0;
            }
            int dist0 = GetDistance(hand, rest);
            hand[tile.GetIndex()] -= 2;
            int dist1 = GetDistance(hand, rest);
            hand[tile.GetIndex()] += 2;
            if (dist0 < dist1) {
                return 0;
            }
            if (GameState.Dora.Any(dora => tile == dora.Tile)) {
                return 20000;
            }
            return (rest[tile.GetIndex()] == 2) ? 5000 : 0;
        }

        private int EvaluateDiscard(AnnotatedTile tile, int[] hand, int[] rest)
        {
            var debug = new StringBuilder();
            int score = 10000;

            --hand[tile.BaseTile.GetIndex()];

            int dist = GetDistance(hand, rest);
            int dora = GetDoraCount(tile);
            score -= dist * 1000 + (1 + dora * 10) * GetTileScore(tile.BaseTile);
            debug.AppendFormat("{0}: Distance={1}, Dora={2}", tile, dist, dora);

            for (int i = 0; i < hand.Length; i++) {
                if (rest[i] <= hand[i]) continue;
                ++hand[i];
                bool isEffective = GetDistance(hand, rest) < dist;
                --hand[i];
                if (isEffective) {
                    score += (rest[i] - hand[i]) * 10;
                    debug.AppendFormat(", {0}({1})", Tile.AllTiles[i], rest[i] - hand[i]);
                }
            }

            Log.Debug(debug.AppendFormat(", Score={0}", score));
            ++hand[tile.BaseTile.GetIndex()];

            return score;
        }

        private int GetDoraCount(AnnotatedTile tile)
        {
            int count = GameState.Dora.Count(dora => dora.Tile == tile.BaseTile);
            return (tile.Has(TA.Red)) ? (count + 1) : count;
        }

        private int GetDistance(int[] hand, int[] rest)
        {
            return HandAnalysis.GetDistance(4 - Self.Melds.Count, hand, rest);
        }

        private int GetTileScore(Tile tile)
        {
            if (tile.IsNumberTile()) {
                return NumberTileScore[tile.Rank];
            } else {
                return mValuedTiles.Contains(tile) ? 4 : 2;
            }
        }

        private void SetValuedTiles()
        {
            mValuedTiles = new HashSet<Tile>() {
                Tile.JP, Tile.JF, Tile.JC,
                Self.SeatWind.GetTile(), GameState.PrevailingWind.GetTile(),
            };
        }
    }
}
