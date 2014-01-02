using System;
using NLog;
using NMahjong.Base;
using NMahjong.Japanese;

namespace Yuizumi.Mahjong.Intelligences
{
    public class Fickle : IIntelligenceFactory
    {
        public Intelligence Create(IntelligenceArgs args,
                                   IEventHandlerRegisterer registerer)
        {
            return new FickleBody(args);
        }
    }

    internal class FickleBody : Intelligence
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Random mRandom;

        internal FickleBody(IntelligenceArgs args)
            : base(args)
        {
            mRandom = new Random();
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
            IPlayerAction outcome = choices[mRandom.Next(choices.Count)];
            Log.Info("Exiting {0} with {1}.", caller, outcome);

            return outcome;
        }
    }
}
