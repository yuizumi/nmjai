using System;
using System.Collections.Generic;
using System.Linq;
using NMahjong.Aux;
using NMahjong.Base;
using NMahjong.Japanese;

using MS = NMahjong.Base.MeldState;
using TA = NMahjong.Japanese.TileAnnotations;

namespace Yuizumi.Mahjong.Intelligences
{
    public static class HandAnalysis
    {
        private static readonly int[][] MidToTids = BuildMidToTids();

        private static readonly int NumTiles = Tile.AllTiles.Count;
        private static readonly int NumMelds = MidToTids.Length;

        private static int[][] BuildMidToTids()
        {
            var melds = Enumerable.Concat(Pung.GetAllPungs(MS.Concealed).Cast<Meld>(),
                                          Chow.GetAllChows(MS.Concealed).Cast<Meld>());
            return melds.Select(m => m.Tiles.Select(Tiles.GetIndex).ToArray()).ToArray();
        }

        public static int[] GetVector(IEnumerable<AnnotatedTile> tiles)
        {
            int[] vector = new int[NumTiles];
            foreach (AnnotatedTile tile in tiles) ++vector[tile.BaseTile.GetIndex()];
            return vector;
        }

        public static int[] GetRemainderVector(IGameState gameState)
        {
            int[] vector = Enumerable.Repeat(4, NumTiles).ToArray();
            foreach (IPlayerState player in gameState.Players) {
                foreach (RevealedMeld meld in player.Melds) {
                    meld.Tiles.ForEach(tile => --vector[tile.GetIndex()]);
                }
                foreach (AnnotatedTile discard in player.Discards) {
                    if (!discard.Has(TA.Claimed)) --vector[discard.BaseTile.GetIndex()];
                }
            }
            return vector;
        }

        public static int GetDistance(int reqMelds, int[] hand, int[] rest)
        {
            // TODO(yuizumi): Consider Thirteen Orphans.
            int limit = (reqMelds == 4) ? GetSevenPairsDistance(hand, rest) : 99;
            return GetStandardDistance(reqMelds, hand, rest, 0, 0, limit);
        }

        private static int GetSevenPairsDistance(int[] hand, int[] rest)
        {
            int[] vector = new int[NumTiles];
            for (int i = 0; i < NumTiles; i++) {
                vector[i] = (rest[i] >= 2) ? Math.Max(2 - hand[i], 0) : 99;
            }
            Array.Sort(vector);
            int sum = vector.Take(7).Sum();
            return Math.Min(99, (sum <= 2) ? sum : sum * 2);
        }

        private static int GetStandardDistance(int nmelds, int[] hand, int[] rest, int mid,
                                                int accum, int limit)
        {
            if (nmelds == 0) {
                for (int i = 0; i < NumTiles; i++) {
                    if (rest[i] < 2) continue;
                    int newAccum = accum + Math.Min(Math.Max(2 - hand[i], 0), 2);
                    limit = Math.Min(limit, newAccum);
                }
            } else {
                for (; mid < NumMelds; ++mid) {
                    var tids = MidToTids[mid];
                    int newAccum = accum;
                    foreach (int tid in tids) {
                        if (--rest[tid] < 0) { newAccum = 99; }
                        if (--hand[tid] < 0) { ++newAccum; }
                    }
                    if (newAccum < limit) {
                        limit = GetStandardDistance(nmelds - 1, hand, rest, mid, newAccum, limit);
                    }
                    foreach (int tid in tids) { ++rest[tid]; ++hand[tid]; }
                }
            }

            return limit;
        }
    }
}
