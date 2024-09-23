using System.Collections.Concurrent;

namespace DocuWare.Assignment.Radu
{
    public class Sequencer
    {
        private const int MAX_ALLOWED_ITEMS = 20;
        private List<int> _queue = [];
        private readonly Task? _workerTask;
        private readonly ConcurrentQueue<Action<List<int>>> _requestsQueue = new();

        public Action<List<int>>? QueueChangedNotifier { get; set; }

        public Sequencer(CancellationToken cancellationToken)
        {
            _workerTask = Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (_requestsQueue.TryDequeue(out Action<List<int>>? request))
                        {
                            request(_queue);

                            if (_queue.Count > MAX_ALLOWED_ITEMS)
                            {
                                _queue = _queue.Skip(_queue.Count - MAX_ALLOWED_ITEMS).ToList();
                            }

                            QueueChangedNotifier?.Invoke(new List<int>(_queue));

                            await Task.Delay(10, cancellationToken);
                        }
                        else
                        {
                            await Task.Delay(1, cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Handle graceful task cancellation
                    Console.WriteLine("Sequencer task canceled.");
                }
            }, cancellationToken);
        }

        public async Task StopAsync(CancellationTokenSource cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();

            if (_workerTask != null)
            {
                await _workerTask.WaitAsync(CancellationToken.None);
            }
        }

        public Task PerformAsync(Action<List<int>> action)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            _requestsQueue.Enqueue(queue =>
            {
                action(queue);

                taskCompletionSource.SetResult(true);
            });

            return taskCompletionSource.Task;
        }
    }
}
