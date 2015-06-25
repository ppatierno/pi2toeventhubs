using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pi2EventHubProcessor
{
    class Pi2EventHubProcessor : IEventProcessor
    {
        private Stopwatch checkpointStopWatch;

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine(string.Format("Processor close.  Partition '{0}', Reason: '{1}'.", context.Lease.PartitionId, reason.ToString()));
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        public Task OpenAsync(PartitionContext context)
        {
            Console.WriteLine(string.Format("Processor open.  Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset));
            this.checkpointStopWatch = new Stopwatch();
            this.checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (EventData eventData in messages)
            {
                if (eventData.Properties.ContainsKey("time"))
                {
                    if (eventData.Properties.ContainsKey("temp"))
                        Console.WriteLine(string.Format("time = {0}, temp = {1}", eventData.Properties["time"], eventData.Properties["temp"]));
                }
                else
                {
                    byte[] body = eventData.GetBytes();
                    Console.WriteLine(Encoding.ASCII.GetString(body));
                }
            }

            //Call checkpoint every 5 minutes, so that worker can resume processing from the 5 minutes back if it restarts.
            //if (this.checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
            if (this.checkpointStopWatch.Elapsed > TimeSpan.FromSeconds(30))
            {
                await context.CheckpointAsync();
                this.checkpointStopWatch.Restart();
            }
        }
    }
}
