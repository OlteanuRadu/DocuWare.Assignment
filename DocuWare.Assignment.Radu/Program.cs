namespace DocuWare.Assignment.Radu
{
    public static class Program
    {
        private const int EVEN_PRODUCER_LONG_DELAY = 2000;
        private const int EVEN_PRODUCER_SHORT_DELAY = 1;
        private const int NEGATIVE_PRODUCER_LONG_DELAY = 1000;
        private const int NEGATIVE_PRODUCER_SHORT_DELAY = 2;

        static async Task Main(string[] args)
        {
            var (sequencer, sequencerCancelationTokenSource) = CreateAndStartSequencer();

            var producers = CreateAndStartProducers(sequencer);

            Console.ReadKey();

            await StopAllProducersAsync(producers);
            await StopSequencerGracefullyAsync(sequencer, sequencerCancelationTokenSource);
        }

        static Func<int> GenerateEvenNumbers()
        {
            int number = 0;
            return () =>
            {
                number += 2;
                return number;
            };
        }

        static Func<int> GenerateNegativeNumbers()
        {
            int number = -1;
            return () =>
            {
                return --number;
            };
        }

        public static (Sequencer sequencer, CancellationTokenSource sequencerCancelationTokenSource) CreateAndStartSequencer()
        {
            var sequencerCancelationTokenSource = new CancellationTokenSource();
            var sequencer = new Sequencer(sequencerCancelationTokenSource.Token)
            {
                QueueChangedNotifier = queue =>
                {
                    Console.Clear();
                    Console.WriteLine("Queue Updated:");
                    foreach (var item in queue)
                    {
                        Console.WriteLine(item);
                    }
                }
            };

            return (sequencer, sequencerCancelationTokenSource);
        }

        public static async Task StopSequencerGracefullyAsync(Sequencer sequencer, CancellationTokenSource sequencerCancelationTokenSource)
        {
            await sequencer.StopAsync(sequencerCancelationTokenSource);
        }

        public static Dictionary<Producer, CancellationTokenSource> CreateAndStartProducers(Sequencer sequencer)
        {
            var producers = new Dictionary<Producer, CancellationTokenSource>();

            var evenProducer = new Producer(
               GenerateEvenNumbers(),
               sequencer,
               EVEN_PRODUCER_LONG_DELAY,
               EVEN_PRODUCER_SHORT_DELAY
               );

            var negativeProducer = new Producer(
                GenerateNegativeNumbers(),
                sequencer,
                NEGATIVE_PRODUCER_LONG_DELAY,
                NEGATIVE_PRODUCER_SHORT_DELAY
                );

            producers.Add(evenProducer, new CancellationTokenSource());
            producers.Add(negativeProducer, new CancellationTokenSource());

            foreach (var producer in producers)
            {
                producer.Key.Start(producer.Value.Token);
            }

            return producers;
        }

        public static async Task StopAllProducersAsync(IDictionary<Producer, CancellationTokenSource> producers)
        {
            foreach (var producer in producers)
            {
                await producer.Key.StopAsync(producer.Value);
            }
        }
    }
}
