namespace DocuWare.Assignment.Radu
{
    public class Producer(Func<int> numberGenerator, Sequencer sequencer, int longDelay, int shortDelay)
    {
        private Task? _workerTask;
        private bool _useLongDelay;

        public void Start(CancellationToken cancellationToken)
        {
            _workerTask = Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var number = numberGenerator();

                        await sequencer.PerformAsync(queue => queue.Add(number));
                        await ApplyAlternatingDelay(cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Handle graceful task cancellation
                    Console.WriteLine("Producer task canceled.");
                }
            }, cancellationToken);
        }

        public async Task StopAsync(CancellationTokenSource cancellationTokenSource )
        {
            cancellationTokenSource.Cancel();

            if (_workerTask != null)
            {
                await _workerTask.WaitAsync(CancellationToken.None);
            }
        }

        private async Task ApplyAlternatingDelay(CancellationToken cancellationToken)
        {
            if (_useLongDelay)
            {
                await Task.Delay(longDelay, cancellationToken);
            }
            else
            {
                await Task.Delay(shortDelay, cancellationToken);
            }

            _useLongDelay = !_useLongDelay;
        }
    }
}
